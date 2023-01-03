using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDeserialize : MonoBehaviour
{
   public TextAsset textAsset;
   
   [MyButton(nameof(ShopDatabase))] [SerializeField]
   private int testShow;

   public void ShopDatabase()
   {
      var clone = ScriptableObject.CreateInstance<ShopDatabase>();
      Debug.Log(clone);
      JsonUtility.FromJsonOverwrite(textAsset.text, clone);
//      var shopDatabase = JsonUtility.FromJson<ShopDatabase>(textAsset.text);
      Debug.Log(clone.GetReward(0).amount);
   }
}
