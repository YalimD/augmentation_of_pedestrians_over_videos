using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Reads the camera external and internal's from txt file and applies to the cameras related to video
 * in the scene
 * Txt file contains:
 *      resolution_x resolution_y CX CY focal_x focal_y 
 *      rotation matrix (Single line from 11 to 33, column-wise, left to right= X,Y,Z) Translation vector
 *      
 * Got help from: https://stackoverflow.com/questions/36561593/opencv-rotation-rodrigues-and-translation-vectors-for-positioning-3d-object-in
 */

public class CameraPlacement : MonoBehaviour {

    public static void placeCameras(string camera_path, Camera mainCam)
    {
        string mode = "pnp";
        
        //Read the calibration file
        string[] cameraParameters = System.IO.File.ReadAllLines(@camera_path);
        Debug.Log("Camera file read");

        string [] internalParams = cameraParameters[0].Split(' ');
        string [] externalParams = cameraParameters[1].Split(' ');
      
        Vector3 u = new Vector3(float.Parse(externalParams[3]),float.Parse(externalParams[4]),float.Parse(externalParams[5])); // from OpenCV,Rodrigues matrix second column
        Vector3 f = new Vector3(float.Parse(externalParams[6]),float.Parse(externalParams[7]),float.Parse(externalParams[8])); // from OpenCV, Rodrigues matrix third column

        // notice that Y coordinates here are inverted to pass from OpenCV right-handed coordinates system to Unity left-handed one
        Quaternion rot;
        if (mode == "pnp"){

            rot = Quaternion.LookRotation(new Vector3(f.x, f.y, f.z), new Vector3(u.x, u.y, u.z)); //PNP Solution, conversion done here
            //rot = Quaternion.LookRotation(new Vector3(f.x, f.y, f.z), new Vector3(u.x, u.y, u.z)); //PNP Solution, conversion done in Python

            mainCam.transform.position = Vector3.zero; //Reset pos (PNP)
            mainCam.transform.rotation = Quaternion.identity;
        }
        else
            rot = Quaternion.LookRotation(new Vector3(f.x, f.z, f.y), new Vector3(u.x, u.z, u.y)); // Decomp

        
		float width = float.Parse(internalParams[0]);
		float height = float.Parse(internalParams[1]);
		
        // STEP 1 : fetch position from OpenCV + basic transformation
        Vector3 pos_read = new Vector3(float.Parse(externalParams[9]),float.Parse(externalParams[10]),float.Parse(externalParams[11])); //from OpenCV
        Vector3 pos;
        pos = new Vector3(pos_read.x, -pos_read.y, pos_read.z); // Pnp, The Y here needs to stay positive, as rotation already adjusts the axes. Making y negative would revert the adjustment
        //pos = new Vector3(pos_read.x, pos_read.y, height + pos_read.z); // Pnp, here as the coordiantes systems arematched in Unity side, the translation is untouched


        // STEP 2 : set virtual camera's frustrum (Unity) to match physical camera's parameters
        Vector2 fparams = new Vector2(float.Parse(internalParams[4]),float.Parse(internalParams[5])); // from OpenCV (calibration parameters Fx and Fy = focal lengths in pixels)
        Vector2 resolution = new Vector2(float.Parse(internalParams[0]), float.Parse(internalParams[1])); // image resolution from OpenCV TODO: Should be the scale of the 
      //  float vfov = 2.0f * Mathf.Atan(0.5f * resolution.y / fparams.y) * Mathf.Rad2Deg;
        float vFov = FocaltoFOV(fparams.y, resolution.y);// virtual camera (pinhole type) vertical field of view

        Debug.Log("Focal length:" + fparams.y);
        Debug.Log("FOV:" + vFov);

        mainCam.fieldOfView = vFov;
        mainCam.aspect = resolution.x / resolution.y; // you could set a viewport rect with proper aspect as well... I would prefer the viewport approach


        // STEP 3 : shift position to compensate for physical camera's optical axis not going exactly through image center
        Vector2 cparams = new Vector2(float.Parse(internalParams[2]), float.Parse(internalParams[3]));  // from OpenCV (calibration parameters Cx and Cy = optical center shifts from image center in pixels)
        Vector3 imageCenter = new Vector3(0.5f, 0.5f, pos.z); // in viewport coordinates
        Vector3 opticalCenter = new Vector3(0.5f + cparams.x / resolution.x, 0.5f + cparams.y / resolution.y, pos.z); // in viewport coordinates
        //pos += mainCam.ViewportToWorldPoint(imageCenter) - mainCam.ViewportToWorldPoint(opticalCenter); // position is set as if physical camera's optical axis went exactly through image center
        Debug.Log(mainCam.ViewportToWorldPoint(imageCenter) - mainCam.ViewportToWorldPoint(opticalCenter));

        //Camera is placed at 0,0,0 and looks at horizon, not looking from above (PNP)

        //Rot and trans are coupled
        if (mode == "pnp")
        {
            //mainCam.transform.rotation = rot; //Pnp
            Debug.Log("ROT:" + rot.eulerAngles);
            Debug.Log("POS" + pos);
            Debug.Log("Main Cam Name" + mainCam.name);

            //Vector3 post_pos = new Vector3(pos.x, pos.y, pos.z); 
            //mainCam.transform.rotation = Quaternion.Euler(new Vector3(-rot.eulerAngles.x, rot.eulerAngles.y, -rot.eulerAngles.z)); //Old conversion, Python side
            mainCam.transform.rotation = Quaternion.Euler(new Vector3(-rot.eulerAngles.x, rot.eulerAngles.y, -rot.eulerAngles.z)); //New conversion, Unity side
            mainCam.transform.position = pos;
            //post_pos = Vector3.Dot()
            //mainCam.transform.Rotate(new Vector3(rot.eulerAngles.x, rot.eulerAngles.z, rot.eulerAngles.y));
            //mainCam.transform.Rotate(90f, 0f, 0f, Space.World);
            //pos = new Vector3(pos_read.x, pos_read.z, pos_read.y);
            //mainCam.transform.Translate(pos, mainCam.transform);
        }
        else {
            mainCam.transform.Rotate(rot.eulerAngles); //Decomp

            mainCam.transform.Translate(pos, mainCam.transform); //Both
           // mainCam.transform.Rotate(new Vector3(90, 0, 0), Space.World); //Decomp
            //mainCam.transform.Translate(new Vector3(0, mainCam.transform.position.y * -2, 0),  Space.World); //Decomp
        }


        Debug.Log("New Pos " + mainCam.transform.position + " and new orientation " + mainCam.transform.rotation);

        //Read another line about adjustment to side
        //

        for(int sol = 2;sol < 6 && false;sol++)
        {
            string[] adjustmentParam = cameraParameters[sol].Split(' ');
            u = new Vector3(float.Parse(adjustmentParam[3]), float.Parse(adjustmentParam[4]), float.Parse(adjustmentParam[5])); // from OpenCV,Rodrigues matrix second column
            f = new Vector3(float.Parse(adjustmentParam[6]), float.Parse(adjustmentParam[7]), float.Parse(adjustmentParam[8])); // from OpenCV, Rodrigues matrix third column

            // notice that Y coordinates here are inverted to pass from OpenCV right-handed coordinates system to Unity left-handed one
            if (mode == "pnp")
            {
                rot = Quaternion.LookRotation(new Vector3(f.x, f.y, f.z), new Vector3(u.x, u.y, u.z)); //PNP Solution
            }
            else
                rot = Quaternion.LookRotation(new Vector3(f.x, f.z, f.y), new Vector3(u.x, u.z, u.y)); // Decomp


            // STEP 1 : fetch position from OpenCV + basic transformation
            pos = new Vector3(float.Parse(adjustmentParam[9]), float.Parse(adjustmentParam[10]), float.Parse(adjustmentParam[11])); //from OpenCV
            if (mode == "pnp") pos = new Vector3(pos.x, -pos.y, pos.z); // Pnp, The Y here needs to stay positive, as rotation already adjusts the axes. Making y negative would revert the adjustment
            else pos = new Vector3(-pos.x, pos.y, -pos.z) * 278; // Decomp

            Debug.Log("Decomposed rot" + new Vector3(360-rot.eulerAngles.x, rot.eulerAngles.y,360 -rot.eulerAngles.z));
            Debug.Log("Decomposed trans" + pos);
        }
        

        //mainCam.transform.Rotate(new Vector3(-rot.eulerAngles.x, rot.eulerAngles.y, -rot.eulerAngles.z), Space.Self);
       // mainCam.transform.Translate(pos, Space.Self);

        //TODO: Initialize camera properties
      //  GameObject.Find("UICanvas").GetComponent<GUIController>().initiateCameraControls();

	}

    public static float FocaltoFOV(float focal, float axisResolution)
    {
        return 2.0f * Mathf.Atan(0.5f * axisResolution / focal) * Mathf.Rad2Deg; // virtual camera (pinhole type) vertical field of view
    }

    public static float FOVtoFocal(float fov, float axisResolution)
    {
        return axisResolution / (2 * Mathf.Tan(fov * Mathf.Deg2Rad / 2.0f ));
    }
	
}
