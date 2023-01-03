using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CrossAppManager : BaseFrameworkView
{
    [SerializeField] private RectTransform container;
    [SerializeField] private LocalCrossAppItemView[] localAdsViews;
    [SerializeField] private RemoteCrossAppItemView[] remoteAdsViews;
    [SerializeField] private ToggleScript[] toggleScripts;

   

    protected override void OnKernelLoaded()
    {
        base.OnKernelLoaded();
        foreach (var crossAppItemView in remoteAdsViews)
            crossAppItemView.SetActive();
    }

    private void OnEnable()
    {
        if (!RemoteConfigKey.display_list_view_ads.GetValueBoolean())
        {
            Debug.Log($"FireBaseController.display_list_view_ads:" +RemoteConfigKey.display_list_view_ads.GetValueBoolean());
            
            Show(false);
            return;
        }

        CheckShow();
    }

    private void CheckShow()
    {
        var validShow = PlayerData.playTimes >=
                        RemoteConfigController.GetValue(RemoteConfigKey.level_show_cross_app).LongValue;
        if (validShow)
        {
            SetLocalDisplay();
            int localCount = localAdsViews.Count(x => x.gameObject.activeSelf);
            int totalActive = localCount + _remoteCount;
            Show(totalActive > 0);
        }
        else
            Show(false);
    }

    public void Show(bool val)
    {
        Debug.Log(nameof(Show) + val);
        if (val)
            Shuffle();
        toggleScripts.OnChanged(val);
    }

    private void Shuffle()
    {
        var list = DuongExtensions.ShuffledIndexList(container.childCount);
        var listTransform = new List<Transform>();
        for (int i = 0; i < container.childCount; i++)
            listTransform.Add(container.GetChild(i));

        for (var i = 0; i < listTransform.Count; i++)
            listTransform[list[i]].SetAsFirstSibling();
    }

    private void SetLocalDisplay()
    {
        var ignoreSkuKeys = RemoteConfigController.GetValue(RemoteConfigKey.app_sku_ignore_list).StringValue.Split(',');
        foreach (var localCrossAppItemView in localAdsViews)
        {
            var ignore = ignoreSkuKeys.Any(x => string.Equals(x, localCrossAppItemView.appData.sku.ToString()));
            localCrossAppItemView.gameObject.SetActive(!ignore);
        }
    }

    private int _remoteCount = 0;

    public void EnableRemoteAds(RemoteCrossAppDataItem remoteData, Texture2D texture)
    {
        remoteAdsViews[_remoteCount].gameObject.SetActive(true);
        remoteAdsViews[_remoteCount].SetData(remoteData, texture);
        _remoteCount++;
    }
}