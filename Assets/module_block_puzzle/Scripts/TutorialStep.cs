using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockPuzzle
{
    
    [CreateAssetMenu(fileName = "TutorialStep.asset", menuName = "BlockPuzzle/TutorialStep")]
    public class TutorialStep : BaseTutorialStep
    {
        [ListPointName(
            new int []{10}, 
            new []{nameof(CustomParameter.boolValue),nameof(CustomParameter.boolValue),nameof(CustomParameter.intValue3),nameof(CustomParameter.boolValue)}, 
            new []{"InitMap"}, 
            new []{40,30} ,ListPointType.ColorMap,5,5)]
        public ListPoint map; // spawn map on board
        
        [ListPointName(
            new []{10,10}, 
            new []{nameof(CustomParameter.intValue),nameof(CustomParameter.boolValue),nameof(CustomParameter.intValue3),nameof(CustomParameter.boolValue)}, 
            new []{"Color","Yes"}, 
            new []{40,30} ,ListPointType.Absolute,5,5)]
        public ListPoint hintPlacePoints; // first point is place point
        
        [ListPointName(
            new []{10,5,5}, 
            new []{nameof(CustomParameter.intValue),nameof(CustomParameter.boolValue),nameof(CustomParameter.boolValue2),nameof(CustomParameter.boolValue)}, 
            new []{"Color","Spawn","Drag"}, 
            new []{40,40,40} ,ListPointType.Relative,3,3)]
        [SerializeField] private ListPoint item;
        [ListPointName(
            new []{10,5,5}, 
            new []{nameof(CustomParameter.intValue),nameof(CustomParameter.boolValue),nameof(CustomParameter.boolValue2),nameof(CustomParameter.boolValue)}, 
            new []{"Color","Spawn","Drag"}, 
            new []{40,40,40} ,ListPointType.Relative,3,3)]
        [SerializeField] private ListPoint item2;
        [ListPointName(
            new []{10,5,5}, 
            new []{nameof(CustomParameter.intValue),nameof(CustomParameter.boolValue),nameof(CustomParameter.boolValue2),nameof(CustomParameter.boolValue)}, 
            new []{"Color","Spawn","Drag"}, 
            new []{40,40,40} ,ListPointType.Relative,3,3)]
        [SerializeField] private ListPoint item3; //spawn item
        
        public ListPoint[] Items
        {
            get
            {
                return new[]
                {
                    item.customParameter.boolValue ? item : null,
                    item2.customParameter.boolValue ? item2 : null, 
                    item3.customParameter.boolValue ? item3 : null,
                };
            }
        }

       
        [EditorDisplayName(
            new []{15,10,15,10}, 
            new []{nameof(PointPoint.col1),nameof(PointPoint.row1),nameof(PointPoint.col2),nameof(PointPoint.row2)}, 
            new []{"Pos","","Size",""}, 
            new []{30,0,30,0} )]
        public PointPoint hintMask; // to mask Area on screen, my point 

        public Point PlacePoint => hintPlacePoints.points[0]; // where to place

        public string messages;
        public string doneMessage = "Well done!";

        public bool Spawn
        {
            get
            {
                foreach (var listPoint in Items)
                    if (listPoint != null && listPoint.customParameter.boolValue)
                        return true;

                return false;
            }
        }

        public int DragItemIndex
        {
            get
            {
                if (item.customParameter.boolValue2)
                    return 0;
                if (item2.customParameter.boolValue2)
                    return 1;
                if (item3.customParameter.boolValue2)
                    return 2;
                if ((TutorialActionEnum) actionEnum == TutorialActionEnum.DragBlock && actionParam == (int)TutorialActionParam.FromSaveSlot)
                    return 3;
                return -1;
            }
        }

        public int sortOrderIndex;
        public bool turnOffRayCast = true;
    }
}