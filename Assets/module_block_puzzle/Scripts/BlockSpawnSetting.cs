using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BlockPuzzle
{
    [Serializable]
    [CreateAssetMenu(fileName = "BlockSpawnSetting", menuName = "Create BlockSpawnSetting")]
    public class BlockSpawnSetting : ScriptableObject
    {
        public LevelData[] levelData;
        [ListPointName(
            new int []{10}, 
            new []{nameof(CustomParameter.boolValue),nameof(CustomParameter.boolValue),nameof(CustomParameter.intValue3),nameof(CustomParameter.boolValue)}, 
            new []{"Rotate"}, 
            new []{40,30} ,ListPointType.Relative,3,3)]
        public ListPoint[] map;

        public ListPoint GetRandomDes(int currentScoreValue)
        {
            var r = GetFixedRandomType(currentScoreValue);
            var m = map[r];
            if (m.customParameter.boolValue)
                return m.Rotate(Random.Range(0, 4));
            return m;
        }

        private int GetFixedRandomType(int score)
        {
            for (int i = 0; i < levelData.Length; i++)
            {
                if (score < levelData[i].Score)
                {
                    return RandomWeight(levelData[i].weight);
                }
            }

            return RandomWeight(levelData[levelData.Length - 1].weight);
        }

        private static int RandomWeight(IReadOnlyList<int> list)
        {
            int totalWeight = 0;
            for (int i = 0; i < list.Count; i++)
            {
                totalWeight += list[i];
            }

            int choice = Random.Range(0, totalWeight);
            int sum = 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] + sum >= choice)
                {
                    return i;
                }

                sum += list[i];
            }

            return 0;
        }
    }
}