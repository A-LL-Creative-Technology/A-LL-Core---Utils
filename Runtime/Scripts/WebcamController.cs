using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebcamController : MonoBehaviour
{

    [SerializeField] private string specifiedWebcamToLookFor = "BRIO";

    public RawImage rawimage;
    public GameObject webcamView;

    bool isWebcamFound = false;
    bool isDefaultWebcamSet = false;

    void Start()
    {
        //Hide Cursor
        Cursor.visible = false;

        InitWebcam();
    }

    private void InitWebcam()
    {
        WebCamTexture webcamTexture = new WebCamTexture();

        Debug.Log("Available Webcams");
        
        foreach (WebCamDevice currentDevice in WebCamTexture.devices)
        {
            Debug.Log(currentDevice.name);

            string cameraShortName = currentDevice.name.Split(' ')[0];

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



        webcamTexture.requestedFPS = 60;
        webcamTexture.requestedHeight = 1080;
        webcamTexture.requestedWidth = 1920;

        rawimage.texture = webcamTexture;
        rawimage.material.mainTexture = webcamTexture;

        webcamTexture.Play();
    }
}
