using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//TODO: Useless ?
public class TranslateMatrix : MonoBehaviour {

    public Vector3 translation;
    private MeshFilter mf;
    private Vector3[] origVerts;
    private Vector3[] newVerts;

    void Start() {
        mf = GetComponent<MeshFilter>();
        origVerts = mf.mesh.vertices;
        newVerts = new Vector3[origVerts.Length];
    }

    void Update() {

        Matrix4x4 matrix = new Matrix4x4();
        string camera_path = "unityCamCalibration.txt";
        string[] cameraParameters = System.IO.File.ReadAllLines(@camera_path);
        Debug.Log("Camera file read");

        string [] internalParams = cameraParameters[0].Split(' ');
        string [] externalParams = cameraParameters[1].Split(' ');
      
        Vector3 u = new Vector3(float.Parse(externalParams[3]),float.Parse(externalParams[4]),float.Parse(externalParams[5])); // from OpenCV,Rodrigues matrix second column
        Vector3 f = new Vector3(float.Parse(externalParams[6]),float.Parse(externalParams[7]),float.Parse(externalParams[8])); // from OpenCV, Rodrigues matrix third column

        // notice that Y coordinates here are inverted to pass from OpenCV right-handed coordinates system to Unity left-handed one
        Quaternion rot;
        rot = Quaternion.LookRotation(new Vector3(f.x, -f.y, f.z), new Vector3(u.x, -u.y, u.z)); //P


        //int i = 0;
        //while (i < origVerts.Length) {
        //    newVerts[i] = m.MultiplyPoint3x4(origVerts[i]);
        //    i++;
        //}
        //mf.mesh.vertices = newVerts;
    }
}