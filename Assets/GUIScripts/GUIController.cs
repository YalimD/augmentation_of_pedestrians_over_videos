using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

/*
 * Written by Yalım Doğan
 * 
 * This class manages the GUI controls on RVO, selection of artificial agents etc.
 */

public class GUIController : MonoBehaviour
{
    Button add, remove;
   // InputField neighbours; //TODO: Turn this into a text field
    Transform agentCount;
    Text numOfSelected, totalArtificial, totalProjected, totalAgents;
    ColorBlock cbAdd, cbRemove;
    //Modes for adding/removing artificial agents
    /* 0 - idle
     * 1 - Add
     * 2- Remove
     */
    private int currentMode = 0;

    //Currently selected agents
    private List<RVO.ArtificialAgent> selectedAgents;

    //Add agent mode
    public void addAgentMode()
    {
        if (currentMode == 0)
        {
            currentMode = 1;
            cbAdd.normalColor = Color.yellow;
            cbAdd.highlightedColor = Color.yellow;
            add.colors = cbAdd;
            remove.interactable = false;
        }
        else
        {
            cbAdd.normalColor = Color.white;
            cbAdd.highlightedColor = Color.white;
            add.colors = cbAdd;
            remove.interactable = true;
            currentMode = 0;
        }
    }

    //Remove agent mode
    //Also deletes any already selected agent
    public void removeAgentMode()
    {
        if (currentMode == 0)
        {
            currentMode = 2;
            cbRemove.normalColor = Color.yellow;
            cbRemove.highlightedColor = Color.yellow;
            remove.colors = cbRemove;
            add.interactable = false;

            deleteSelectedAgents();

        }
        else
        {
            cbRemove.normalColor = Color.white;
            cbRemove.highlightedColor = Color.white;
            remove.colors = cbRemove;
            add.interactable = true;
            currentMode = 0;
        }

       
    }


    /*
    public void toogleArtificialManagement(int mode)
    {
        if (currentMode == mode)
        {
            cbAdd.normalColor = Color.white;
            cbAdd.highlightedColor = Color.white;
            add.colors = cbAdd;
            add.interactable = true;

            cbRemove.normalColor = Color.white;
            cbRemove.highlightedColor = Color.white;
            remove.colors = cbRemove;
            remove.interactable = true;

            currentMode = 0;

        }
        else
        {
            currentMode = mode;
            switch (currentMode)
            { 
                case 1:
                    cbAdd = add.colors;
                    cbAdd.normalColor = Color.yellow;
                    cbAdd.highlightedColor = Color.yellow;
                    add.colors = cbAdd;
                    remove.interactable = false;
                    break;
                case 2:
                    cbRemove = remove.colors;
                    cbRemove.normalColor = Color.yellow;
                    cbRemove.highlightedColor = Color.yellow;
                    remove.colors = cbRemove;
                    add.interactable = false;
                    break;

            }
        }
        //Depending on the current mode of the program, change the color of the related button.
    }
    */


    Transform collisionCounter;
    Text numOfCollisions, artificialToArtificialCollisions, artificialToProjectedCollisions;

    Button pauseButton;
    Text simRunning;

    InputField x_rot, y_rot, z_rot;
    InputField x_pos, y_pos, z_pos;
    InputField focalArea;
    InputField heightField;

    public GameObject mainCamera;//, backGroundCamera;
    

    void Start()
    {
        simRunning = transform.Find("VideoPlayer").Find("SimFinished").GetComponent<Text>();

        //Pause button which changes to Play when clicked
        pauseButton = transform.Find("VideoPlayer").Find("Pause").GetComponent<Button>();

        // Adding / Removing agents
        add = transform.Find("RVOControl").Find("AddArtificial").GetComponent<Button>();
        cbAdd = add.colors;
        remove = transform.Find("RVOControl").Find("RemoveArtificial").GetComponent<Button>();
        cbRemove = remove.colors;

       // neighbours = transform.Find("RVOControl").Find("Neighbours").Find("Slider").GetComponent<Slider>();

        selectedAgents = new List<RVO.ArtificialAgent>();

        agentCount = transform.Find("AgentCounter"); //Root agentCount object

        numOfSelected =  agentCount.transform.Find("NumOfSelectedArt").Find("Num").GetComponent<Text>();
        totalProjected = agentCount.transform.Find("Show Projected").Find("Num").GetComponent<Text>();
        totalArtificial = agentCount.transform.Find("Show Artificial").Find("Num").GetComponent<Text>();
        totalAgents = agentCount.transform.Find("Total").Find("Num").GetComponent<Text>();

        collisionCounter = transform.Find("CollisionCounter");
        numOfCollisions = collisionCounter.transform.Find("NumOfCollisions").Find("Text").GetComponent<Text>();
        artificialToArtificialCollisions = collisionCounter.transform.Find("ArtArt").Find("Text").GetComponent<Text>();
        artificialToProjectedCollisions = collisionCounter.transform.Find("ArtPro").Find("Text").GetComponent<Text>();

        mainCamera = GameObject.Find("MainCamera");

        saveButton = GameObject.Find("SaveVideo").GetComponentInChildren<Text>();
        saveStatus = GameObject.Find("SaveStatus").GetComponent<Text>();

        x_rot = GameObject.Find("X_Rot").GetComponent<InputField>();
        y_rot = GameObject.Find("Y_Rot").GetComponent<InputField>();
        z_rot = GameObject.Find("Z_Rot").GetComponent<InputField>();

        x_pos = GameObject.Find("X_Pos").GetComponent<InputField>();
        y_pos = GameObject.Find("Y_Pos").GetComponent<InputField>();
        z_pos = GameObject.Find("Z_Pos").GetComponent<InputField>();

        focalArea = GameObject.Find("FocalLength").GetComponent<InputField>();
        heightField = GameObject.Find("HeightField").GetComponent<InputField>();

    }

    //Called after initialized using the camera calibration file
    public void initiateCameraControls()
    {

        x_rot.text = mainCamera.transform.rotation.x + "";
        y_rot.text = mainCamera.transform.rotation.y + "";
        z_rot.text = mainCamera.transform.rotation.z + "";

        x_pos.text = mainCamera.transform.position.x + "";
        y_pos.text = mainCamera.transform.position.y + "";
        z_pos.text = mainCamera.transform.position.z + "";

        focalArea.text = CameraPlacement.FOVtoFocal(mainCamera.GetComponent<Camera>().fieldOfView,
           mainCamera.GetComponent<MyVideoPlayer>().retrieveResolution()[1]) + "";
        
    }
     
    public void pauseVideo()
    {
        if (Camera.main.GetComponent<MyVideoPlayer>().VideoPlaying)
        {
            simRunning.text = "Simulation is Paused";
            pauseButton.GetComponentInChildren<Text>().text = "Resume";
            Camera.main.GetComponent<MyVideoPlayer>().pauseVideo();
        }
        else
        {
            simRunning.text = "Simulation is Running";
            pauseButton.GetComponentInChildren<Text>().text = "Pause";
            Camera.main.GetComponent<MyVideoPlayer>().resumeVideo();
        }

    }

    public void stopVideo()
    {
       // if (Camera.main.GetComponent<MyVideoPlayer>().VideoPlaying)
     //   {
            simRunning.text = "Simulation is Stopped";
            pauseButton.GetComponentInChildren<Text>().text = "Play";
            Camera.main.GetComponent<MyVideoPlayer>().stopVideo();
            selectedAgents = new List<RVO.ArtificialAgent>();
     //  }

    }
    
    //Change the visibility of the artificial Agents
    public void changeArtificialVisibility(bool visibility)
    {
        RVO.AgentBehaviour.Instance.Visibility = visibility;
    }

    //Change the visibility of the projected Agents
    public void changeProjectedVisibility(bool visibility)
    {
        RVO.PedestrianProjection.Instance.Visibility = visibility;
    }

    public void deleteSelectedAgents()
    {
        while (selectedAgents.Count > 0)
        {
            RVO.AgentBehaviour.Instance.removeAgent(selectedAgents[0].gameObject);
            selectedAgents.Remove(selectedAgents[0]);
        }

    }

    public void selectAllAgents()
    {
        foreach (GameObject obj in RVO.AgentBehaviour.Instance.ArtificialAgents)
        {
            if (!obj.GetComponent<RVO.ArtificialAgent>().isSelected())
            {
                obj.GetComponent<RVO.ArtificialAgent>().setSelected();
                selectedAgents.Add(obj.GetComponent<RVO.ArtificialAgent>());
            }
        }
        //If the mode is remove, then just delete the agent
        if (currentMode == 2)
        {
            deleteSelectedAgents();
        }
    }

    public void deSelectAllAgents()
    {
        foreach (GameObject obj in RVO.AgentBehaviour.Instance.ArtificialAgents)
        {
            if (obj.GetComponent<RVO.ArtificialAgent>().isSelected())
            {
                obj.GetComponent<RVO.ArtificialAgent>().deSelect();
                selectedAgents.Remove(obj.GetComponent<RVO.ArtificialAgent>());
            }
        }
    }

    void Update()
    {
        //Agent selection counters are updated here
        numOfSelected.text = selectedAgents.Count.ToString();
        totalProjected.text = RVO.PedestrianProjection.Instance.RealAgents.Count.ToString();
        totalArtificial.text = RVO.AgentBehaviour.Instance.ArtificialAgents.Count.ToString();
        totalAgents.text = (RVO.AgentBehaviour.Instance.ArtificialAgents.Count + RVO.PedestrianProjection.Instance.RealAgents.Count).ToString();

        //Agent collision counters are updated here
        numOfCollisions.text = (RVO.AgentBehaviour.Instance.getAACollision() / 2 + RVO.AgentBehaviour.Instance.getAPCollision()).ToString();
        artificialToArtificialCollisions.text = (RVO.AgentBehaviour.Instance.getAACollision() / 2 ).ToString();
        artificialToProjectedCollisions.text = (RVO.AgentBehaviour.Instance.getAPCollision()).ToString();


        if (focalArea.text == "0")
        {
            focalArea.text = CameraPlacement.FOVtoFocal(mainCamera.GetComponent<Camera>().fieldOfView,
                    mainCamera.GetComponent<MyVideoPlayer>().retrieveResolution()[1]) + "";
            Debug.Log("FOV is " + CameraPlacement.FocaltoFOV(float.Parse(focalArea.text),
                    mainCamera.GetComponent<MyVideoPlayer>().retrieveResolution()[1]));
        }

        if (Input.GetMouseButtonUp(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            switch (currentMode)
            {
                case 0: //Select agents
                    if (Physics.Raycast(ray, out hit, 10000, 1 << 2) && hit.collider.tag == "Agent")
                    {
                        RVO.ArtificialAgent art = hit.collider.transform.GetComponentInParent<RVO.ArtificialAgent>();
                        if (!art.isSelected())
                        {
                            art.setSelected();
                            selectedAgents.Add(art);
                        }

                    }
                    
                    break;
                case 1://Add agent


                    if (Physics.Raycast(ray, out hit, 10000) && hit.collider.tag != "Agent")
                    {
                        RVO.AgentBehaviour.Instance.addAgent(hit.point);
                    }
                   // neighbours.maxValue = RVO.AgentBehaviour.Instance.numOfAgents + RVO.PedestrianProjection.Instance.;

                    break;
                case 2://Remove agent
                    if (Physics.Raycast(ray, out hit, 10000, 1 << 2) && hit.collider.tag == "Agent")
                    {
                        RVO.AgentBehaviour.Instance.removeAgent(hit.collider.transform.gameObject);
                        
                    }
                   // neighbours.maxValue = RVO.AgentBehaviour.Instance.numOfAgents;
                    break;
                default:
                    break;
            }



        }
        else if (Input.GetMouseButtonUp(1)) //Right clicking will deselect the agent clicked (if it has been selected before)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (currentMode == 0 && Physics.Raycast(ray, out hit, 10000, 1 << 2) && hit.collider.tag == "Agent")
            {
                RVO.ArtificialAgent art = hit.transform.GetComponentInParent<RVO.ArtificialAgent>();
                if (hit.transform.GetComponentInParent<RVO.ArtificialAgent>().isSelected())
                {
                    art.deSelect();
                    selectedAgents.Remove(art);
                }
            }
            else if (currentMode == 0 && Physics.Raycast(ray, out hit, 10000) && hit.collider.tag != "Agent")
            {
                foreach (RVO.ArtificialAgent agent in selectedAgents)
                    // agent.transform.GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(hit.point); 
                    agent.transform.GetComponent<RVO.ArtificialAgent>().setDestination(hit.point);
            }
        }

        //Debug.Log(selectedAgents.Count + "Selected agents");
    }


    public void changeConsideredNeighbours(string numOfNeighbours) { 
        foreach (RVO.ArtificialAgent art in selectedAgents)
            art.AgentReference.maxNeighbors_ = int.Parse(numOfNeighbours); 
    }
    public void changeMaxSpeed(string maxSpeed) {

        if (maxSpeed.Length != 0)
        {
            foreach (RVO.ArtificialAgent art in selectedAgents)
            {
                art.AgentReference.maxSpeed_ = float.Parse(maxSpeed);
                art.setSpeed(float.Parse(maxSpeed));
                //     art.GetComponent<UnityEngine.AI.NavMeshAgent>().speed = int.Parse(maxSpeed);
                //   art.GetComponent<UnityEngine.AI.NavMeshAgent>().acceleration = 10;
            }
        }
    } //Also changes their navMeshAgent speed
    public void changeRange(string range) { 
        if (range.Length != 0) { 
            foreach (RVO.ArtificialAgent art in selectedAgents) 
                art.AgentReference.neighborDist_ = int.Parse(range);
        } 
    }
    public void changeReactionSpeed(string reactionSpeed) { 
        if (reactionSpeed.Length != 0) { 
            foreach (RVO.ArtificialAgent art in selectedAgents) art.AgentReference.timeHorizon_ = int.Parse(reactionSpeed); 
        } }



    public void loadVideo()
    {
        RVO.AgentBehaviour.Instance.loadVideo();
    }

    public void loadMesh()
    {
        RVO.AgentBehaviour.Instance.loadMesh();
    }

    //Save the video so far (Need to check on Unity's capabilities on this)
    bool saving = false;
    Text saveButton;
    Text saveStatus;
    public void saveVideo() {
        if (!saving)
        {
            //Start capturing
            if (RockVR.Video.VideoCaptureCtrl.instance.status == RockVR.Video.VideoCaptureCtrl.StatusType.NOT_START)
            {
                
                RockVR.Video.VideoCaptureCtrl.instance.StartCapture();
            }

            saveButton.text = "Capturing...";

            saving = true;
        }
        else
        {
            if (RockVR.Video.VideoCaptureCtrl.instance.status == RockVR.Video.VideoCaptureCtrl.StatusType.STARTED)
            {
                RockVR.Video.VideoCaptureCtrl.instance.StopCapture();
            }

            while (RockVR.Video.VideoCaptureCtrl.instance.status != RockVR.Video.VideoCaptureCtrl.StatusType.FINISH)
            {
                saveButton.text = "Saving...";
            }

            saveButton.text = "Save Video";
            saveStatus.text = "Saved video to folder \"Video\"";
            saving = false;

        }
    }

    /**
     * Camera adjustment controls
     */
    public void translateCamera(string direction) {
        
        Vector3 v_direction = Vector3.zero;
        switch (direction)
        {
            case "UP": 
                v_direction = Vector3.up;
                break;
            case "DOWN": 
                v_direction = Vector3.down;
                break;
            case "LEFT": 
                v_direction = Vector3.left;
                break;
            case "RIGHT": 
                v_direction = Vector3.right;
                break;
            case "FORWARD": 
                v_direction = Vector3.forward;
                break;
            case "BACK": 
                v_direction = Vector3.back;
                break;
        }
        mainCamera.transform.Translate(v_direction,Space.World);
       // backGroundCamera.transform.Translate(v_direction,Space.World);

    
    } //Moves the camera by a unit towards the given direction


    public void updateCameraPosition()
    {

        try
        {
            float x = float.Parse(x_pos.text);
            float y = float.Parse(y_pos.text);
            float z = float.Parse(z_pos.text);
            Debug.Log("Pos changed to" + x + y + z);
            mainCamera.transform.position = new Vector3(x,y,z);
            //backGroundCamera.transform.rotation = Quaternion.Euler(x, y, z);
        }
        catch (System.FormatException e) { }
    }


    public void updateCameraOrientation() {

        try
        {
            float x = float.Parse(x_rot.text);
            float y = float.Parse(y_rot.text);
            float z = float.Parse(z_rot.text);
            mainCamera.transform.rotation = Quaternion.Euler(x, y, z);
            //backGroundCamera.transform.rotation = Quaternion.Euler(x, y, z);
        }catch (System.FormatException e){}
    }

    public void updateCameraFOV()
    {
        try
        {
            float focalLength = float.Parse(focalArea.text);
            mainCamera.GetComponent<Camera>().fieldOfView = CameraPlacement.FocaltoFOV(focalLength, mainCamera.GetComponent<MyVideoPlayer>().VideoHeight);
        }
        catch (System.FormatException e) { }
    }

    public void resetCamera (string attribute) //Reset the given attribute of the camera (either position or orientation)
    { 
        if(attribute.Equals("POS"))
        {
            Debug.Log("Pos changed");
            mainCamera.transform.position = Vector3.zero;
            //backGroundCamera.transform.position = Vector3.zero;
        }
        else if (attribute.Equals("ROT"))
        {
            mainCamera.transform.rotation = Quaternion.Euler(0, 0, 0);
           // backGroundCamera.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        initiateCameraControls();
    }


    public void toggleMeshVisibility()
    {
        RVO.AgentBehaviour.Instance.NavigableArea.GetComponent<MeshRenderer>().enabled =
            !RVO.AgentBehaviour.Instance.NavigableArea.GetComponent<MeshRenderer>().enabled;
    }

    public void definePedestrianHeight(string height)
    {
        RVO.PedestrianProjection.Instance.resetHeights(float.Parse(height));
    }

    public void updatePedestrianHeight(float value)
    {
        heightField.text = "" + value;
    }

}
