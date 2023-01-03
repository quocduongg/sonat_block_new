using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class CustomButtonAnimation : ToggleScript,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerClickHandler
{
    [SerializeField] private Vector3 scaleOut = new Vector3(1.1f,1.1f,1.1f);

    private Vector3 _startScale;
    [SerializeField] private float time = .05f;

    
    void Awake()
    {
        _startScale = transform.localScale;
    }

    public override void Load()
    {
        
    }
    
    
    private IDisposable _tween;

    [SerializeField] private bool useLast;
    [SerializeField] private bool onlyWhenChange;
    private bool _last;
    protected override void Trigger(bool val)
    {
        if (useLast)
        {
            if (val != _last)
                _last = val;
            else
                return;
        }
        if (custom)
        {
            _tween?.Dispose();
            _t = val ? 1f : 0;
            Set();
            busy = false;
        }
        else
        {
            if (!busy)
            {
                _tween?.Dispose();
                _tween = Observable.FromMicroCoroutine(Tween)
                    .TakeUntilDisable(this).Subscribe();
            }
        }
    }

    private void OnDisable()
    {
        _tween?.Dispose();
        CurrentValue = false;
        Set(0);
    }
    
    private void OnEnable()
    {
        _tween?.Dispose();
        CurrentValue = false;
        Set(0);
    }

    [SerializeField] private bool busy;
    private float _t;
    IEnumerator Tween()
    {
        busy = true;
        _t = CurrentValue ? 0 : 1;
        Set();
        yield return null;
        while (CurrentValue ? (_t < 1f) : (_t > 0f))
        {
            _t += Time.unscaledDeltaTime /  (CurrentValue ? time : -time);
            if (Mathf.Abs(_t) >= 1)
                _t = CurrentValue ? 1 : 0;
            Set();
            yield return null;
        }

        if (_t <= 0.01f)
            transform.localScale = _startScale;
        busy = false;
    }
    
    private void Set()
    {
        Set(_t);
    }
    
    private void Set(float t)
    {
        transform.localScale = Vector3.Lerp(_startScale, scaleOut, t);
    }

    
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnChanged(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnChanged(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnChanged(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnChanged(true);
    }
}
