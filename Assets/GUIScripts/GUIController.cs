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

    Transform agentCount;
    Text numOfSelectedText, totalArtificialText, totalProjectedText, totalAgentsText;

    InputField numOfNeighboursText, maxSpeedText, rangeText, reactionSpeedText;

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

            DeleteSelectedAgents();

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

    Transform collisionCounter;
    Text numOfCollisions, artificialToArtificialCollisions, artificialToProjectedCollisions;

    Button pauseButton;
    Text simRunning;

    InputField x_rot, y_rot, z_rot;
    InputField x_pos, y_pos, z_pos;
    InputField focalArea;
    InputField heightField;
    public GameObject mainCamera;

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

        numOfSelectedText = agentCount.transform.Find("NumOfSelectedArt").Find("Num").GetComponent<Text>();
        totalProjectedText = agentCount.transform.Find("Show Projected").Find("Num").GetComponent<Text>();
        totalArtificialText = agentCount.transform.Find("Show Artificial").Find("Num").GetComponent<Text>();
        totalAgentsText = agentCount.transform.Find("Total").Find("Num").GetComponent<Text>();

        collisionCounter = transform.Find("CollisionCounter");
        numOfCollisions = collisionCounter.transform.Find("NumOfCollisions").Find("Text").GetComponent<Text>();
        artificialToArtificialCollisions = collisionCounter.transform.Find("ArtArt").Find("Text").GetComponent<Text>();
        artificialToProjectedCollisions = collisionCounter.transform.Find("ArtPro").Find("Text").GetComponent<Text>();

        mainCamera = GameObject.Find("MainCamera");

        saveButton = GameObject.Find("SaveVideo").GetComponentInChildren<Text>();

        x_rot = GameObject.Find("X_Rot").GetComponent<InputField>();
        y_rot = GameObject.Find("Y_Rot").GetComponent<InputField>();
        z_rot = GameObject.Find("Z_Rot").GetComponent<InputField>();

        x_pos = GameObject.Find("X_Pos").GetComponent<InputField>();
        y_pos = GameObject.Find("Y_Pos").GetComponent<InputField>();
        z_pos = GameObject.Find("Z_Pos").GetComponent<InputField>();

        focalArea = GameObject.Find("FocalLength").GetComponent<InputField>();
        heightField = GameObject.Find("HeightField").GetComponent<InputField>();

        numOfNeighboursText = GameObject.Find("Neighbours").GetComponentInChildren<InputField>();
        maxSpeedText = GameObject.Find("MaxSpeed").GetComponentInChildren<InputField>();
        rangeText = GameObject.Find("Range").GetComponentInChildren<InputField>();
        reactionSpeedText = GameObject.Find("ReactionSpeed").GetComponentInChildren<InputField>();

    }

    //Called after initialized using the camera calibration file
    public void updateCameraParameterView()
    {
        Vector3 eulerRot = mainCamera.transform.rotation.eulerAngles;
        x_rot.text = eulerRot.x + "";
        y_rot.text = eulerRot.y + "";
        z_rot.text = eulerRot.z + "";

        x_pos.text = mainCamera.transform.position.x + "";
        y_pos.text = mainCamera.transform.position.y + "";
        z_pos.text = mainCamera.transform.position.z + "";

        focalArea.text = CameraPlacement.FOVtoFocal(mainCamera.GetComponent<Camera>().fieldOfView,
           mainCamera.GetComponent<MyVideoPlayer>().RetrieveResolution()[1]) + "";
    }

    public void pauseVideo()
    {
        if (mainCamera.GetComponent<Camera>().GetComponent<MyVideoPlayer>().VideoPlaying)
        {
            simRunning.text = "Simulation is Paused";
            pauseButton.GetComponentInChildren<Text>().text = "Resume";
            mainCamera.GetComponent<Camera>().GetComponent<MyVideoPlayer>().PauseVideo();
        }
        else
        {
            simRunning.text = "Simulation is Running";
            pauseButton.GetComponentInChildren<Text>().text = "Pause";
            mainCamera.GetComponent<Camera>().GetComponent<MyVideoPlayer>().ResumeVideo();
        }

    }

    public void StopVideo()
    {
        simRunning.text = "Simulation is Stopped";
        pauseButton.GetComponentInChildren<Text>().text = "Play";
        mainCamera.GetComponent<Camera>().GetComponent<MyVideoPlayer>().StopVideo();
        selectedAgents = new List<RVO.ArtificialAgent>();
    }

    //Change the visibility of the artificial Agents
    public void ChangeArtificialVisibility(bool visibility)
    {
        RVO.AgentBehaviour.Instance.Visibility = visibility;
    }

    //Change the visibility of the projected Agents
    public void ChangeProjectedVisibility(bool visibility)
    {
        RVO.PedestrianProjection.Instance.Visibility = visibility;
    }

    public void DeleteSelectedAgents()
    {
        while (selectedAgents.Count > 0)
        {
            RVO.AgentBehaviour.Instance.RemoveAgent(selectedAgents[0].gameObject);
            selectedAgents.Remove(selectedAgents[0]);
        }

    }

    public void SelectAllAgents()
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
            DeleteSelectedAgents();
        }

        UpdateRVOView();
    }

    public void DeSelectAllAgents()
    {
        foreach (GameObject obj in RVO.AgentBehaviour.Instance.ArtificialAgents)
        {
            if (obj.GetComponent<RVO.ArtificialAgent>().isSelected())
            {
                obj.GetComponent<RVO.ArtificialAgent>().deSelect();
                selectedAgents.Remove(obj.GetComponent<RVO.ArtificialAgent>());
            }
        }

        ViewDefaultRVOParameters();
    }

    void UpdateRVOView()
    {
        //If no agents is selected, default paramters are shown
        if (selectedAgents.Count == 0)
        {
            ViewDefaultRVOParameters();
        }
        else if (selectedAgents.Count == 1) //show selected agent's info
        {
            ViewSingleAgentRVOParameters(selectedAgents[0]);
        }
        else //Show VAR
        {
            ViewPlaceholderRVOParameters();
        }
    }

    void ViewSingleAgentRVOParameters(RVO.ArtificialAgent selectedAgent)
    {
        numOfNeighboursText.text = "" + selectedAgent.AgentReference.maxNeighbors_;
        rangeText.text = "" + selectedAgent.coefficients.rangeCoefficient;
        reactionSpeedText.text = "" + selectedAgent.coefficients.reactionCoefficient;
        maxSpeedText.text = "" + selectedAgent.coefficients.speedCoefficient;
    }

    void ViewDefaultRVOParameters()
    {
        numOfNeighboursText.text = "100";
        rangeText.text = "1";
        reactionSpeedText.text = "1";
        maxSpeedText.text = "1";
    }

    void ViewPlaceholderRVOParameters()
    {
        numOfNeighboursText.text = "VAR";
        rangeText.text = "VAR";
        reactionSpeedText.text = "VAR";
        maxSpeedText.text = "VAR";
    }

    void Update()
    {
        //Agent selection counters are updated here
        numOfSelectedText.text = selectedAgents.Count.ToString();
        totalProjectedText.text = RVO.PedestrianProjection.Instance.RealAgents.Count.ToString();
        totalArtificialText.text = RVO.AgentBehaviour.Instance.ArtificialAgents.Count.ToString();
        totalAgentsText.text = (RVO.AgentBehaviour.Instance.ArtificialAgents.Count + RVO.PedestrianProjection.Instance.RealAgents.Count).ToString();

        //Agent collision counters are updated here
        numOfCollisions.text = (RVO.AgentBehaviour.Instance.getAACollision() / 2 + RVO.AgentBehaviour.Instance.getAPCollision()).ToString();
        artificialToArtificialCollisions.text = (RVO.AgentBehaviour.Instance.getAACollision() / 2).ToString();
        artificialToProjectedCollisions.text = (RVO.AgentBehaviour.Instance.getAPCollision()).ToString();


        if (focalArea.text == "0")
        {
            updateCameraParameterView();
            focalArea.text = CameraPlacement.FOVtoFocal(mainCamera.GetComponent<Camera>().fieldOfView,
                mainCamera.GetComponent<MyVideoPlayer>().RetrieveResolution()[1]) + "";

            Debug.Log("FOV is " + CameraPlacement.FocaltoFOV(float.Parse(focalArea.text),
                    mainCamera.GetComponent<MyVideoPlayer>().RetrieveResolution()[1]));
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
                        RVO.ArtificialAgent foundAgent = hit.collider.transform.GetComponentInParent<RVO.ArtificialAgent>();
                        if (!foundAgent.isSelected())
                        {
                            foundAgent.setSelected();
                            selectedAgents.Add(foundAgent);
                        }

                        UpdateRVOView();
                    }

                    break;
                case 1://Add agent

                    if (Physics.Raycast(ray, out hit, 10000) && hit.collider.tag != "Agent")
                    {
                        RVO.AgentBehaviour.Instance.AddAgent(hit.point);
                    }

                    // neighbours.maxValue = RVO.AgentBehaviour.Instance.numOfAgents + RVO.PedestrianProjection.Instance.;

                    break;
                case 2://Remove agent
                    if (Physics.Raycast(ray, out hit, 10000, 1 << 2) && hit.collider.tag == "Agent")
                    {
                        RVO.AgentBehaviour.Instance.RemoveAgent(hit.collider.transform.gameObject);

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
                    agent.transform.GetComponent<RVO.ArtificialAgent>().setDestination(hit.point);
            }
        }

        //Debug.Log(selectedAgents.Count + "Selected agents");
    }

    public void ChangeConsideredNeighbours(string numOfNeighbours)
    {
        foreach (RVO.ArtificialAgent art in selectedAgents)
        {
            art.AgentReference.maxNeighbors_ = int.Parse(numOfNeighbours);
        }
    }

    public void changeMaxSpeed(string maxSpeed)
    {

        if (maxSpeed.Length != 0)
        {
            foreach (RVO.ArtificialAgent art in selectedAgents)
            {
                art.AgentReference.maxSpeed_ = (art.AgentReference.maxSpeed_ * (float.Parse(maxSpeed)))
                                              / (art.coefficients.speedCoefficient);
                art.coefficients.speedCoefficient = float.Parse(maxSpeed);//Works as a coefficient
            }
        }
    }

    //Also changes their navMeshAgent speed
    public void ChangeRange(string range)
    {
        if (range.Length != 0)
        {
            foreach (RVO.ArtificialAgent art in selectedAgents)
            {
                art.AgentReference.neighborDist_ = (art.AgentReference.neighborDist_ * (float.Parse(range)))
                                              / (art.coefficients.rangeCoefficient);
                art.coefficients.rangeCoefficient = float.Parse(range);//Works as a coefficient
            }
        }
    }
    public void changeReactionSpeed(string reactionSpeed)
    {
        if (reactionSpeed.Length != 0)
        {
            foreach (RVO.ArtificialAgent art in selectedAgents)
            {
                art.AgentReference.timeHorizon_ = (art.AgentReference.timeHorizon_ * (float.Parse(reactionSpeed)))
                                             / (art.coefficients.reactionCoefficient);
                art.coefficients.reactionCoefficient = float.Parse(reactionSpeed);//Works as a coefficient
            }
        }
    }



    public void loadVideo()
    {
        RVO.AgentBehaviour.Instance.LoadVideo();
    }

    public void loadMesh()
    {
        RVO.AgentBehaviour.Instance.loadMesh();
    }

    //Save the video so far (Need to check on Unity's capabilities on this)
    bool saving = false;
    Text saveButton;
    public void saveVideo()
    {
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
            saving = false;

        }
    }

    /**
     * Camera adjustment controls
     */
    public void translateCamera(string direction)
    {

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
        mainCamera.transform.Translate(v_direction, Space.World);


    } //Moves the camera by a unit towards the given direction

    //TODO: Put camera properties in a struct aside from the camera object
    public void updateCameraPosition_X(string x_pos)
    {
        float x = float.Parse(x_pos);
        mainCamera.transform.position.Set(x, mainCamera.transform.position.y,
                                            mainCamera.transform.position.z);
    }

    public void updateCameraOrientation_Y(string y_rot)
    {

        float y = float.Parse(y_rot);
        Vector3 rotationEuler = mainCamera.transform.rotation.eulerAngles;
        mainCamera.transform.rotation = Quaternion.Euler(rotationEuler.x,
                                                        y, rotationEuler.z);
    }

    public void updateCameraPosition_Z(string z_pos)
    {
        float z = float.Parse(z_pos);
        mainCamera.transform.position.Set(mainCamera.transform.position.x,
                                            mainCamera.transform.position.y, z);
    }

    public void updateCameraOrientation_X(string x_rot)
    {

        float x = float.Parse(x_rot);
        Vector3 rotationEuler = mainCamera.transform.rotation.eulerAngles;
        mainCamera.transform.rotation = Quaternion.Euler(x, rotationEuler.y,
                                                            rotationEuler.z);
    }

    public void updateCameraPosition_Y(string y_pos)
    {
        float y = float.Parse(y_pos);
        mainCamera.transform.position.Set(mainCamera.transform.position.x,
                                            y,
                                            mainCamera.transform.position.z);
    }

    public void updateCameraOrientation_Z(string z_rot)
    {

        float z = float.Parse(z_rot);
        Vector3 rotationEuler = mainCamera.transform.rotation.eulerAngles;
        mainCamera.transform.rotation = Quaternion.Euler(rotationEuler.x,
                                                            rotationEuler.y,
                                                            z);
    }

    public void updateCameraFOV()
    {
        try
        {
            float focalLength = float.Parse(focalArea.text);
            mainCamera.GetComponent<Camera>().fieldOfView = CameraPlacement.FocaltoFOV(focalLength, mainCamera.GetComponent<MyVideoPlayer>().VideoHeight);
        }
        catch (FormatException e) { }
    }

    public void resetCamera(string attribute) //Reset the given attribute of the camera (either position or orientation)
    {
        if (attribute.Equals("POS"))
        {
            Debug.Log("Pos changed");
            mainCamera.transform.position = Vector3.zero;
        }
        else if (attribute.Equals("ROT"))
        {
            mainCamera.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        updateCameraParameterView();
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

    public void UpdatePedestrianHeight(float value)
    {
        heightField.text = "" + value;
    }

}
