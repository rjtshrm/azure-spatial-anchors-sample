using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Microsoft.Azure.SpatialAnchors;

//[RequireComponent(typeof(ARCameraManager))]
public class AzureSpatialAnchor : MonoBehaviour
{

    private AzureSpatialAnchor instance;
    private CloudSpatialAnchorSession cloudSession;

    private ARSession aRSession;
    private ARCameraManager aRCameraManager;

    private Camera m_Camera;
    private XRCameraParams xRCameraParams;

    long lastFrameProcessedTimeStamp = 0;

    [SerializeField] private string Account_Key;
    [SerializeField] private string Account_ID;
    [SerializeField] private string Account_Domain;
    [SerializeField] private TMP_Text feedback;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }



        aRSession = GetComponent<ARSession>(); 
        aRCameraManager = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<ARCameraManager>();

        m_Camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        xRCameraParams = new XRCameraParams
        {
            zNear = m_Camera.nearClipPlane,
            zFar = m_Camera.farClipPlane,
            screenWidth = Screen.width,
            screenHeight = Screen.height,
            screenOrientation = Screen.orientation
        };

        CreateAzureSession();
    }

    void Start()
    {
        
    }
    void OnEnable()
    {
        if (aRCameraManager != null)
        {
            aRCameraManager.frameReceived += HandleFrameReceived;
        }
    }

    void OnDisable()
    {
        if (aRCameraManager != null)
        {
            aRCameraManager.frameReceived -= HandleFrameReceived;
        }
    }


    private void CreateAzureSession()
    {
        Debug.Log("Creating Azure Spatial Session ...");
        // In your view handler
        cloudSession = new CloudSpatialAnchorSession();
        cloudSession.Configuration.AccountKey = @Account_Key;
        cloudSession.Configuration.AccountId = @Account_ID;
        cloudSession.Configuration.AccountDomain = @Account_Domain;

        cloudSession.Session = aRSession.subsystem.nativePtr;

        cloudSession.Start();

        // attach feedbacks
        cloudSession.SessionUpdated += OnSessionUpdated;
    }

    void OnSessionUpdated(object sender, SessionUpdatedEventArgs args)
    {
        var status = args.Status;
        if (status.UserFeedback == SessionUserFeedback.None) return;
        feedback.text += $"Feedback: {Enum.GetName(typeof(SessionUserFeedback), status.UserFeedback)} -" +
            $" Recommend Create={status.RecommendedForCreateProgress: 0.#%} \n";
    }


    void HandleFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        XRCameraFrame xRCameraFrame;
        if (aRCameraManager.subsystem.TryGetLatestFrame(xRCameraParams, out xRCameraFrame))
        {
            long latestFrameTimeStamp = xRCameraFrame.timestampNs;

            bool newFrameToProcess = latestFrameTimeStamp > lastFrameProcessedTimeStamp;

            if (newFrameToProcess)
            {
                cloudSession.ProcessFrame(xRCameraFrame.nativePtr);
                lastFrameProcessedTimeStamp = latestFrameTimeStamp;
            }
        }
    }

}
