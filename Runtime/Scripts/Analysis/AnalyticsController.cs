using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnalyticsController : MonoBehaviour
{
    private DependencyStatus dependencyStatus;
    //private bool firebaseInitialized;

    public enum LOGS
    {
        LOGIN,
        GUEST,
        OPEN_LINK,
        VISIT_PAGE,
        CHANGE_LANGUAGE,
    }

    private Dictionary<LOGS, string> customEvents;
       

    private Dictionary<LOGS, string> customParams;
        

    private static AnalyticsController instance;

    public static AnalyticsController GetInstance()
    {
        return instance;
    }
    
    //static AnalyticsController()
    //{
    //    //customEvents = new Dictionary<LOGS, string>
    //    //{
    //    //    {LOGS.LOGIN, FirebaseAnalytics.EventLogin},
    //    //    {LOGS.GUEST, "logged_as_guest"},
    //    //    {LOGS.OPEN_LINK, "open_external_link"},
    //    //    {LOGS.VISIT_PAGE, "visit_page"},
    //    //};

    //    //customParams = new Dictionary<LOGS, string>
    //    //{
    //    //    {LOGS.OPEN_LINK, "url"},
    //    //    {LOGS.VISIT_PAGE, "page_name"},
    //    //};
    //}

    private void Awake()
    {
        instance = this;

        customEvents = new Dictionary<LOGS, string>
        {
            {LOGS.LOGIN, FirebaseAnalytics.EventLogin},
            {LOGS.GUEST, "logged_as_guest"},
            {LOGS.OPEN_LINK, "open_external_link"},
            {LOGS.VISIT_PAGE, "visit_page"},
            {LOGS.CHANGE_LANGUAGE, "change_language"},
        };

        customParams = new Dictionary<LOGS, string>
        {
            {LOGS.OPEN_LINK, "url"},
            {LOGS.VISIT_PAGE, "page_name"},
            {LOGS.CHANGE_LANGUAGE, "language" },
        };

        

        //firebaseInitialized = false;
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError(
                  "Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });

        

    }

    private void InitializeFirebase()
    {
        //DebugLog("Enabling data collection.");
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

        //DebugLog("Set user properties.");
        // Set the user's sign up method.
        //FirebaseAnalytics.SetUserProperty(
        //  FirebaseAnalytics.UserPropertySignUpMethod,
        //  "CCIF");
        //// Set the user ID.
        //FirebaseAnalytics.SetUserId("uber_user_510");
        //// Set default session duration values.
        ////FirebaseAnalytics.SetMinimumSessionDuration(new TimeSpan(0, 0, 10));
        //FirebaseAnalytics.SetSessionTimeoutDuration(new TimeSpan(0, 30, 0));
        //firebaseInitialized = true;
    }

    // End our analytics session when the program exits.
    void OnDestroy() { }

    public void Log(LOGS log, string value)
    {
        FirebaseAnalytics.LogEvent(customEvents[log], customParams[log], value);
    }

    public void Log(LOGS log)
    {
        FirebaseAnalytics.LogEvent(customEvents[log]);
    }
}
