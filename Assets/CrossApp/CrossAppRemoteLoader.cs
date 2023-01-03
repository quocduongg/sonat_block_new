using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UniRx;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Networking;

[Serializable]
public struct RemoteCrossAppDataItem
{
    public string app_name;
    public string store_link;
    public string type_app;
    public string img_link;
    public int total_frame;
}

[Serializable]
public struct CrossAppData
{
    public RemoteCrossAppDataItem[] list_view_ads;
}

public class CrossAppRemoteLoader : MonoBehaviour
{
    public float timeGapBetween = 2;
    public CrossAppManager crossAppManager;
   
    private CrossAppData _data;

    private void Start()
    {
        if (FireBaseController.FireBaseRemoteReady)
            StartLoading();
        else
            SubjectController.FireBaseRemoteReadyEvent.Take(1).Subscribe(data => StartLoading());
    }

    private void StartLoading()
    {
        if (FireBaseController.FireBaseRemoteReady)
        {
            Debug.Log("start to load cross app data");
            if (!string.IsNullOrEmpty(RemoteConfigKey.cross_app_data.GetValueString()))
            {
                Debug.Log("download texture...");
                try
                {
                    var data = JsonUtility.FromJson<CrossAppData>(RemoteConfigKey.cross_app_data.GetValueString());
                    Debug.Log("Load data from FireBase:" + data.list_view_ads[0].img_link);
                    LoadAdsData(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
    }


    private void LoadAdsData(CrossAppData crossAppData)
    {
        if (!RemoteConfigKey.display_list_view_ads.GetValueBoolean())
        {
            Debug.Log($"FireBaseController.display_list_view_ads:" +RemoteConfigKey.display_list_view_ads.GetValueBoolean());
            return;
        }

        _data = crossAppData;
        for (var i = 0; i < _data.list_view_ads.Length; i++)
        {
            var i1 = i;
            Observable.Timer(TimeSpan.FromSeconds(i * timeGapBetween + 1))
                .Subscribe(_ =>
                {
                    Debug.Log($"start load cross ads {crossAppData.list_view_ads[i1].app_name} :{crossAppData.list_view_ads[i1].img_link}");

                    GetWwwTexture2D(_data.list_view_ads[i1].img_link)
                        .DoOnError(Debug.LogError)
                        .Subscribe(texture2D =>
                        {
                            Debug.Log("loaded cross ads " + crossAppData.list_view_ads[i1].app_name);
                            crossAppManager.EnableRemoteAds(_data.list_view_ads[i1], texture2D);
                        });
                });
        }
    }


    private static IObservable<Texture2D> GetWwwTexture2D(string url)
    {
        return Observable.FromCoroutine<Texture2D>((observer, cancellationToken) =>
            GetWWWCore(url, observer, cancellationToken));
    }

    // ReSharper disable once InconsistentNaming
    private static IEnumerator GetWWWCore(string url, IObserver<Texture2D> observer, CancellationToken cancellationToken)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        www.SendWebRequest();

        while (!www.isDone && !cancellationToken.IsCancellationRequested)
            yield return null;

        if (cancellationToken.IsCancellationRequested) yield break;

        if (www.error != null)
        {
            observer.OnError(new Exception(www.error));
        }
        else
        {
            var texture = ((DownloadHandlerTexture) www.downloadHandler).texture;
            observer.OnNext(texture);
            observer.OnCompleted(); // IObserver needs OnCompleted after OnNext!
        }
    }

}