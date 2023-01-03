using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class VerifyIapData
{
    public IapKind kind;
    public string package_name;
    public string sku_id;
    public string purchase_token;
}

public enum IapKind
{
    product,
    subscription,
}
public class TestVerifyIAP : MonoBehaviour
{
    [MyButton(nameof(TestVerifyIap))] public int abc;
    // Start is called before the first frame update
    public string url = "https://us-central1-sonat-arm-358507.cloudfunctions.net/verify_inapp_purchase";
    public VerifyIapData data;
    
    public void TestVerifyIap()
    {
        WWWForm form = new WWWForm();
        form.AddField("kind", data.kind.ToString());
        form.AddField("package_name", data.package_name);
        form.AddField("sku_id", data.sku_id);
        form.AddField("purchase_token", data.purchase_token);

        StartCoroutine(IeVerify(form));
        
//        ObservableWWW.Post(url,form)
//            .Subscribe(
//                x =>
//                {
//                    Debug.Log(x);
//                }, // onSuccess
//                ex =>
//                {
//                    Debug.LogException(ex);
//                }); 
    }

    public void VerifyIap(string kind, string packageName, string skuId, string purchaseToken)
    {
        WWWForm form = new WWWForm();
        form.AddField("kind", kind);
        form.AddField("package_name", packageName);
        form.AddField("sku_id", skuId);
        form.AddField("purchase_token", purchaseToken);
        StartCoroutine(IeVerify(form));
    }
    
    IEnumerator IeVerify(WWWForm form)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();
            Debug.Log(www.responseCode);
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
              
                Debug.Log(www.downloadHandler.text);
            }
        }
    }

   
}
