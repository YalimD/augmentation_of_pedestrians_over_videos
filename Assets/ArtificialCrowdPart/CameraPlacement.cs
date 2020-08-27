using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

public class CameraPlacement : MonoBehaviour
{
    public static Vector2 placeCameras(string camera_path, Camera mainCam)
    {
        //Invariant culture is expected from output of rectification
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        //Read the calibration file
        string[] cameraParameters = System.IO.File.ReadAllLines(@camera_path);
        Debug.Log("Camera file read");

        string[] internalParams = cameraParameters[0].Split(' ');
        string[] externalParams = cameraParameters[1].Split(' ');

        Vector3 u = new Vector3(float.Parse(externalParams[3]), float.Parse(externalParams[4]), float.Parse(externalParams[5])); // from OpenCV, Rodrigues matrix second column
        Vector3 f = new Vector3(float.Parse(externalParams[6]), float.Parse(externalParams[7]), float.Parse(externalParams[8])); // from OpenCV, Rodrigues matrix third column

        // notice that Y coordinates here are inverted to pass from OpenCV right-handed coordinates system to Unity left-handed one
        Quaternion rot;
        rot = Quaternion.LookRotation(new Vector3(f.x, f.y, f.z), new Vector3(u.x, u.y, u.z)); //PNP Solution, conversion done here

        mainCam.transform.position = Vector3.zero; //Reset pos (PNP)
        mainCam.transform.rotation = Quaternion.identity;

        // STEP 1 : fetch position from OpenCV + basic transformation
        Vector3 pos_read = new Vector3(float.Parse(externalParams[9]), float.Parse(externalParams[10]), float.Parse(externalParams[11])); //from OpenCV
        Vector3 pos;
        pos = new Vector3(pos_read.x, -pos_read.y, pos_read.z); // Pnp, The Y here needs to stay positive, as rotation already adjusts the axes. Making y negative would revert the adjustment

        // STEP 2 : set virtual camera's frustrum (Unity) to match physical camera's parameters
        Vector2 fparams = new Vector2(float.Parse(internalParams[4]), float.Parse(internalParams[5])); // from OpenCV (calibration parameters Fx and Fy = focal lengths in pixels)
        Vector2 resolution = new Vector2(float.Parse(internalParams[0]), float.Parse(internalParams[1])); // image resolution from OpenCV TODO: Should be the scale of the 
        float vFov = FocaltoFOV(fparams.y, resolution.y);// virtual camera (pinhole type) vertical field of view

        mainCam.fieldOfView = vFov;
        mainCam.aspect = resolution.x / resolution.y; // you could set a viewport rect with proper aspect as well... I would prefer the viewport approach

        Debug.Log("Focal length:" + fparams.y);
        Debug.Log("FOV:" + mainCam.fieldOfView);

        // STEP 3 : shift position to compensate for physical camera's optical axis not going exactly through image center

        //Rot and trans are coupled
        Debug.Log("ROT:" + rot.eulerAngles);
        Debug.Log("POS" + pos);
        Debug.Log("Main Cam Name" + mainCam.name);

        mainCam.transform.rotation = Quaternion.Euler(new Vector3(-rot.eulerAngles.x, rot.eulerAngles.y, -rot.eulerAngles.z)); //New conversion, Unity side
        mainCam.transform.position = pos;

        Debug.Log("New Pos " + mainCam.transform.position + " and new orientation " + mainCam.transform.rotation.eulerAngles);

        //Read another line about adjustment to side
        for (int sol = 2; sol < 6 && false; sol++)
        {
            string[] adjustmentParam = cameraParameters[sol].Split(' ');
            u = new Vector3(float.Parse(adjustmentParam[3]), float.Parse(adjustmentParam[4]), float.Parse(adjustmentParam[5])); // from OpenCV,Rodrigues matrix second column
            f = new Vector3(float.Parse(adjustmentParam[6]), float.Parse(adjustmentParam[7]), float.Parse(adjustmentParam[8])); // from OpenCV, Rodrigues matrix third column

            // notice that Y coordinates here are inverted to pass from OpenCV right-handed coordinates system to Unity left-handed one
            rot = Quaternion.LookRotation(new Vector3(f.x, f.y, f.z), new Vector3(u.x, u.y, u.z)); //PNP Solution

            // STEP 1 : fetch position from OpenCV + basic transformation
            pos = new Vector3(float.Parse(adjustmentParam[9]), float.Parse(adjustmentParam[10]), float.Parse(adjustmentParam[11])); //from OpenCV
            pos = new Vector3(pos.x, -pos.y, pos.z); // Pnp, The Y here needs to stay positive, as rotation already adjusts the axes. Making y negative would revert the adjustment

            Debug.Log("Decomposed rot" + new Vector3(360 - rot.eulerAngles.x, rot.eulerAngles.y, 360 - rot.eulerAngles.z));
            Debug.Log("Decomposed trans" + pos);
        }

        float width = float.Parse(internalParams[0]);
        float height = float.Parse(internalParams[1]);

        return new Vector2(width, height);
    }

    public static float FocaltoFOV(float focal, float axisResolution)
    {
        return 2.0f * Mathf.Atan(0.5f * axisResolution / focal) * Mathf.Rad2Deg; // virtual camera (pinhole type) vertical field of view
    }

    public static float FOVtoFocal(float fov, float axisResolution)
    {
        return axisResolution / (2 * Mathf.Tan(fov * Mathf.Deg2Rad / 2.0f));
    }

}
