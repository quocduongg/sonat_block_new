using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(fileName = "BlockPuzzleSettingData.asset", menuName = "MyScriptableObject/BlockPuzzleSettingData")]
public class BlockPuzzleSettingData : ScriptableObject
{
    public ArrayBool[] spawnItems;
    public int spawnColorRange;
}
