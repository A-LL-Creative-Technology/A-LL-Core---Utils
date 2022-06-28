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

    private Dictionary<string, BUNDLE_STATUS> downloadedBundleStatus;

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

    public IEnumerator GetAssetBundle(string assetURI, string assetName, uint version = 0, Action<UnityEngine.Object> successCallBack = null, Action<string> errorCallBack = null)
    {
        downloadedBundleStatus.Add(assetURI, BUNDLE_STATUS.PENDING);

        UnityWebRequest www = (version > 0) ? UnityWebRequestAssetBundle.GetAssetBundle(assetURI, version, 0) : UnityWebRequestAssetBundle.GetAssetBundle(assetURI);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            downloadedBundleStatus[assetURI] = BUNDLE_STATUS.ERROR;
            errorCallBack?.Invoke(www.error);
        }
        else
        {
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);

            AssetBundleRequest assetRequest = bundle.LoadAssetAsync<GameObject>(assetName);
            yield return assetRequest;

            if (!assetRequest.asset)
            {
                downloadedBundleStatus[assetURI] = BUNDLE_STATUS.EMPTY;
            }
            else
            {
                UnityEngine.Object asset = assetRequest.asset;

                successCallBack?.Invoke(asset);
                bundle.Unload(false);

                downloadedBundleStatus[assetURI] = BUNDLE_STATUS.READY;
            }

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
