using System;
using System.Collections;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


public class RemoteCrossAppItemView : MonoBehaviour
{
    [SerializeField] private LocalCrossAppItemView localApp;

    private bool Active { get; set; }

    public void SetData(RemoteCrossAppDataItem data, Texture2D texture)
    {
        Active = true;
        localApp.gameObject.SetActive(false);
        localApp.appData = new LocalCrossAppData()
        {
            Url = data.store_link,
            appName = data.app_name
        };
        localApp.SetTexture(texture, data.total_frame);
        localApp.gameObject.SetActive(true);
    }

    public void SetActive()
    {
        gameObject.SetActive(Active);
    }
}