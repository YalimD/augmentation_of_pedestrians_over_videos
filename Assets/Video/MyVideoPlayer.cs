using UnityEngine;
using System;
using System.IO;
using System.Collections;

public class MyVideoPlayer : MonoBehaviour
{
    string videoName;

    public UnityEngine.Video.VideoPlayer VideoPlayer { get; private set; }

    public bool VideoPlaying { get; private set; }
    public int VideoHeight { get { return VideoPlayer.texture.height; } }
    public int VideoWidth { get { return VideoPlayer.texture.width; } }

    //Resume, pause and stop the video
    public void ResumeVideo()
    {
        VideoPlaying = true;
        Time.timeScale = 1;
    }

    public void PauseVideo()
    {
        VideoPlaying = false;
        Time.timeScale = 0;
    }

    public void StopVideo()
    {
        RVO.AgentBehaviour.Instance.Restart();
        VideoPlaying = false;
        VideoPlayer.frame = 0;
        Time.timeScale = 0;
    }

    public string GetVideoName()
    {
        return videoName;
    }

    //Initialize the video player
	public void StartVideo(string videoPath)
	{
        videoName = Path.GetFileName(videoPath);
		GameObject camera = GameObject.Find("MainCamera");

		VideoPlayer = camera.AddComponent<UnityEngine.Video.VideoPlayer>();

		VideoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.CameraFarPlane;
		VideoPlayer.targetCameraAlpha = 1F;
		VideoPlayer.url = videoPath;

		VideoPlayer.isLooping = false;
        VideoPlayer.skipOnDrop = true;

        VideoPlaying = false;
        VideoPlayer.playOnAwake = true;

        VideoPlayer.aspectRatio = UnityEngine.Video.VideoAspectRatio.Stretch;

        VideoPlayer.Prepare();
        VideoPlayer.Pause();

        Debug.Log("Video FPS:" + VideoPlayer.frameRate);
    }

    double deltaTime = 0.0f;

    void Update()
    {
        deltaTime += Time.deltaTime;

        if (deltaTime >= (double)(1 / VideoPlayer.frameRate))
        {
            RVO.AgentBehaviour.Instance.Step();
            VideoPlaying = VideoPlayer.frame < (long)VideoPlayer.frameCount;

            if (VideoPlaying)
            {
                //If projection step is not sucessful, don't continue the video since it will cause delays for pedestrians
                if (RVO.PedestrianProjection.Instance.Step())
                {
                    VideoPlayer.StepForward();

                    //Debug.Log("This should be (optimistically) 1 sec:" + deltaTime * videoPlayer.frameRate + " where framerate is " + videoPlayer.frameRate);
                    deltaTime = 0.0f;
                }
            }
        }
 
    }

    internal int[] RetrieveResolution()
    {
        int[] resolution = {0,0};
        try
        {
            resolution[0] = VideoPlayer.texture.width; resolution[1] = VideoPlayer.texture.height;
        }
        catch (NullReferenceException n) { }

        return resolution;

    }
}