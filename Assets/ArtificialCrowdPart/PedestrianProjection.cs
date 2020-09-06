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
 */

namespace RVO
{
    public sealed class PedestrianProjection : MonoBehaviour
    {

        public static PedestrianProjection Instance { get; } = new PedestrianProjection();

        //Speed limit for projected agents; used against false positives. Value determined according to pedestrian height
        private float speedLimit = 100f;
        public float SpeedLimit { get { return speedLimit; } set { speedLimit = value; } }

        //CONSTANTS

        //Threshold for velocity, used for validating the detection. If the velocity of the detected pedestrian exceeds this, it is considered as a noise
        //in detection system
        private const float velocityDifferenceThreshold = 40f;
        private const float distance = 100000.0F; //Creating distance from camera

        //VARIABLES

        private bool isRunning;
        public bool IsSimulationRunning { get { return Instance.isRunning; } }

        //Are projected agents visible to user ?
        private bool visibility;
        public bool Visibility { set { Instance.visibility = value; } get { return Instance.visibility; } }

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
        public Dictionary<int, GameObject> RealAgents { get { return Instance.realAgents; } }
        public int NumOfRealAgents { get { return Instance.realAgents.Count; } }

        private GameObject model; //Dummy for agents
        private GameObject newAgent; //Reference to the created agent

        MyVideoPlayer video;
        GameObject camera;

        private List<float> proposedHeights;
        private int heightFrames;
        public bool PedestrianHeightValid { get; set; }
        public float PedestrianHeight { get; set; } = 0.0f;

        //The execution starts here (Also system is restarted from here upon call)
        public void InitiateProjection(string videoPath, string txtPath)
        {

            //Delete all agents in the simulation if the simulation has been restarted
            List<int> keys = Instance.realAgents.Keys.ToList();

            foreach (int key in keys)
            {
                Destroy(Instance.realAgents[key]);
                Instance.realAgents.Remove(key);
            }

            Instance.realAgents = new Dictionary<int, GameObject>();

            Instance.frameNumber = 1;

            //Read the output file resulted from the video
            string file = txtPath;
            Debug.Log("Name of the output file:" + file);
            Instance.frame_track_info = System.IO.File.ReadAllLines(@file);
            Debug.Log("Number of tracking info loaded: " + frame_track_info.Length);

            //Start the video
            Instance.video = GameObject.Find("MainCamera").GetComponent<MyVideoPlayer>();
            Instance.video.StartVideo(videoPath);

            // Handle scene light based on video name, use the default light otherwise
            string videoName = System.IO.Path.GetFileNameWithoutExtension(videoPath);

            Transform[] trs = GameObject.Find("lights").GetComponentsInChildren<Transform>(true);
            bool foundVideo = false;

            foreach (Transform t in trs)
            {
                if (t.name == videoName)
                {
                    t.gameObject.SetActive(true);
                    foundVideo = true;
                }
            }

            if (!foundVideo)
            {
                foreach (Transform t in trs)
                {
                    if (t.name == "sun")
                    {
                        t.gameObject.SetActive(true);
                    }
                }
            }

            //     instance.magnificationMultiplier = 1;

            //Load the pedestrian model
            Instance.model = Resources.Load("ProjectedAgent", typeof(GameObject)) as GameObject;

            //TODO: I dont think we need this
            Instance.camera = GameObject.Find("MainCamera");
            Instance.viewPortScale = Instance.camera.transform.GetComponent<Camera>().rect.width;

            Instance.visibility = true;
            Instance.linesRead = 0;
            Instance.isRunning = false;

            //Height related
            Instance.heightFrames = 0;
            Instance.proposedHeights = new List<float>();
            Instance.PedestrianHeightValid = false;

        }
        
        //Reads and returns the float representation of the given string
        float FloatFromText(string coord, bool convert)
        {
            return float.Parse(coord, CultureInfo.InvariantCulture);
        }

        //Returns the feet location of given origin of the detector, also reverses the y coordinate system
        float FeetAdjuster(string origin, string distance)
        {
            return (float)Instance.resY - ((FloatFromText(origin, false) + (FloatFromText(distance, false))));
        }

        //Generates the ray for pixel at x and y
        Ray RayGenerator(float x, float y)
        {
            return Camera.main.ScreenPointToRay(new Vector3(x / (Instance.resX) * Screen.width * Instance.viewPortScale, y / (Instance.resY) * Screen.height * Instance.viewPortScale, 0));
        }

        //Generates the velocity from given position and velocity information 
        Vector3 VelocityGenerator(string[] input, Vector3 origin)
        {
            //Frame info: FRAMENUM, AGENTID, UL_POSX, UL_POSY, WIDTH, HEIGHT
            Vector3 vel = new Vector3(FloatFromText(input[2], true) + FloatFromText(input[4], true) / 2, 0, FeetAdjuster(input[3], input[5]));

            //Project the vel + pos (which is proposed position)
            //Ray velRay = Camera.main.ScreenPointToRay(new Vector2(agentVelocity.x / (resX) * Screen.width, ((float)resY - ((agentVelocity.z - float.Parse(output[index + 6], CultureInfo.InvariantCulture) / 2) / magnificationMultiplier)) / resY * Screen.height));
            Ray velRay = RayGenerator(vel.x, vel.z);
            RaycastHit hit;
            Physics.Raycast(velRay, out hit, distance);

            //Get the projected velocity (projected endpoint - origin)
            vel = new Vector3(hit.point.x, hit.point.y/* + model.transform.lossyScale.y * 4*/, hit.point.z) - (origin);
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

        bool VerifyDetectorSize(String height, RaycastHit hit)
        {
            //Find the angle ray makes with camera
            float angle = (float)(180 * Math.Asin(Math.Abs(Instance.camera.transform.position.y - hit.point.y)
                / (Vector3.Distance(Instance.camera.transform.position, hit.point))) / Math.PI);

            //Angles are subject to change (I am thinking of relating them to the positioning of the camera)
            return (minAngle < angle) && (angle < maxAngle);
        }

        /* 
         * Starting from first frame, on each frame passed in game, update the agents 
         * Each time:
         *  - Project pedestrians if they have not been added before
         *  - Check their location each frame, for any misdetections
         *  - Update their velocity according to given output file 
         *  - Return if step is successfully taken
         */
        public bool Step()
        {
            if (!Instance.isRunning)
            {
                int[] res = Instance.video.RetrieveResolution();
                //If the resolution is not readable, it means the video is not loaded yet
                if (res[0] != 0)
                {
                    Instance.isRunning = true;
                    Instance.resX = res[0];
                    Instance.resY = res[1];
                    Debug.Log("The loaded video's resolution is " + Instance.resX + "X" + Instance.resY);
                }

                return false;
            }
            else
            {
                //Update positions of each projected agent
                List<int> keys = Instance.realAgents.Keys.ToList();

                foreach (int key in keys)
                {
                    Instance.realAgents[key].GetComponent<ProjectedAgent>().Step();
                }

                //If there are still frames to process
                while (Instance.linesRead < Instance.frame_track_info.Length)
                {
                    //Current Frame info: FRAMENUM, AGENTID, UL_POSX, UL_POSY, WIDTH, HEIGHT
                    string[] output = Instance.frame_track_info[Instance.linesRead].Split(',');

                    if (int.Parse(output[0]) != Instance.frameNumber)
                    {
                        break;
                    }

                    float rayX = FloatFromText(output[2], true) + FloatFromText(output[4], true) / 2;
                    float rayY = FeetAdjuster(output[3], output[5]);
                    Ray ray = RayGenerator(rayX, rayY);

                    RaycastHit hit;
                    int readId = int.Parse(output[1]);
                    GameObject tryOutput; //Used for trygetvalue method

                    //New agent
                    if (Physics.Raycast(ray, out hit, distance, 1 << LayerMask.NameToLayer("navigationArea"))
                        && !Instance.realAgents.TryGetValue(readId, out tryOutput)
                        && VerifyDetectorSize(output[5], hit)) //New agent
                    {

                        //Hold the reference to the newly created agent
                        Vector3 agentPos = new Vector3(hit.point.x, hit.point.y /*+ 3.5f * instance.model.transform.localScale.y*/, hit.point.z);

                        //Whenever camera changes, height becomes invalid.
                        if (!Instance.PedestrianHeightValid)
                        {
                            //Height inference from detection box
                            //Find the scalar projection of the ray onto the non-y distance from camera to feet location, then use its ratio to normalized distance
                            //to lead the ray to head pos in 3d
                            Ray head_ray = RayGenerator(rayX, rayY + float.Parse(output[5]));

                            Vector3 proj_dist_vector = agentPos - new Vector3(Instance.camera.transform.position.x, hit.point.y, Instance.camera.transform.position.z);
                            Vector3 head = head_ray.direction * (proj_dist_vector.sqrMagnitude / (Vector3.Dot(head_ray.direction, proj_dist_vector))) + Instance.camera.transform.position;

                            float height = Vector3.Distance(head, agentPos);
                            Instance.proposedHeights.Add(height);

                        }

                        Vector3 agentVelocity = VelocityGenerator(output, agentPos);

                        Quaternion initialOrientation = Quaternion.LookRotation(agentVelocity);
                        initialOrientation.z = 0;
                        initialOrientation.x = 0;
                        newAgent = (GameObject)Instantiate(Instance.model, agentPos, initialOrientation);
                        newAgent.transform.localScale += Vector3.one * ((Instance.PedestrianHeight == 0.0f) ? 0 : (Instance.PedestrianHeight / Instance.model.GetComponent<CapsuleCollider>().height) - 1);

                        int agentId; //seperate from the readId, as that is used for tracking from the output file, while this is used for tracking in RVO
                        Vector2 origin = new Vector2(agentPos.x, agentPos.z);
                        Agent agentReference = Simulator.Instance.addIrresponsiveAgent(origin /* * instance.PedestrianHeight*/ * RVOMagnify.Magnify); //TODO: RVOmagnifiy

                        //Modify the agent's parameters for its management.
                        newAgent.GetComponent<ProjectedAgent>().createAgent(agentVelocity, readId, agentReference.id_, agentReference);

                        Instance.realAgents.Add(readId, newAgent);
                        newAgent.GetComponent<ProjectedAgent>().IsSync = true;


                    }

                    /* If the agent already exists in the dictionary, we need to assess its current state.
                     * The detector which is attached to the agent might not be valid, best way to detect that is to 
                     * check its velocity. It might change its position MUCH more than its initial velocity if it is invalid.This will indicate either
                     *      - Agent left the area but its detector is now attached to someone else using the same ID
                     *      - The detector made an error
                     *  If it is valid, update its velocity
                     */
                    else if (Physics.Raycast(ray, out hit, distance, 1 << 9) && Instance.realAgents.TryGetValue(readId, out tryOutput))
                    {
                        Vector3 proposedPos = new Vector3(hit.point.x, hit.point.y /*+ 3.5f * instance.model.transform.localScale.y*/, hit.point.z);
                        GameObject checkedAgent = Instance.realAgents[readId];
                        Vector3 checkedVelocity = proposedPos - checkedAgent.GetComponent<ProjectedAgent>().Pos;
                        if (checkedAgent != null)
                        {

                            if (Vector3.Distance(checkedAgent.GetComponent<ProjectedAgent>().Pos, proposedPos) > velocityDifferenceThreshold
                                /*checkedVelocity.magnitude > velocityDifferenceThreshold*/)
                            {
                                //Get rid of the agent

                                Debug.Log("Agent with id " + readId + " is destroyed");

                                Instance.removeAgent(readId, checkedAgent);
                            }
                            else //Update the velocity and rotation
                            {
                                checkedAgent.GetComponent<ProjectedAgent>().Velocity = checkedVelocity;
                                checkedAgent.GetComponent<ProjectedAgent>().IsSync = true;
                            }

                        }

                    }

                    Instance.linesRead++;
                    Instance.heightFrames++;

                    if (Instance.linesRead >= Instance.frame_track_info.Length)
                    {
                        Instance.isRunning = false;

                        //If no pedestrian left, validate height
                        Instance.PedestrianHeightValid = true;

                        if (Instance.PedestrianHeight == 0.0f)
                            Instance.PedestrianHeight = 10f; //Default
                    }

                }

                //Update the height if it is invalid, like a change in the camera. As the height is determined according to the navigable area, the
                //positioning of the camera is very crucial for its calculation to be accurate.
                if (Instance.proposedHeights.Count > 0 && Instance.heightFrames >= 10)
                {
                    Instance.PedestrianHeight = Instance.proposedHeights.Average();
                    Debug.Log("Height (re)calculated as :" + Instance.PedestrianHeight);
                    Instance.proposedHeights.Clear();
                    Instance.PedestrianHeightValid = true;
                    GameObject.Find("HeightField").GetComponent<InputField>().text = "" + Instance.PedestrianHeight;

                    AgentBehaviour.Instance.ResetRVOAgentDefaults();
                    Instance.SpeedLimit = AgentBehaviour.DefaultRVO.defaultMaxSpeed * Instance.PedestrianHeight / 2;
                }

                Instance.frameNumber++;
            }

            return true;

        }

        public void resetHeights(float height)
        {
            Instance.PedestrianHeightValid = false;
            Instance.heightFrames = 0;
            if (height > 0)
            {
                Instance.PedestrianHeightValid = true;
                Instance.PedestrianHeight = height;
                AgentBehaviour.Instance.ResetRVOAgentDefaults();

            }

            GameObject.Find("HeightField").GetComponent<InputField>().text = "" + Instance.PedestrianHeight;
        }

        /*
         * Remove the agent from the simulation
         */
        internal void removeAgent(int agentId, GameObject agent)
        {
            Simulator.Instance.agents_.Remove(agent.GetComponent<ProjectedAgent>().AgentReference);
            Instance.realAgents.Remove(agentId);
            Destroy(agent);
            Simulator.Instance.SetNumWorkers(Simulator.Instance.GetNumWorkers());
        }
    }
}