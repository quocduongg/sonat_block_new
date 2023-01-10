using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


namespace BlockPuzzle
{
    

    [CreateAssetMenu(fileName = nameof(CurrentGameSetting) + ".asset",
        menuName = "Duong/BlockPuzzle/" + nameof(CurrentGameSetting))]
    public class CurrentGameSetting : ScriptableObject
    {
        public BlockSpawnSetting blockSpawnSetting;
        public float tileDistance = 1;
        public int totalColor = 5;
        public float delayDestroy = 0.1f;
        public float itemPlaceTween = 0.1f;
        public int timeToHint = 10;
        public float waitEating = 1;
        public int soundCount = 1;
        public float waitLoseClearScreen = 0.5f;
        public float waitBoardLose = 2;
        public int navigationTime = 10;
        public Vector2Int randomRange = new Vector2Int(10, 15);
        private JigsawCollection _jigsawCollection = null;
        public float timeRotate = 0.105f;
        public int[] starBoxProgress;
        public Product[] boxRewards;
        [SerializeField] private TextAsset jigsawCollection;
        public bool scoringInTutorial;
        public int timeHintJigsawMode = 10;


        public int LastStarCondition => starBoxProgress[starBoxProgress.Length - 1];
        public JigsawCollection JigsawCollection
        {
            get
            {
                //   if (_jigsawCollection == null)
                _jigsawCollection = JsonUtility.FromJson<JigsawCollection>(jigsawCollection.text);
                return _jigsawCollection;
            }
        }
    }
}