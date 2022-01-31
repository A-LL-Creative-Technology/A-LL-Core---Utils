using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class NotificationController : MonoBehaviour
{
    private static NotificationController instance;

    public static NotificationController GetInstance()
    {
        return instance;
    }

#if UNITY_ANDROID
    AndroidNotificationChannel channel;
#endif

    private void Awake()
    {
        instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private bool isCurrentlyPaused = false;
    private Firebase.FirebaseApp app; //Used by Firebase.

    public delegate void OnNotificationReceived(Firebase.Messaging.MessageReceivedEventArgs pushNotificationMessage);
    public static event OnNotificationReceived notificationReceivedDelegate;

    public delegate void OnDependenciesCheckedAndFixed();
    public static event OnDependenciesCheckedAndFixed dependeciesCheckedAndFixed;

    private void Start()
    {

        // for push notification
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                app = Firebase.FirebaseApp.DefaultInstance;

                Firebase.Messaging.FirebaseMessaging.TokenReceived += OnPushNotificationTokenReceived;
                Firebase.Messaging.FirebaseMessaging.MessageReceived += OnPushNotificationReceived;

                // Set a flag here to indicate whether Firebase is ready to use by your app.

                if (dependeciesCheckedAndFixed != null)
                    dependeciesCheckedAndFixed();
            }
            else
            {
                GlobalController.LogMe(String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }

    private void OnDestroy()
    {
        Firebase.Messaging.FirebaseMessaging.TokenReceived -= OnPushNotificationTokenReceived;
        Firebase.Messaging.FirebaseMessaging.MessageReceived -= OnPushNotificationReceived;
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (!isCurrentlyPaused && isPaused)
        {
            // we just entered background
            GlobalController.LogMe("Application just entered background");

#if UNITY_IOS
            // synchronize the badge number
            SyncBadgeCount();
#endif
        }

        if (isCurrentlyPaused && !isPaused)
        {
            // we just entered foreground
            GlobalController.LogMe("Application just entered foreground");
        }

        isCurrentlyPaused = isPaused;
    }

#if UNITY_IOS
    private void SyncBadgeCount()
    {
        // adjust badge number
        iOSNotificationCenter.ApplicationBadge = iOSNotificationCenter.GetDeliveredNotifications().Length;
    }
#endif

    public void OnPushNotificationTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
    {
        GlobalController.LogMe("Received the push notification registration token: " + token.Token);
    }

    public void OnPushNotificationReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs pushNotificationMessage)
    {

        // wait for the A-LL Core to be ready before processing the notification (it will cause a crash otherwise)
        StartCoroutine(WaitForALLCoreReady(() => {

            //if notification has been opened
            if (pushNotificationMessage.Message.NotificationOpened)
            {
                // we then extract the data from the notification
                if (pushNotificationMessage.Message.Data.Count > 0 && notificationReceivedDelegate != null)
                    notificationReceivedDelegate(pushNotificationMessage);
            }

        }));


    }


    private IEnumerator WaitForALLCoreReady(Action callback)
    {
        while (!ALLCoreConfig.isALLCoreReady)
            yield return null;

        callback?.Invoke();
    }


}