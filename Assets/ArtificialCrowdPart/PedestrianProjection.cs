using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UI;

/*
 * Written by Yalım Doğan
 * This code projects the pedestrians from given video feed onto the provided plan of the area
 * Version 3.0
 *  + Poles and other unnecessary obstacles are removed.
 *  + Testing for raytracing and its coordinate system
 *  + Adapting the coordinate system of given output to ray tracing
 *  + Ray shouldn't consider agents (Done by IgnoreRayCast layer)
 *  + Positioning is made more adaptive to resolution, output file format
 *  + Velocity is also projected for each agent
 *  + Agents within certain angle to camera are ignored, to fight false positives. But angles are given manually
 *  + Velocities are updated for each agent, each frame
 *  + As the process is only few frames long (not a live stream yet), we need longer duration in order to fully project the recorded behaviour
 *  + If not provided, we can safely assume that they preserve their velocities. This can be also done by giving the control of those
 *    projected agents to artificial agent class (Therefore, wandering in the area with random objectives)
 *    
 *  
 */

namespace RVO
{

    public sealed class PedestrianProjection : MonoBehaviour
    {

        //Singleton instance
        private static PedestrianProjection instance = new PedestrianProjection();

        public static PedestrianProjection Instance
        {
            get
            {
                return instance;
            }
        }

        //Speed limit for projected agents; used against false positives. Value determined according to pedestrian height
        private float speedLimit = 10f;
        public float SpeedLimit { get { return speedLimit; } set { speedLimit = value; } }

        //CONSTANTS

        //Threshold for velocity, used for validating the detection. If the velocity of the detected pedestrian exceeds this, it is considered as a noise
        //in detection system
        private const float velocityDifferenceThreshold = 50f;
        private const float distance = 5000.0F; //Creating distance from camera

        //VARIABLES


        private bool isRunning;
        public bool IsSimulationRunning { get { return instance.isRunning; } }

        //Are projected agents visible to user ?
        private bool visibility;
        public bool Visibility { set { instance.visibility = value; } get { return instance.visibility; } }

        //The camera viewport parameters are needed for placement of camera render on scene (as width and height are same, only one is enough)
        private float viewPortScale;

        //Determined in editor for which video output to be used
        private string[] frame_track_info; //Frames containing the pedestrian information

        //As serkan magnified the image for better detection, I need to divide my coordinates with
        //the its multiplier. This info is given in the first line of the output file
        //  private float magnificationMultiplier;
        private int frameNumber; //Current frame index
        private int linesRead;
        private int resX, resY; //Resolution of given frames

        //Dictionary for agents, where the key is their id
        Dictionary<int, GameObject> realAgents = new Dictionary<int, GameObject>();
        public Dictionary<int, GameObject> RealAgents { get { return instance.realAgents; } }
        public int NumOfRealAgents { get { return instance.realAgents.Count; } }

        private GameObject model; //Dummy for agents
        private GameObject newAgent; //Reference to the created agent

        MyVideoPlayer video;
        GameObject camera;

        private List<float> proposedHeights;
        private int heightFrames;

        private bool pedestrianHeightValid;
        public bool PedestrianHeightValid
        {
            get { return pedestrianHeightValid; }
            set { pedestrianHeightValid = value; }
        }

        private float pedestrianHeight = 0.0f; //This will determine the relative height of the pedestrians
        public float PedestrianHeight
        {
            get { return pedestrianHeight;  }
            set { pedestrianHeight = value; }

        }

        //The execution starts here (Also system is restarted from here upon call)
        public void InitiateProjection(string videoPath, string txtPath)
        {

            //Delete all agents in the simulation if the simulation has been restarted
            List<int> keys = instance.realAgents.Keys.ToList();

            foreach (int key in keys)
            {
                Destroy(instance.realAgents[key]);
                instance.realAgents.Remove(key);
            }

            instance.realAgents = new Dictionary<int, GameObject>();

            instance.frameNumber = 1;

            //Read the output file resulted from the video
            string file = txtPath;
            Debug.Log("Name of the output file:" + file);
            instance.frame_track_info = System.IO.File.ReadAllLines(@file);
            Debug.Log("Number of tracking info loaded: " + frame_track_info.Length);

            //Start the video
            instance.video = GameObject.Find("MainCamera").GetComponent<MyVideoPlayer>();
            instance.video.StartVideo(videoPath);

            //     instance.magnificationMultiplier = 1;

            //Load the pedestrian model
            instance.model = Resources.Load("ProjectedAgent", typeof(GameObject)) as GameObject;

            instance.camera = GameObject.Find("MainCamera");
            instance.viewPortScale = instance.camera.transform.GetComponent<Camera>().rect.width;

            instance.visibility = true;
            instance.linesRead = 0;
            instance.isRunning = false;

            //Height related
            instance.heightFrames = 0;
            instance.proposedHeights = new List<float>();
            instance.pedestrianHeightValid = false;

        }

        //Reads and returns the float representation of the given string
        float floatFromText(string coord, bool convert)
        {
            return float.Parse(coord, CultureInfo.InvariantCulture);// / (convert ? instance.magnificationMultiplier : 1);
        }

        //Returns the feet location of given origin of the detector, also reverses the y coordinate system
        float feetAdjuster(string origin, string distance)
        {
            return (float)instance.resY - ((floatFromText(origin, false) + (floatFromText(distance, false))) /*/ instance.magnificationMultiplier*/);
        }

        //Generates the ray for pixel at x and y
        Ray rayGenerator(float x, float y)
        {
            return Camera.main.ScreenPointToRay(new Vector3(x / (instance.resX) * Screen.width * instance.viewPortScale, y / (instance.resY) * Screen.height * instance.viewPortScale, 0));
        }

        //Generates the velocity from given position and velocity information 
        Vector3 velocityGenerator(string[] input, Vector3 origin)
        {
            //Was using velocity data from output file, which was wrong
            //  Vector3 vel = new Vector3(floatFromText(input[index + 3], true), 0, -floatFromText(input[index + 4], true));
            //vel = vel + new Vector3(floatFromText(input[index + 1], true), 0, feetAdjuster(input[index + 2], input[index + 6]));


            //Frame info: FRAMENUM, AGENTID, UL_POSX, UL_POSY, WIDTH, HEIGHT
            Vector3 vel = new Vector3(floatFromText(input[2], true) + floatFromText(input[4], true) / 2, 0, feetAdjuster(input[3], input[5]));

            //Project the vel + pos (which is proposed position)
            //Ray velRay = Camera.main.ScreenPointToRay(new Vector2(agentVelocity.x / (resX) * Screen.width, ((float)resY - ((agentVelocity.z - float.Parse(output[index + 6], CultureInfo.InvariantCulture) / 2) / magnificationMultiplier)) / resY * Screen.height));
            Ray velRay = rayGenerator(vel.x, vel.z);
            RaycastHit hit;
            Physics.Raycast(velRay, out hit, distance);

            //Get the projected velocity (projected endpoint - origin)
            vel = new Vector3(hit.point.x, hit.point.y/* + model.transform.lossyScale.y * 4*/, hit.point.z) - (origin);
            // vel = new Vector3(-vel.z, vel.y,vel.x);
            return vel;
        }

        /*
         * This method verifies the size of the detector, by inversely relating it to its distance from camera.
         * The width of the detector is not important, but the height is used. The angle between the top of the detector and the camera
         * (which is the angle created by the ray that hits the plane) should be between certain angles 
          * 
         */

        const float minAngle = 15.0f;
        const float maxAngle = 60.0f;

        bool verifyDetectorSize(String height, RaycastHit hit)
        {
            //Find the angle ray makes with camera
            float angle = (float)(180 * Math.Asin(Math.Abs(instance.camera.transform.position.y - hit.point.y)
                / (Vector3.Distance(instance.camera.transform.position, hit.point))) / Math.PI);
            //Debug.Log("The angle is " + angle);

            //Angles are subject to change (I am thinking of relating them to the positioning of the camera)
            return (minAngle < angle) && (angle < maxAngle);
            //return (floatFromText(width, true) * floatFromText(height, true)) * hit.distance >= 250000; //Old size comparator
        }

        /* 
         * Starting from first frame, on each frame passed in game, update the agents 
         * Each time:
         *  - Project pedestrians if they have not been added before
         *  - Check their location each frame, for any misdetections
         *  - Update their velocity according to given output file 
         *  
         */
        public void Step()
        {

            if (!instance.isRunning)
            {

                int[] res = instance.video.retrieveResolution();
                //If the resolution is not readable, it means the video is not loaded yet
                if (res[0] != 0)
                {
                    instance.isRunning = true;
                    instance.resX = res[0];
                    instance.resY = res[1];
                    Debug.Log("The loaded video's resolution is " + instance.resX + "X" + instance.resY);
                }
            }
            else
            {
                //If the pedestrian height is found, determine speed as 1/4 of it
                if (pedestrianHeight != 0.0f)
                {
      
                }

                //Update positions of each projected agent
                List<int> keys = instance.realAgents.Keys.ToList();

                foreach (int key in keys)
                {
                    instance.realAgents[key].GetComponent<RVO.ProjectedAgent>().Step();
                }

                //If there are still frames to process
                while (instance.linesRead < instance.frame_track_info.Length)
                {
                    //Current Frame info: FRAMENUM, AGENTID, UL_POSX, UL_POSY, WIDTH, HEIGHT
                    string[] output = instance.frame_track_info[instance.linesRead].Split(',');

                    if (int.Parse(output[0]) != instance.frameNumber)
                    {
                        break;
                    }

                    float rayX = floatFromText(output[2], true) + floatFromText(output[4], true) / 2;
                    float rayY = feetAdjuster(output[3], output[5]);
                    Ray ray = rayGenerator(rayX, rayY);

                    RaycastHit hit;
                    int readId = int.Parse(output[1]);
                    GameObject tryOutput; //Used for trygetvalue method

                    //New agent
                    if (Physics.Raycast(ray, out hit, distance, 1 << LayerMask.NameToLayer("navigationArea")) 
                        && !instance.realAgents.TryGetValue(readId, out tryOutput) 
                        && verifyDetectorSize(output[5], hit)) //New agent
                    {

                        //Hold the reference to the newly created agent
                        Vector3 agentPos = new Vector3(hit.point.x, hit.point.y /*+ 3.5f * instance.model.transform.localScale.y*/, hit.point.z);

                        //Whenever camera changes, height becomes invalid.
                        if (!instance.pedestrianHeightValid)
                        {
                            //Height inference from detection box
                            //Find the scalar projection of the ray onto the non-y distance from camera to feet location, then use its ratio to normalized distance
                            //to lead the ray to head pos in 3d
                            Ray head_ray = rayGenerator(rayX, rayY + float.Parse(output[5]));

                            Vector3 proj_dist = agentPos - new Vector3(instance.camera.transform.position.x, hit.point.y, instance.camera.transform.position.z); ;
                            Vector3 head = head_ray.direction * (proj_dist.sqrMagnitude / (Vector3.Dot(head_ray.direction, proj_dist))) + instance.camera.transform.position;

                            float height = Vector3.Distance(head, agentPos);
                            instance.proposedHeights.Add(height);

                        }

                        Vector3 agentVelocity = velocityGenerator(output, agentPos);
                        // Vector3 agentVelocity = Vector3.zero;

                        Quaternion initialOrientation = Quaternion.LookRotation(agentVelocity);
                        initialOrientation.z = 0;
                        initialOrientation.x = 0;
                        newAgent = (GameObject)Instantiate(instance.model, agentPos /*+ Vector3.up * 3.5f * instance.model.transform.localScale.y*/, initialOrientation);
                        newAgent.transform.localScale += Vector3.one * ((instance.PedestrianHeight == 0.0f) ? 0 : (instance.PedestrianHeight / instance.model.GetComponent<CapsuleCollider>().height) - 1);

                        int agentId; //seperate from the readId, as that is used for tracking from the output file, while this is used for tracking in RVO
                        RVO.Vector2 origin = new Vector2(agentPos.x, agentPos.z);
                        RVO.Agent agentReference = Simulator.Instance.addIrresponsiveAgent(origin * RVOMagnify.magnify); //TODO: RVOmagnifiy

                        //Modify the agent's parameters for its management.
                        newAgent.GetComponent<ProjectedAgent>().createAgent(agentVelocity, readId, agentReference.id_, agentReference);

                        instance.realAgents.Add(readId, newAgent);
                        newAgent.GetComponent<ProjectedAgent>().IsSync = true;


                    }

                    /* If the agent already exists in the dictionary, we need to assess its current state.
                     * The detector which is attached to the agent might not be valid, best way to detect that is to 
                     * check its velocity. It might change its position MUCH more than its initial velocity if it is invalid.This will indicate either
                     *      - Agent left the area but its detector is now attached to someone else using the same ID
                     *      - The detector made an error
                     *  If it is valid, update its velocity
                     */
                    else if (Physics.Raycast(ray, out hit, distance, 1 << 9) && instance.realAgents.TryGetValue(readId, out tryOutput))
                    {
                        Vector3 proposedPos = new Vector3(hit.point.x, hit.point.y /*+ 3.5f * instance.model.transform.localScale.y*/, hit.point.z);
                        GameObject checkedAgent = instance.realAgents[readId];
                        Vector3 checkedVelocity = proposedPos - checkedAgent.GetComponent<ProjectedAgent>().Pos;
                        if (checkedAgent != null)
                        {

                            if (Vector3.Distance(checkedAgent.GetComponent<ProjectedAgent>().Pos, proposedPos) > velocityDifferenceThreshold
                                /*checkedVelocity.magnitude > velocityDifferenceThreshold*/)
                            {
                                //Get rid of the agent

                                Debug.Log("Agent with id " + readId + " is destroyed");

                                instance.removeAgent(readId, checkedAgent);
                            }
                            else //Update the velocity
                            {
                                checkedAgent.GetComponent<ProjectedAgent>().Velocity = checkedVelocity;
                                checkedAgent.GetComponent<ProjectedAgent>().IsSync = true;
                            }

                        }

                    }

                    instance.linesRead++;
                    instance.heightFrames++;

                    if (instance.linesRead >= instance.frame_track_info.Length)
                        instance.isRunning = false;



                }

                //Update the height if it is invalid, like a change in the camera. As the height is determined according to the navigable area, the
                //positioning of the camera is very crucial for its calculation to be accurate.
                if (instance.proposedHeights.Count > 0 && instance.heightFrames >= 10)
                {
                    instance.PedestrianHeight = instance.proposedHeights.Average();
                    Debug.Log("Height (re)calculated as :" + instance.PedestrianHeight);
                    instance.proposedHeights.Clear();
                    instance.pedestrianHeightValid = true;
                    GameObject.Find("HeightField").GetComponent<InputField>().text = "" + instance.PedestrianHeight;

                    instance.speedLimit = instance.pedestrianHeight / 4.0f;
                    AgentBehaviour.Instance.changeStartingSpeed(instance.speedLimit);

                }

                instance.frameNumber++;

            }


        }

        public void resetHeights(float height)
        {
            instance.pedestrianHeightValid = false;
            instance.heightFrames = 0;
            if (height > 0)
            {
                instance.pedestrianHeightValid = true;
                instance.PedestrianHeight = height;
                
            }
            GameObject.Find("HeightField").GetComponent<InputField>().text = "" + instance.PedestrianHeight;

        }

        /*
         * Remove the agent from the simulation
         */
        internal void removeAgent(int agentId, GameObject agent)
        {
            RVO.Simulator.Instance.agents_.Remove(agent.GetComponent<ProjectedAgent>().AgentReference);
            instance.realAgents.Remove(agentId);
            Destroy(agent);
            Simulator.Instance.SetNumWorkers(Simulator.Instance.GetNumWorkers());
        }
    }
}