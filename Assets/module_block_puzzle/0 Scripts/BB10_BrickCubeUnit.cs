using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BB10_BrickCubeUnit : MonoBehaviour
{
    public Types thisType;

    public SpriteRenderer render;

    public int ID;
    public int row;
    public int col;

    public int indexRow;
    public int indexCol;

    public float scale;

//    public BB10_ColorData myData;
    public float targetGray;


    public float durationDrop;
    public Vector3 dropScaleMin;

    public AnimationCurve ac;
    public float speed;

}
