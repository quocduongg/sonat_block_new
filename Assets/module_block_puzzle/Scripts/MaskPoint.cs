using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BlockPuzzle
{
    public class MaskPoint : MonoBehaviour
    {
        [SerializeField] private float size = 100;
        [SerializeField] private Point point;
        [SerializeField] private Point mapSize;

        public Point Point => point;
        public Point MapSize => mapSize;
        
        
        [MyButton(nameof(Set))] public int abc123;

        public void SetData(Point p,Point map)
        {
            point = p;
            mapSize = map;
        }

        public void SetPosAndSize(Vector3 pos, Vector3 scale)
        {
            transform.position = pos;
            transform.localScale = scale;
        }
        
        public void Set()
        {
            var position = point.GetGamePos() - new Vector3(Point.PointSize / 2, Point.PointSize / 2, 0);
            var transform1 = transform;
            transform1.localScale = new Vector3(100 / size * mapSize.col * MyGameSetting.PointSetup.GetDistance().x,
                100 / size * mapSize.row * MyGameSetting.PointSetup.GetDistance().y);
            transform1.position = position;
        }
    }
}