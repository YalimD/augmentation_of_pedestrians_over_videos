using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;

/*
 * 
 * Agent Behaviour code extends the simulation code provided by RVO for the
 * spawn and management of the agents. It will also consider the projected agents coming from
 * the Pedestrian Projection class. But they won't have the change in velocity, only their velocities 
 * will be modified
 * 
 * This class manages the rvo by having an instance for its simulation logic. It creates artificial agents (Each has an agent reference) 
 * unless there are certain number of them wandering on the screen already. TODO: THIS MUST BE AN OPTION, HOW CROWDED THE USER WANTS THE SCREEN
 * 
 * This class communicates with Pedestrian Projection class for the managemtn of ProjectedAgents (Each has an agent reference)
 * It will cue the Pedestrian Projection to spawn and move the projected agents. This way, both agent types
 * will be managed from a single source, meaning better control. 
 * 
 * The associated Agent references are managed by their own objects, therefore a centralized control SHOULDN'T be required for RVO.
 * 
 * The option to define goal for each artificial agent is also done in their own respective class.
 * 
 */

namespace RVO
{
    public sealed class AgentBehaviour : MonoBehaviour
    {

        private string[] videoFiles;

        //temporary vector for holding the goal locations
        private List<Vector2> goals;

        public static AgentBehaviour Instance { get; } = new AgentBehaviour();

        private List<GameObject> agentModels;
        private List<GameObject> artificialAgents;

        public List<GameObject> ArtificialAgents { get { return Instance.artificialAgents; } }

        public int NumOfAgents { get { return Instance.artificialAgents.Count; } }

        //OPTIONS FOR RVO ABOUT THE AGENTS
        public struct DefaultRVO
        {
            public const int defaultNumOfNeighbours = 10;
            public const float defaultNeighbourRange = 12f;
            public const float defaultMaxSpeed = 4f;
            public const float defaultReactionSpeed = 500000f;
            public const float defaultAgentRange = 2f;
        }

        public int NumOfNeighboursConsidered { get; set; }
        public float NeighbourRange { get; set; }
        public float MaxSpeed { get; set; }

        public float ReactionSpeed { get; set; }

        public float AgentRadius { get; set; }

        //Are artificial agents visible to user ?
        private bool visibility;
        public bool Visibility
        {
            set { Instance.visibility = value; }
            get { return Instance.visibility; }
        }

        private int AAcollision;
        public int getAACollision() { return AAcollision; }

        public void IncrementArtificialCollision()
        {
            lock (Instance)
            {
                Instance.AAcollision++;
            }
        }

        private int APcollision;
        public int getAPCollision() { return APcollision; }

        public void IncrementProjectedCollision()
        {
            lock (Instance)
            {
                Instance.APcollision++;
            }
        }

        Camera cam;

        void Start()
        {
            Instance.cam = transform.GetComponent<Camera>();
            LoadVideo();
        }

        // Initialize the artificial agents on the area.Assign goals randomly at a radius around the center of the square
        public void LoadVideo()
        {
            //Load the video, the related txt file and the mesh first
            string[] content_paths = LoadFiles();

            foreach (string content in content_paths)
            {
                Debug.Log(content);
            }

            //Also, wait for the mesh correction (and creating a navMesh for it)
            Instance.loadMesh();
            Instance.PlaceCamera();
            Instance.videoFiles = content_paths;
            Instance.InstantiateSimulation(); //This won't use the mesh data
        }

        private void PlaceCamera()
        {
            string c_path = "";
            c_path = EditorUtility.OpenFilePanel("Load the camera placement file (.txt)", "", "txt");

            //If camera placement is not provided, exit the program
            if (c_path.Length == 0)
            {
                Application.Quit();
            }

            CameraPlacement.placeCameras(c_path, Instance.cam);
            Debug.Log("Camera is placed");
        }

        public GameObject NavigableArea { get; private set; }

        //Load the mesh from the selected obj file (or any file that is compatible with the Unity), move the file to the assets folder and place it onto the
        //(0,0,0) position in the Unity 3D space. Uses the explorer to select a .obj file.
        public void loadMesh()
        {
            string m_path = "";
            m_path = EditorUtility.OpenFilePanel("Load the mesh file (.obj) to be placed", "", "obj");

            if (m_path.Length == 0)
            {
                Application.Quit();
            }

            Instance.NavigableArea = MeshPlacement.placeMesh(m_path);

            if (Instance.NavigableArea == null)
            {
                Application.Quit();
            }
        }

        //Load a Video, associated text file (In case one fails, return false)
        public string[] LoadFiles()
        {
            string v_path = "";
            v_path = EditorUtility.OpenFilePanel("Load a video to be added to background (Prefably mp4)", "", "mp4");

            if (v_path.Length == 0)
                Application.Quit();

            string t_path = "";
            t_path = EditorUtility.OpenFilePanel("Load the txt file associated to the pedestrians to be simulated", "", "txt");

            if (t_path.Length == 0)
                Application.Quit();

            string[] files = { v_path, t_path };

            return files;
        }

        public void Restart()
        {
            while (Instance.artificialAgents.Count > 0)
            {
                Debug.Log("Deleted agent");
                Instance.RemoveAgent(Instance.artificialAgents[0]);
            }

            Instance.InstantiateSimulation();
        }

        //Clear the simulation, add agents and define goals
        void InstantiateSimulation()
        {
            PedestrianProjection.Instance.InitiateProjection(Instance.videoFiles[0], Instance.videoFiles[1]);

            //Initiate agent models
            Instance.agentModels = new List<GameObject>();
            Instance.agentModels.Add(Resources.Load("AgentChuan", typeof(GameObject)) as GameObject);
            Instance.agentModels.Add(Resources.Load("AgentJenna", typeof(GameObject)) as GameObject);

            // instance.agentModels.Add(Resources.Load("AgentDavid", typeof(GameObject)) as GameObject);
            // instance.agentModels.Add(Resources.Load("AgentAngelica", typeof(GameObject)) as GameObject);

            Instance.artificialAgents = new List<GameObject>();
            ArtificialAgent.selectedMat = Resources.Load("Selected", typeof(Material)) as Material;
            Instance.Visibility = true;

            Instance.AAcollision = 0;
            Instance.APcollision = 0;

            Simulator.Instance.Clear();

            /* Specify the global time step of the simulation. */
            Simulator.Instance.setTimeStep(1f);

            ResetRVOAgentDefaults();
        }

        public void ResetRVOAgentDefaults()
        {
            //Wait until pedestrian heights is calculated
            if (!PedestrianProjection.Instance.PedestrianHeightValid)
            {
                //We shouldn't see this
                Debug.LogError("Height not ready yet");
            }

            Instance.NeighbourRange = DefaultRVO.defaultNeighbourRange * PedestrianProjection.Instance.PedestrianHeight;
            Instance.NumOfNeighboursConsidered = DefaultRVO.defaultNumOfNeighbours;
            Instance.ReactionSpeed = DefaultRVO.defaultReactionSpeed;
            Instance.MaxSpeed = DefaultRVO.defaultMaxSpeed * (PedestrianProjection.Instance.PedestrianHeight / 10);
            Instance.AgentRadius = DefaultRVO.defaultAgentRange * PedestrianProjection.Instance.PedestrianHeight;

            Debug.Log("Speed is" + Instance.MaxSpeed);

            Simulator.Instance.setAgentDefaults(Instance.NeighbourRange,
                                    Instance.NumOfNeighboursConsidered,
                                    Instance.ReactionSpeed,
                                    10.0f,
                                    Instance.AgentRadius,
                                    Instance.MaxSpeed,
                                    new Vector2(0.0f, 0.0f));
        }

        // Update the RVO simulation 
        public void Step()
        {
            //I stop and resume the navmeshagent part as it causes ossilation between navmesh velocity and rvo velocity
            foreach (GameObject ag in Instance.artificialAgents)
            {
                ag.GetComponent<ArtificialAgent>().setPreferred();
            }

            //Apply the step for determining the required velocity for each agent on the move
            Simulator.Instance.doStep();

            foreach (GameObject ag in Instance.artificialAgents)
            {
                ag.GetComponent<ArtificialAgent>().updateVelo();
                //     ag.GetComponent<UnityEngine.AI.NavMeshAgent>().isStopped = false; //Resume so that the velocity of the path is recalculated
                ag.GetComponent<ArtificialAgent>().Step();
            }

            //   Debug.Log(Simulator.Instance.getNumAgents() + "with workers" + Simulator.Instance.GetNumWorkers());
        }


        //Add the agent to given position in the space
        public void AddAgent(Vector3 pos)
        {
            //I don't know why, but when the simulation starts, any additional agent is ignored, clearing the workers seem
            // prevent that

            Simulator.Instance.SetNumWorkers(Simulator.Instance.GetNumWorkers());
            Simulator.Instance.setTimeStep(1f);

            //Initiate the agent properties, these will also help us modify the agent behavior using the RVO simulation
            // Simulator.Instance.setAgentDefaults(instance.neighbourRange, instance.numOfNeighboursConsidered, instance.reactionSpeed, 10.0f, 0.5f * RVOMagnify.magnify, instance.maxSpeed, new Vector2(0.0f, 0.0f));

            Vector2 origin = new Vector2(pos.x, pos.z);

            GameObject newArtAgent = (GameObject)Instantiate(Instance.agentModels[(int)Math.Floor(UnityEngine.Random.value * Instance.agentModels.Count)], new Vector3(origin.x_, pos.y, origin.y_), new Quaternion());

            //TODO: ADJUST RVO AGENT ACCORDING TO SCALE
            newArtAgent.transform.localScale += Vector3.one * ((PedestrianProjection.Instance.PedestrianHeight == 0.0f) ? 0 : (PedestrianProjection.Instance.PedestrianHeight / newArtAgent.transform.GetComponent<CapsuleCollider>().height) - 1);

            //Initialize the RVO part of the agent by connecting the reference to the
            //related new agent
            Agent agentReference = Simulator.Instance.addResponsiveAgent(origin * RVOMagnify.Magnify);

            newArtAgent.GetComponent<ArtificialAgent>().createAgent(agentReference.id_, agentReference);

            Instance.artificialAgents.Add(newArtAgent);
        }

        internal void RemoveAgent(GameObject agent)
        {
            RVO.Simulator.Instance.agents_.Remove(agent.GetComponent<ArtificialAgent>().AgentReference);
            Instance.artificialAgents.Remove(agent);
            Destroy(agent);
            Simulator.Instance.SetNumWorkers(Simulator.Instance.GetNumWorkers());
        }
    }
}