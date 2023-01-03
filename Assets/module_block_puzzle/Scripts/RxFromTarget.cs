using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class RxFromTarget : BaseRxAnimator
{
    [SerializeField] private RectTransform[] froms;
    [SerializeField] private Vector3 to;
    [SerializeField] private Vector3 fromScale = new Vector3(1, 1, 1);
    [SerializeField] private Vector3 toScale = new Vector3(1, 1, 1);
    [SerializeField] private AnimationCurve moveCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    [SerializeField] private AnimationCurve scaleCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    [SerializeField] float duration = 1;
    [SerializeField] float durationScale = 1;

    private RectTransform from => froms[index];
    
    [SerializeField] [MyButton(nameof(Test))]
    private bool test;

    public void Test()
    {
        ResetAnimation();
        var rt = GetComponent<RectTransform>();
        rt.DOAnchorPos(to, duration).SetEase(moveCurve);
        rt.DOScale(toScale, durationScale).SetEase(moveCurve);
    }

    public override void ResetAnimation(bool setActive = false)
    {
        base.ResetAnimation(setActive);
        var rt = GetComponent<RectTransform>();
        rt.position = from.position;
        rt.localScale = fromScale;
    }

    public override void SetAtAnimationIn(bool setActive = false)
    {
        base.SetAtAnimationIn(setActive);
        var rt = GetComponent<RectTransform>();
        rt.anchoredPosition = to;
        rt.localScale = toScale;
    }

    public override bool HasOut()
    {
        return true;
    }

    public override bool HasIn()
    {
        return true;
    }

    protected override IEnumerator ThisDoAnimate_In()
    {
        var rt = GetComponent<RectTransform>();
        rt.position = from.position;
        rt.localScale = fromScale;

        rt.DOAnchorPos(to, duration).SetEase(moveCurve);
        rt.DOScale(toScale, durationScale).SetEase(moveCurve);
        
        yield return new WaitForSeconds(Mathf.Max(duration,durationScale));

    }

    protected override IEnumerator ThisDoAnimate_Out()
    {
        var rt = GetComponent<RectTransform>();
        rt.DOMove(@from.position, duration).SetEase(moveCurve);
        rt.DOScale(fromScale, durationScale).SetEase(scaleCurve);
        
        yield return new WaitForSeconds(Mathf.Max(duration,durationScale));
    }

    protected override IEnumerator ThisDoAnimate_InThenOut()
    {
        var rt = GetComponent<RectTransform>();
        rt.position = from.position;
        rt.localScale = fromScale;

        rt.DOAnchorPos(to, duration).SetEase(moveCurve);
        rt.DOScale(toScale, durationScale).SetEase(scaleCurve);
        
        yield return new WaitForSeconds(Mathf.Max(duration,durationScale));

        rt.DOMove(@from.position, duration).SetEase(moveCurve);
        rt.DOScale(fromScale, durationScale).SetEase(scaleCurve);
        
        yield return new WaitForSeconds(Mathf.Max(duration,durationScale));
    }
}