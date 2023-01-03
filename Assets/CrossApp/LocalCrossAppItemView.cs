using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public enum AppSku
{
    block_jewel,
    block_1010,
    bubble_dino,
    bubble_panda,
    cat_bubble,
    dino_sort,
    tittle_connect,
    water_sort,
    ball_sort,
    no_use,
    water_sort2
}

[Serializable]
public struct LocalCrossAppData
{
    public AppSku sku;
    [SerializeField] private string url;
    [SerializeField] private string url_ios;
    public string appName;

    public string Url
    {
        set
        {
#if UNITY_ANDROID
            url = value;
#else
         url_ios = value;
#endif
        }
        get
        {
#if UNITY_ANDROID
            return url;
#else
return url_ios;
#endif
        }
    }
}

public class LocalCrossAppItemView : MonoBehaviour
{
    public LocalCrossAppData appData;
    [SerializeField] private Texture2D texture2D;

    public Button btnOpenLink;
    public RawImage imgDisplay;
    public RawImage imgIcon;
    public RectTransform downloadIcon;

    private int Row => Mathf.CeilToInt(totalFrame / 10f);
    public int column = 10;
    public int totalFrame = 50;
    public bool startOnEnable = true;
    public bool test = true;
    public float time = 2f;

    void Start()
    {
        btnOpenLink.onClick.AddListener(() =>
        {
            AdsManager.AppLeaving = true;
            Application.OpenURL(appData.Url);
            Kernel.Resolve<FireBaseController>().LogEvent("click_cross_app_ads",
                new LogParameter("app_name", appData.appName));
        });
    }

    void OnEnable()
    {
        downloadIcon.DOKill();
        downloadIcon.gameObject.SetActive(false);
        imgIcon.gameObject.SetActive(false);

        if (startOnEnable)
            Observable.FromCoroutine(StartSpriteSheet)
                .TakeUntilDisable(this)
                .Subscribe();
    }

    void OnDisable()
    {
        downloadIcon.DOKill();
        downloadIcon.gameObject.SetActive(false);
        imgIcon.gameObject.SetActive(false);
    }

    [ContextMenu("test")]
    void Test()
    {
        StartCoroutine(StartSpriteSheet());
    }

    IEnumerator StartSpriteSheet()
    {
        imgDisplay.color = Color.white;
        imgIcon.gameObject.SetActive(false);


        int i = UnityEngine.Random.Range(0, totalFrame - 2);
        imgDisplay.uvRect = GetFrame(i);
        float t = i * 1f / (totalFrame - 2) * time;

//        yield return new WaitForSeconds(1);
        while (t < time)
        {
            t += Time.deltaTime;
            i = Mathf.Clamp((int) (t / time * (totalFrame - 2)), 0, totalFrame - 2);
            imgDisplay.uvRect = GetFrame(i);

            yield return null;
            if (t > time)
            {
                t = 0;
                yield return imgDisplay.DOColor(new Color(0.3f, 0.3f, 0.3f, 1), 0.5f).WaitForCompletion();
                yield return new WaitForSeconds(0.5f);

                imgIcon.gameObject.SetActive(true);
                imgIcon.rectTransform.localScale = Vector3.zero;
                yield return imgIcon.rectTransform.DOScale(Vector3.one, 0.3f).WaitForCompletion();

                downloadIcon.gameObject.SetActive(true);

                downloadIcon.localScale = Vector3.one;
                downloadIcon.anchoredPosition = new Vector2(0, -400);
                downloadIcon.DOAnchorPos(Vector2.zero, 0.5f);
                downloadIcon.DOScale(new Vector3(1.1f, 1.1f, 1.1f), 0.25f).SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutBack);

                yield return new WaitForSeconds(3);

                downloadIcon.DOKill();
                downloadIcon.gameObject.SetActive(false);
                imgIcon.gameObject.SetActive(false);
                imgDisplay.color = Color.white;
            }
        }
    }

    public int frame = -1;

    void Update()
    {
        if (test && frame >= 0)
            OnFrame(frame);
    }

    private void OnFrame(int i)
    {
        imgDisplay.uvRect = GetFrame(i);
    }


    [ContextMenu("set icon")]
    void Test2()
    {
        SetIcon(int.Parse(Regex.Match(texture2D.name, @"\d+").Value));
    }

    private void SetIcon(int nFrame)
    {
        totalFrame = (frame == 0) ? 70 : nFrame;
        gameObject.name = appData.sku.ToString();
        imgDisplay.texture = texture2D;
        imgIcon.texture = texture2D;
        imgIcon.uvRect = GetFrame(totalFrame - 1);
    }

    private Rect GetFrame(int i)
    {
        var interval = 1f / Row;
        var intervalCol = 1f / column;
        return new Rect(
            i % column * intervalCol,
            Mathf.Max((Row - i / column - 1) * interval, 0),
            intervalCol,
            interval);
    }

    public void SetTexture(Texture2D texture, int nFrame)
    {
        texture2D = texture;
        SetIcon(nFrame);
    }
}