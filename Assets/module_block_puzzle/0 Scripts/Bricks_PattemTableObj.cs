using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

//using System;

[CreateAssetMenu(fileName = "PattemData", menuName = "Create Pattem Data")]
public class Bricks_PattemTableObj : ScriptableObject
{
    public PattemInfor[] listPattemsInfor;
    [MyButton(nameof(ConvertMap))] [SerializeField]
    private int copy;
    public LevelData[] levelData;
    public ListPoint[] map;
    
    public void ConvertMap()
    {
        var root = new Point(2, 2);
        map = new ListPoint[listPattemsInfor.Length];
        for (var index = 0; index < listPattemsInfor.Length; index++)
        {
            map[index] = new ListPoint();
            var pattemInfor = listPattemsInfor[index];
            List<Point> list = new List<Point>();

            for (var i = 0; i < pattemInfor.grid.Length; i++)
            {
                if (pattemInfor.grid[i])
                {
                    var point = i.ToPoint(5);
                    if (point - root != Point.Zero)
                        list.Add(point - root);
                    Debug.Log((point - root).toString());
                }
            }

            map[index].points = list.Select(x => x.ToPointWithTwoValue(0, 0)).ToList();
        }
    }


    public static int count = 0;

    bool CheckException()
    {
        count++;
        //Debug.Log("GetFixedRandomType: " + count);

        // Comment for test
//        if(!FireBaseController.is_get_pattern_exception_value)
//        {
//            return false;
//        }
//
//        if(count < (BB10_FBController.number_to_create_pattern_exception_value + 1))
//        {
//            return false;
//        }
//        else if(count < ((BB10_FBController.number_to_create_pattern_exception_value + 1 + BB10_FBController.number_pattern_exception_value)))
//        {
//            //Debug.Log("MainCanvas.timePlay: " + MainCanvas.timePlay);
//            if(/*true || */((BB10_Settings.GetTimePlay > 1) && ((BB10_MainCanvasUI.timePlay % (int)(BB10_FBController.number_play_exception_value)) == 0)))
//            {
//                return true;
//            }
//            else
//            {
//                return false;
//            }
//        }
//        else
//        {
//            count = 1;
//            return false;
//        }

        return false;
    }

    Types GetTypeException()
    {
        //Debug.Log("GetTypeException");

        int rand = Random.Range(0, 6);

        switch (rand)
        {
            case 0:
            case 1:
                return Types.O2;
            case 2:
                return Types.I2;
            case 3:
            case 4:
                return Types.I3;
            case 5:
                return Types.L1;
            default:
                break;
        }

        return Types.O2;
    }


   
   
}