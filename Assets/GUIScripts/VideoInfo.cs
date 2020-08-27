using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/*
 * Insprired from http://wiki.unity3d.com/index.php?title=FramesPerSecond by Dave Hampson
 * 
 * This code calculates the frames per second currently on Unity and displays it on the related
 * text on screen.
 * 
 * Writes the name of the video on the screen.
 * 
 * Also displays the current time of the video.
 */ 

public class VideoInfo: MonoBehaviour {

    private const int frameThreshold = 24;

    private Text fpsCount, name;
    float fps;
    private int framesPassed;
    private float deltaTime;

    void Start()
    {
        framesPassed = 0;
        fps = 0;
        deltaTime = 0.0f;

        //Reference to related children of the parent panel
        fpsCount = GameObject.Find("FPSCount").gameObject.GetComponent<Text>();
        name = GameObject.Find("VideoName").gameObject.GetComponent<Text>();

        name.text = "Video Name: " + Camera.main.GetComponent<MyVideoPlayer>().GetVideoName();
    }

    //Update the delta time for fps and time of the video
	void Update()
	{
        if (Time.timeScale > 0)
        {
            deltaTime += Time.deltaTime;

            //Update fps every few frames
            if (++framesPassed == frameThreshold)
            {
                //Debug.Log(deltaTime);
                fps = frameThreshold / (deltaTime);
                framesPassed = 0;
                deltaTime = 0;
            }

            fpsCount.text = string.Format("FPS: {0:0.}", fps);
        }
	}
}

