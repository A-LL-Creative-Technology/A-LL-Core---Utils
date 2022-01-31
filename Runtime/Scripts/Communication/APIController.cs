using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Proyecto26; // Rest Client
using SimpleJSON;
using UnityEngine;
using UnityEngine.Localization.Settings;
//using UnityEngine.Localization.Settings;
using UnityEngine.Networking;

public class APIController : MonoBehaviour
{

    //private static APIController instance;
    public static APIController instance;

    public static APIController GetInstance()
    {
        return instance;
    }

#pragma warning disable 0649

    public static event EventHandler OnLogout;

    // Parameters
    readonly static private bool ENABLE_DEBUG = false;

    public string serverURL;          //address of the API to call

    [HideInInspector]
    public int nbRequestsInProgress = 0;

#pragma warning restore 0649

    private void Awake()
    {
        instance = this;
    }

    private void HandleError(RequestException requestException, string endpoint, Action<RequestException> errorCallback = null, bool showAPIErrorNotification = true)
    {
        GlobalController.LogMe("Error in the API call: " + endpoint + " - " + requestException.Message + " - " + requestException.Response);

        nbRequestsInProgress--;
        //Debug.Log("Simul -- " + nbRequestsInProgress + " " + endpoint);

        // we verify if the error has a specific error code
        if (requestException.StatusCode == 401)
        {
            // fires the event to notify that cache has been loaded
            if (OnLogout != null)
                OnLogout(GetInstance(), EventArgs.Empty);

        }

        JSONNode data = JSON.Parse(requestException.Response);

        if (data != null && data.HasKey("errors"))
        {
            if (showAPIErrorNotification)
            {
                //Get the first error
                NavigationController.GetInstance().OnNotificationOpen(false, -1, "Erreur de connexion", NavigationController.GetInstance().notificationStringPrefix + data["errors"][0][0]);
            }
        }
        else if (data.HasKey("message"))
        {
            if (showAPIErrorNotification)
            {
                //Fallback on the message
                NavigationController.GetInstance().OnNotificationOpen(false, -1, "Erreur de connexion", NavigationController.GetInstance().notificationStringPrefix + data["message"]);
            }
        }
        else
        {
            if (showAPIErrorNotification)
            {
                //Fallback
                NavigationController.GetInstance().OnNotificationOpen(false, -1, "Erreur de connexion", "CODE_General_Connection_Error");
            }
        }


        errorCallback?.Invoke(requestException);
    }

    private IEnumerator EnsureRequestCanBeSent(Action callback, bool shallWaitForPendingRequests = false)
    {
        while (!ALLCoreConfig.isALLCoreReady)
            yield return null;

        while (shallWaitForPendingRequests && nbRequestsInProgress > 0)
        {
            //Debug.Log("Wait");
            yield return null;
        }

        nbRequestsInProgress++;

        //Debug.Log("Simul ++ " + nbRequestsInProgress);

        callback.Invoke();
    }

    //Test user connected
    public static bool IsConnected(string customAPIToken = "", string customAPITokenExpiresAt = "")
    {
        bool hasLastUpdate = !String.IsNullOrEmpty(CacheController.GetInstance().apiConfig.last_update);

        string apiToken = String.IsNullOrEmpty(customAPIToken) ? CacheController.GetInstance().apiConfig.api_token : customAPIToken;
        string apiTokenExpiresAt = String.IsNullOrEmpty(customAPITokenExpiresAt) ? CacheController.GetInstance().apiConfig.api_token_expires_at : customAPITokenExpiresAt;

        bool hasValidToken = !String.IsNullOrEmpty(apiToken) && DateTime.Parse(apiTokenExpiresAt) > DateTime.Now;

        return hasLastUpdate && hasValidToken;
    }

    //GET a SINGLE element of type R
    public void Get<R>(string endpoint, Dictionary<string, string> parameters, Action<R> successCallback, Action<RequestException> errorCallback = null, string customServerURL = null, string customAPIToken = null, bool showAPIErrorNotification = true, bool shallWaitForPendingRequests = false)
    {
        StartCoroutine(EnsureRequestCanBeSent(() =>
        {
            //Debug.Log("Get " + endpoint);

            //Send the request
            RestClient.Get<R>(BuildRequest(endpoint, parameters, customServerURL, customAPIToken))
               .Then(res =>
               {
                   nbRequestsInProgress--;
                   //Debug.Log("Simul -- " + nbRequestsInProgress + " " + endpoint);


                   successCallback?.Invoke(res);
               })
               .Catch(err =>
               {
                   HandleError((RequestException)err, endpoint, errorCallback, showAPIErrorNotification);
               });

        }, shallWaitForPendingRequests));
    }

    //POST a request with no particular types
    public void Post(string endpoint, Dictionary<string, string> parameters, Action<ResponseHelper> successCallback, Action<RequestException> errorCallback = null, string customServerURL = null, string customAPIToken = null, bool showAPIErrorNotification = true, bool shallWaitForPendingRequests = false)
    {

        StartCoroutine(EnsureRequestCanBeSent(() =>
        {
            //Debug.Log("Post " + endpoint);


            RestClient.Post(BuildRequest(endpoint, parameters, customServerURL, customAPIToken))
                .Then(res =>
                {
                    nbRequestsInProgress--;
                    //Debug.Log("Simul -- " + nbRequestsInProgress + " " + endpoint);

                    successCallback?.Invoke(res);
                })
                .Catch(err =>
                {
                    HandleError((RequestException)err, endpoint, errorCallback, showAPIErrorNotification);
                });
        }, shallWaitForPendingRequests));
    }

    //POST a request with an object of type S to the servers and expects an element of type R from the server
    public void Post<R>(string endpoint, Dictionary<string, string> parameters, Action<R> callback, Action<RequestException> errorCallback = null, string customServerURL = null, string customAPIToken = null, bool showAPIErrorNotification = true, bool shallWaitForPendingRequests = false)
    {
        StartCoroutine(EnsureRequestCanBeSent(() =>
        {
            //Debug.Log("POST R " + endpoint);


            RestClient.Post<R>(BuildRequest(endpoint, parameters, customServerURL, customAPIToken))
                .Then(res =>
                {
                    nbRequestsInProgress--;
                    //Debug.Log("Simul -- " + nbRequestsInProgress + " " + endpoint);

                    callback?.Invoke(res);
                })
                .Catch(err =>
                {
                    HandleError((RequestException)err, endpoint, errorCallback, showAPIErrorNotification);
                });
        }, shallWaitForPendingRequests));
    }

    //POST a file to the server
    public void Post<S, R>(S requestObject, string endpoint, Dictionary<string, string> parameters, List<File> files, Action<R> callback, Action<RequestException> errorCallback = null, string customServerURL = null, string customAPIToken = null, bool showAPIErrorNotification = true, bool shallWaitForPendingRequests = false)
    {
        StartCoroutine(EnsureRequestCanBeSent(() =>
        {
            //Debug.Log("POST S R " + endpoint);


            RestClient.Post<R>(BuildRequest(requestObject, endpoint, parameters, files, customServerURL, customAPIToken))
                .Then(res =>
                {
                    nbRequestsInProgress--;
                    //Debug.Log("Simul -- " + nbRequestsInProgress + " " + endpoint);

                    callback?.Invoke(res);
                })
                .Catch(err =>
                {
                    HandleError((RequestException)err, endpoint, errorCallback, showAPIErrorNotification);
                });
        }, shallWaitForPendingRequests));
    }

    //POST a JSON to the server
    public void Post<S, R>(S requestObject, string endpoint, Dictionary<string, string> parameters, Action<R> callback, Action<RequestException> errorCallback = null, string customServerURL = null, string customAPIToken = null, bool showAPIErrorNotification = true, bool shallWaitForPendingRequests = false)
    {
        StartCoroutine(EnsureRequestCanBeSent(() =>
        {

            RestClient.Post<R>(BuildRequestJson(requestObject, endpoint, parameters, customServerURL, customAPIToken))
                .Then(res =>
                {
                    nbRequestsInProgress--;
                    //Debug.Log("Simul -- " + nbRequestsInProgress + " " + endpoint);

                    callback?.Invoke(res);
                })
                .Catch(err =>
                {
                    HandleError((RequestException)err, endpoint, errorCallback, showAPIErrorNotification);
                });
        }, shallWaitForPendingRequests));
    }

    public void GetImage(string imageUri, Action<Texture2D> callback, Action<RequestException> errorCallback = null, bool showAPIErrorNotification = true, bool shallWaitForPendingRequests = false)
    {
        StartCoroutine(EnsureRequestCanBeSent(() =>
        {
            //Debug.Log("Get Image" + imageUri);


            RestClient.Get(new RequestHelper
            {
                Uri = imageUri, // url is insecure as Kentico staging is not in https (an exception for that domain has been added to InfoPlistUpdater.cs in the Editor folder of Unity)
                DownloadHandler = new DownloadHandlerTexture(),
            }).Then(res =>
            {
                nbRequestsInProgress--;
                //Debug.Log("Simul -- " + nbRequestsInProgress + " " + imageUri);

                Texture2D texture = ((DownloadHandlerTexture)res.Request.downloadHandler).texture;
                callback?.Invoke(texture);
            }).Catch(err =>
            {
                RequestException requestException = (RequestException)err;

                // if no network, we do not display the error but simply call the errorCallback
                if (requestException.IsNetworkError)
                {
                    nbRequestsInProgress--;
                    //Debug.Log("Simul -- " + nbRequestsInProgress + " " + imageUri);

                    errorCallback?.Invoke(requestException);
                }
                else
                {
                    HandleError(requestException, imageUri, errorCallback, showAPIErrorNotification);
                }

            });
        }, shallWaitForPendingRequests));
    }

    //Build request header and body
    private RequestHelper BuildRequest(string endpoint, Dictionary<string, string> parameters, string customeServerURL = null, string customAPIToken = null)
    {
        AddHeaders(customAPIToken);

        // add localization to header
        if (CacheController.GetInstance().appConfig.lang != "")
        {
            if (parameters == null)
            {
                parameters = new Dictionary<string, string>();
            }

            if (!parameters.ContainsKey("lang")) // make sure we are not enforcing lang parameter from API caller
            {
                parameters.Add("lang", CacheController.GetInstance().appConfig.lang);
            }
        }

        string uri = (customeServerURL == null) ? serverURL : customeServerURL;
        return new RequestHelper
        {
            Uri = SecureURL(uri) + "/" + endpoint,
            Params = parameters,
            EnableDebug = ENABLE_DEBUG,
        };

    }

    private RequestHelper BuildRequest<B>(B body, string endpoint, Dictionary<string, string> parameters, List<File> files, string customServerURL = null, string customAPIToken = null)
    {
        RequestHelper request = BuildRequest(endpoint, parameters, customServerURL, customAPIToken);
        request.FormSections = GenerateFormData(body, files);
        request.Body = body;

        return request;
    }

    private RequestHelper BuildRequestJson<B>(B body, string endpoint, Dictionary<string, string> parameters, string customServerURL = null, string customAPIToken = null)
    {
        RequestHelper request = BuildRequest(endpoint, parameters, customServerURL, customAPIToken);
        request.Body = body;

        return request;
    }

    //Add Custom headers to the request
    private void AddHeaders(string customAPIToken = null)
    {
        RestClient.DefaultRequestHeaders["X-Requested-With"] = "XMLHttpRequest";
        AddAuthorizationHeader(customAPIToken);
    }

    //Add access_token and token_type to the Authorization header.
    private void AddAuthorizationHeader(string customAPIToken = null)
    {
        string apiToken = (String.IsNullOrEmpty(customAPIToken)) ? CacheController.GetInstance().apiConfig.api_token : customAPIToken;

        if (String.IsNullOrEmpty(apiToken))
            return;

        RestClient.DefaultRequestHeaders["Authorization"] = "Bearer " + apiToken;
    }



    //Generates a form data for file upload.
    private List<IMultipartFormSection> GenerateFormData<B>(B body, List<File> files)
    {
        //Generate Dictionnary from body
        Dictionary<string, object> bodyDictionary = body.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(prop => prop.Name, prop => prop.GetValue(body));


        //Generate form data
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        //Add sections
        foreach (KeyValuePair<string, object> entry in bodyDictionary)
        {
            if (entry.Value != null && !string.IsNullOrEmpty(entry.Value.ToString()))
                formData.Add(new MultipartFormDataSection(entry.Key, entry.Value.ToString()));
        }

        //Add files
        foreach (File file in files)
        {
            formData.Add(new MultipartFormFileSection(file.field, file.data, file.fileName, file.contentType));
        }

        return formData;
    }

    // convert HTTP to HTTPS (iOS will crash by default)
    private string SecureURL(string url)
    {
        int indexOfSlash = url.IndexOf("/");

        if (url.Length == 0 || indexOfSlash == -1)
        {
            GlobalController.LogMe("Erreur parsing the URL for security: " + url);
        }

        url = "https:" + url.Substring(indexOfSlash);

        return url;
    }

    //File structure
    public class File
    {
        public string field;
        public byte[] data;
        public string fileName;
        public string contentType;
    }
}
