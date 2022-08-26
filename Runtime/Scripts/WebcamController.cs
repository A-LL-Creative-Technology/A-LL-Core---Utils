using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebcamController : MonoBehaviour
{

    private static WebcamController _singleton;

    public static WebcamController Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(WebcamController)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    [SerializeField] private string specifiedWebcamToLookFor = "BRIO";
    [SerializeField] private int requestedFPS = 30;
    [SerializeField] private int requestedWidth = 1920;
    [SerializeField] private int requestedHeight = 1080;

    private IEnumerator coroutineRender;

    /// <summary>
    /// The delay
    /// </summary>
    public float delay = 0.5f;

    public GameObject webcamView;

    private RawImage rawimage;

    bool isWebcamFound = false;
    bool isDefaultWebcamSet = false;

    private WebCamTexture webcamTexture;
        
    /// <summary>
    /// The size of the buffer containing the recorded images
    /// </summary>
    /// <remarks>
    /// Try to keep this value as low as possible according to the delay
    /// </remarks>
    private int bufferSize;
    
    /// <summary>
    /// The recorded frames
    /// </summary>
    private Frame[] frames;

    /// <summary>
    /// The index of the captured texture
    /// </summary>
    private int capturedFrameIndex;

    /// <summary>
    /// The index of the rendered texture
    /// </summary>
    private int renderedFrameIndex;

    /// <summary>
    /// The frame index
    /// </summary>
    private int frameIndex;



    private void Awake()
    {
        Singleton = this;

        coroutineRender = Render();
    }

    void Start()
    {
        //Hide Cursor
        Cursor.visible = false;

        rawimage = webcamView.GetComponent<RawImage>();

        webcamTexture = new WebCamTexture();

        InitWebcam();

        OnSetDelay(delay);
    }

    private void InitWebcam()
    {
        

        Debug.Log("Available Webcams");
        
        foreach (WebCamDevice currentDevice in WebCamTexture.devices)
        {
            string cameraShortName = currentDevice.name.Split(' ')[0];

            Debug.Log(cameraShortName);

            if (String.Equals(cameraShortName, specifiedWebcamToLookFor))
            {
                webcamTexture.deviceName = currentDevice.name;

                isWebcamFound = true;
            }
        }

        if (!isWebcamFound)
        {
            Debug.Log("The specified webcam '" + specifiedWebcamToLookFor + "' couldn't be found. We try again in 5 seconds.");

            Invoke("InitWebcam", 5.0f);

            if (WebCamTexture.devices.Length > 0 && !isDefaultWebcamSet)
            {
                webcamTexture.deviceName = WebCamTexture.devices[0].name;
                isDefaultWebcamSet = true;
            }
            else
                return;
        }
        else
        {
            Debug.Log("The specified webcam '" + specifiedWebcamToLookFor + "' was found.");
        }

        webcamTexture.requestedFPS = requestedFPS;
        webcamTexture.requestedHeight = requestedHeight;
        webcamTexture.requestedWidth = requestedWidth;

        webcamTexture.Play();
    }

    /// <summary>
    /// Makes the camera render with a delay
    /// </summary>
    /// <returns></returns>
    private IEnumerator Render()
    {
        Debug.Log("start Render");

        WaitForEndOfFrame wait = new WaitForEndOfFrame();

        while (true)
        {
            yield return wait;

            capturedFrameIndex = frameIndex % bufferSize;
            frames[capturedFrameIndex].CaptureTextureAndTimeFrom(webcamTexture);

            // Find the index of the frame to render
            // The foor loop is **voluntary** empty
            for (; frames[renderedFrameIndex].CapturedBefore(Time.time - delay); renderedFrameIndex = (renderedFrameIndex + 1) % bufferSize) ;

            //Graphics.Blit(frames[renderedFrameIndex].texture, null as RenderTexture);

            rawimage.texture = frames[renderedFrameIndex].texture;

            frameIndex++;
        }
    }

    public void OnSetDelay(float newDelay)
    {
        StopCoroutine(coroutineRender);

        delay = newDelay < 0 ? 0 : newDelay;

        frameIndex = capturedFrameIndex = renderedFrameIndex = 0;


        bufferSize = (int)(requestedFPS * delay * 20); // compute the buffer size including some margin

        frames = new Frame[bufferSize];

        if (delay > 0f)
            StartCoroutine(coroutineRender);
        else
            rawimage.texture = webcamTexture;

        Debug.Log("Webcam delayed by: " + newDelay + "s (buffer: " + bufferSize + ")");
    }


    public struct Frame
    { 

      /// The texture representing the frame
        public Texture2D texture;
        /// <summary>
        /// The time (in seconds) the frame has been captured at
        /// </summary>
        private float recordedTime;

        /// <summary>
        /// Captures a new frame using the given render texture
        /// </summary>
        /// <param name="renderTexture">The render texture this frame must be captured from</param>
        public void CaptureTextureAndTimeFrom(WebCamTexture webcamTexture)
        {
            // Create a new texture if none have been created yet in the given array index
            if (texture == null)
                texture = new Texture2D(webcamTexture.width, webcamTexture.height);

            // read pixels from webcam texture
            Color [] webcamPixels = webcamTexture.GetPixels();

            texture.SetPixels(webcamPixels);

            texture.Apply();

            recordedTime = Time.time;
        }

        /// <summary>
        /// Indicates whether the frame has been captured before the given time
        /// </summary>
        /// <param name="time">The time</param>
        /// <returns><c>true</c> if the frame has been captured before the given time, <c>false</c> otherwise</returns>
        public bool CapturedBefore(float time)
        {
            return recordedTime < time;
        }
    }

}
