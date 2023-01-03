using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartTestLevelButton : MonoBehaviour
{
   [SerializeField] private int level;
   [SerializeField] private TextAsset source;

   void Start()
   {
      GetComponent<Button>().onClick.AddListener(() =>
      {
         RootView.rootView.gameController.StartLevelDirty(JsonUtility.FromJson<MapListPointCollection>(source.text).maps[level]);
      });
   }
}
