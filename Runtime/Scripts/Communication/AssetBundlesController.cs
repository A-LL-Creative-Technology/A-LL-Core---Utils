using System.Collections;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Networking;
using System.Collections.Generic;

public class AssetBundlesController : MonoBehaviour
{
    private static AssetBundlesController instance;

    public static AssetBundlesController GetInstance()
    {
        return instance;
    }

    private enum BUNDLE_STATUS
    {
        PENDING,
        READY,
        ERROR,
        EMPTY,
    }

    private Dictionary<string, BUNDLE_STATUS> downloadedBundleStatus = new Dictionary<string, BUNDLE_STATUS>();

    private void Awake()
    {
        if (instance != null && instance != this)
            Destroy(this.gameObject);
        else
            instance = this;
    }

    private void Start()
    {
        downloadedBundleStatus = new Dictionary<string, BUNDLE_STATUS>();
    }

    public IEnumerator DownloadAssetBundle(string assetURI, string assetName, uint version = 0, Action<AssetBundle> successCallBack = null, Action<string> errorCallBack = null)
    {
        downloadedBundleStatus.Add(assetName, BUNDLE_STATUS.PENDING);

        UnityWebRequest www = (version > 0) ? UnityWebRequestAssetBundle.GetAssetBundle(assetURI, version, 0) : UnityWebRequestAssetBundle.GetAssetBundle(assetURI);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            downloadedBundleStatus[assetURI] = BUNDLE_STATUS.ERROR;
            Debug.Log(www.error);
            errorCallBack?.Invoke(www.error);
        }
        else
        {
            downloadedBundleStatus[assetURI] = BUNDLE_STATUS.READY;

            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);

            if (successCallBack != null)
                successCallBack.Invoke(bundle);
            else
                bundle.Unload(false);
        }
    }


    public void DownloadAndUnpackAssetBundle<T>(string assetURI, string assetName, uint version = 0, Action<UnityEngine.Object> successCallBack = null, Action<string> errorCallBack = null)
    {
        StartCoroutine(DownloadAssetBundle(assetURI, assetName, version, (AssetBundle bundle) =>
        {
            StartCoroutine(UnpackAssetBundle<T>(assetName, bundle, successCallBack, errorCallBack));
        }, errorCallBack));
    }

    public IEnumerator UnpackAssetBundle<T>(string assetName, AssetBundle bundle, Action<UnityEngine.Object> successCallBack = null, Action<string> errorCallBack = null)
    {
        AssetBundleRequest assetRequest = bundle.LoadAssetAsync<T>(assetName);

        yield return assetRequest;

        if (!assetRequest.asset)
        {
            downloadedBundleStatus[assetName] = BUNDLE_STATUS.EMPTY;
        }
        else
        {
            UnityEngine.Object asset = assetRequest.asset;

            successCallBack?.Invoke(asset);
            bundle.Unload(false);

            downloadedBundleStatus[assetName] = BUNDLE_STATUS.READY;
        }
    }

    public IEnumerator UnpackAllAssetBundle<T>(string bundleName, AssetBundle bundle, Action<UnityEngine.Object> successCallBack = null, Action<string> errorCallBack = null)
    {
        AssetBundleRequest assetRequest = bundle.LoadAllAssetsAsync<T>();

        yield return assetRequest;

        if (assetRequest.allAssets.Length == 0)
        {
            downloadedBundleStatus[bundleName] = BUNDLE_STATUS.EMPTY;
        }
        else
        {

            foreach (UnityEngine.Object item in assetRequest.allAssets)
            {
                successCallBack?.Invoke(item);
            }

            bundle.Unload(false);

            downloadedBundleStatus[bundleName] = BUNDLE_STATUS.READY;
        }
    }

    //Return false if there are still pending requests
    public bool IsDownloadOver()
    {
        foreach (BUNDLE_STATUS status in downloadedBundleStatus.Values)
        {
            if (status == BUNDLE_STATUS.PENDING)
                return false;
        }

        return true;
    }

    public void ClearDownloadQueue()
    {
        if (downloadedBundleStatus != null)
            downloadedBundleStatus.Clear();
        else
            downloadedBundleStatus = new Dictionary<string, BUNDLE_STATUS>();
    }
}
