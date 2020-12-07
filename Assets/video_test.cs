using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Windows.WebCam;

public class video_test : MonoBehaviour
{
    static readonly float MaxRecordingTime = 5.0f;

    VideoCapture m_VideoCapture = null;
    float m_stopRecordingTimer = float.MaxValue;

    // Use this for initialization
    void Start()
    {
        //StartVideoCaptureTest();
    }

    void Update()
    {
        if (m_VideoCapture == null || !m_VideoCapture.IsRecording)
        {
            return;
        }

        if (Time.time > m_stopRecordingTimer)
        {
            m_VideoCapture.StopRecordingAsync(OnStoppedRecordingVideo);
        }
    }

    void StartVideoCaptureTest()
    {

        Resolution cameraResolution = VideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        UnityEngine.Debug.Log($"Camera resolution: {cameraResolution}");

        float cameraFramerate = VideoCapture.GetSupportedFrameRatesForResolution(cameraResolution).OrderByDescending((fps) => fps).First();
        UnityEngine.Debug.Log($"Camer framerate: {cameraFramerate}");

        VideoCapture.CreateAsync(false, delegate (VideoCapture videoCapture)
        {
            UnityEngine.Debug.Log("checkpoint 1");
            if (videoCapture != null)
            {
                UnityEngine.Debug.Log("checkpoint 2");
                m_VideoCapture = videoCapture;
                Debug.Log("Created VideoCapture Instance!");

                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 0.0f;
                cameraParameters.frameRate = cameraFramerate;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.JPEG;

                m_VideoCapture.StartVideoModeAsync(cameraParameters,
                                                   VideoCapture.AudioState.ApplicationAndMicAudio,
                                                   OnStartedVideoCaptureMode);
            }
            else
            {
                Debug.LogError("Failed to create VideoCapture Instance!");
            }
        });
        UnityEngine.Debug.Log("After function");
    }

    void OnStartedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Started Video Capture Mode!");
        string timeStamp = Time.time.ToString().Replace(".", "").Replace(":", "");
        string filename = string.Format("TestVideo_{0}.mp4", timeStamp);
        string filepath = System.IO.Path.Combine(Application.persistentDataPath, filename);
        filepath = filepath.Replace("/", @"\");
        m_VideoCapture.StartRecordingAsync(filepath, OnStartedRecordingVideo);
    }

    void OnStoppedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Stopped Video Capture Mode!");
    }

    void OnStartedRecordingVideo(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Started Recording Video!");
        m_stopRecordingTimer = Time.time + MaxRecordingTime;
    }

    void OnStoppedRecordingVideo(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Stopped Recording Video!");
        m_VideoCapture.StopVideoModeAsync(OnStoppedVideoCaptureMode);
    }
}