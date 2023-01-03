using UnityEngine;

[System.Serializable]
public struct PattemInfor
{
    [SerializeField]
    public Types type;
    public bool[] grid;
}

public enum Level
{
    Level_1,
    Level_2,
    Level_3,
    Level_4,
    Level_5,
    Level_6,
    Level_7,
    Level_8,
    Level_9,
    Level_10,
    Level_11,
    Level_12,
    Level_13,
    Level_14,
    Level_15,
    Level_16,
}

public enum Types
{
    O0,
    O1,
    O2,
    I0,
    I1,
    I2,
    I3,
    L0,
    L1,
    T0,
    T1,
    LB0,
    Z0,

    U0,
    Plus,
    W,

    New1,
    New2,
    New3,
    New4,
    New5,
    New6,
    New7,
    New8,
    New9,
    New10,
    New11,
    New12,
    New13,
    New14,
    New15,
    New16,
}


[System.Serializable]
public struct LevelData
{
    public Level level;
    public int Score;
    public int[] weight;
}

