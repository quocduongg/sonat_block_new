using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RemoteEnableKey
{
    FacebookLogin = 0,
    Vip = 1,
}

public class RemoteConfigEnable : BaseFrameworkView
{
    [SerializeField] private ToggleScript[] targets;
    [SerializeField] private RemoteEnableKey key;

    public void OnEnable()
    {
        Debug.Log(RemoteConfigKey.remote_enable.GetValueString());
        int castKey = (int) key;
        try
        {
            var str = RemoteConfigKey.remote_enable.GetValueString();
            if (!string.IsNullOrEmpty(str))
            {
                var listEnable =  DuongSerializationExtensions.ListIntFromString(str);
                foreach (var i in listEnable)
                {
                    if (i == castKey)
                    {
                        targets.OnChanged(true);
                        return;
                    }
                }
            }
            targets.OnChanged(false);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
       
    }
}