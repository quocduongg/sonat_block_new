using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle
{
    public class PointSetup
    {
        private readonly PointType _pointType;
        private readonly float _distanceX;
        private readonly float _distanceY;
        private readonly Vector3 _centered;

        public PointType PointType => _pointType;

        public PointSetup(PointType pointType, int totalCol, int totalRow, float distanceX, float distanceY)
        {
            _pointType = pointType;
            _distanceX = distanceX;
            _distanceY = distanceY;
            _centered = PointGetPosition.GetBoardCenteredPosition(pointType, totalCol, totalRow, distanceX, distanceY);
        }

        public Vector3 GetPos(Point point)
        {
            return point.GetPos(_distanceX, _distanceY) + _centered;
        }
        
        public Vector2 GetDistance()
        {
           return new Vector2(_distanceX,_distanceY);
        }
        
        public int GetCol(Vector2 origin)
        {
            return Mathf.FloorToInt((origin.x - _centered.x + _distanceX / 2) / _distanceX);
        }
    }

    public static class MyGameSetting
    {
        private static PointSetup _pointSetup;

//        public static PointSetup PointSetup => _pointSetup;

        public static void Setup(PointType pointType, int totalCol, int totalRow, float distanceX, float distanceY)
        {
            _pointSetup = new PointSetup(pointType, totalCol, totalRow, distanceX, distanceY);
        }

        public static Vector3 GetGamePos(this Point point) => _pointSetup.GetPos(point);
        public static Vector3 GetGameScale(this Point point) => _pointSetup.GetPos(point);

        public static PointSetup PointSetup => _pointSetup;
    }

    
}