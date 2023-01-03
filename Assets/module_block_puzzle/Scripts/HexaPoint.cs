//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using System.Security.Permissions;
//
//[System.Serializable]
//public enum HexaType
//{
//    Horizonal = 0,
//    Vertical = 1
//}
//
//public partial class Point
//{
//    public static HexaType HexaType = HexaType.Vertical;
//    public static Vector2 Gap;
//    public static float HexaRate = 0.86604f;
//
//
//    public static HexaPoint GetPointByRelative(Point rootPoint, Point relativeDistance)
//    {
//        if (relativeDistance == Zero)
//            return rootPoint.CloneHexa();
//
//        if (HexaType == HexaType.Vertical)
//        {
//
//            if (relativeDistance.Col == 0)
//                return new HexaPoint(rootPoint.Col, rootPoint.Row + relativeDistance.Row);
//
//            if (rootPoint.Col % 2 == 0) // even column
//            {
//                var col = rootPoint.Col + relativeDistance.Col;
//                var row = rootPoint.Row + relativeDistance.Row;
//                if (col % 2 == 1)
//                    if (relativeDistance.Row > 0)
//                    row--;
//
//                return new HexaPoint(col, row);
//            }
//            else // odd column
//            {
//                var col = rootPoint.Col + relativeDistance.Col;
//                var row = rootPoint.Row + relativeDistance.Row;
//                if (col % 2 == 0)
//                    if (relativeDistance.Row < 0)
//                    row++;
//
//                return new HexaPoint(col, row);
//            }
//        }
//
//        Debug.LogError("GetRelativePoint for " + HexaType + " hasn't been defined yet");
//        return null;
//    }
//
//    public static HexaPoint GetRelativePoint(Point rootPoint, Point secondPoint)
//    {
//        if (HexaType == HexaType.Vertical)
//        {
//            if (rootPoint.Col % 2 == 0) // even column
//            {
//                var col = secondPoint.Col - rootPoint.Col;
//                var row = secondPoint.Row - rootPoint.Row;
//                if(col%2 == 1)
//                    if (row >= 0)
//                        row++;
//
//                return new HexaPoint(col, row);
//            }
//            else // odd column
//            {
//                var col = secondPoint.Col - rootPoint.Col;
//                var row = secondPoint.Row - rootPoint.Row;
//                if (col % 2 == 1)
//                    if (row <= 0)
//                    row--;
//
//                return new HexaPoint(col, row);
//            }
//        }
//
//        Debug.LogError("GetRelativePoint for " + HexaType + " hasn't been defined yet");
//        return null;
//    }
//
//    public static HexaPoint GetPoint(Vector3 position)
//    {
//        Vector2 positionRoot = new Vector2();
//        positionRoot.x = (float)-Point.TotalCols / 2 * Point.PointSize.x + Point.PointSize.x / 2;
//        positionRoot.y = (float)-Point.TotalRows / 2 * Point.PointSize.y + Point.PointSize.y / 2;
//
//        return new HexaPoint()
//        {
//            Col = 0,
//            Row = Mathf.FloorToInt((position.y - positionRoot.y) / Point.PointSize.y),
//        };
//    }
//
//    public static bool[,] CreateIsoscelesHexangonMap(int unit)
//    {
//        bool[,] map = new bool[unit * 2 - 1, unit * 2 - 1];
//        HexaPoint CenterPoint = new HexaPoint(unit - 1, unit - 1);
//        var points = new List<HexaPoint>();
//        points.Add(CenterPoint);
//        var adjacents = CenterPoint.Adjacents(map);
//
//        int count = 0;
//        do
//        {
//            points.AddRange(adjacents);
//            var newAdjacents = new List<HexaPoint>();
//            foreach (var point in adjacents)
//            {
//                var adjacentsOfPoint = point.Adjacents(map);
//                foreach (var newAdjacent in adjacentsOfPoint)
//                {
//                    if (points.Find(x => x.Col == newAdjacent.Col && x.Row == newAdjacent.Row) == null && newAdjacents.Find(x => x.Col == newAdjacent.Col && x.Row == newAdjacent.Row) == null)
//                        newAdjacents.Add(newAdjacent);
//                }
//            }
//            adjacents = newAdjacents;
//            count++;
//        }
//        while (adjacents.Count > 0 && count < unit - 1);
//
//        foreach (var hexaPoint in points)
//        {
//            map[hexaPoint.Col, hexaPoint.Row] = true;
//        }
//
//        return map;
//    }
//
//
//
//    public new List<HexaPoint> Adjacents<T>(T[,] map)
//    {
//        if (Col < 0 || Row < 0 || Col >= map.GetLength(0) || Row >= map.GetLength(1))
//            return null;
//
//        return GetAdjacents(map.GetLength(0), map.GetLength(1));
//    }
//
//    public List<HexaPoint> GetAdjacents(int totalCol, int totalRow)
//    {
//        if (HexaType == HexaType.Horizonal)
//        {
//            if (Row % 2 == 0)
//            {
//                var left = new HexaPoint(Col - 1, Row);
//                var right = new HexaPoint(Col + 1, Row);
//                var topLeft = new HexaPoint(Col - 1, Row + 1);
//                var topRight = new HexaPoint(Col, Row + 1);
//                var bottomLeft = new HexaPoint(Col - 1, Row - 1);
//                var bottomRight = new HexaPoint(Col, Row - 1);
//                var list = new List<HexaPoint>
//            {
//                left,
//                right,
//                topLeft,
//                topRight,
//                bottomLeft,
//                bottomRight
//            };
//                list.RemoveAll(x => x.Col < 0 || x.Col > totalCol - 1 || x.Row < 0 || x.Row > totalRow - 1);
//                return list;
//            }
//            else
//            {
//                var left = new HexaPoint(Col - 1, Row);
//                var right = new HexaPoint(Col + 1, Row);
//                var topLeft = new HexaPoint(Col, Row + 1);
//                var topRight = new HexaPoint(Col + 1, Row + 1);
//                var bottomLeft = new HexaPoint(Col, Row - 1);
//                var bottomRight = new HexaPoint(Col + 1, Row - 1);
//                var list = new List<HexaPoint>
//            {
//                left,
//                right,
//                topLeft,
//                topRight,
//                bottomLeft,
//                bottomRight
//            };
//                list.RemoveAll(x => x.Col < 0 || x.Col > totalCol - 1 || x.Row < 0 || x.Row > totalRow - 1);
//                return list;
//            }
//        }
//        else
//        {
//            if (Col % 2 == 0)
//            {
//                var bottom = new HexaPoint(Col, Row-1);
//                var top = new HexaPoint(Col, Row+1);
//                var bottomLeft = new HexaPoint(Col - 1, Row - 1);
//                var bottomRight = new HexaPoint(Col +1, Row - 1);
//                var topLeft = new HexaPoint(Col - 1, Row);
//                var topRight = new HexaPoint(Col +1, Row);
//                var list = new List<HexaPoint>
//            {
//                bottom,
//                top,
//                topLeft,
//                topRight,
//                bottomLeft,
//                bottomRight
//            };
//                list.RemoveAll(x => x.Col < 0 || x.Col > totalCol - 1 || x.Row < 0 || x.Row > totalRow - 1);
//                return list;
//            }
//            else
//            {
//                var bottom = new HexaPoint(Col, Row - 1);
//                var top = new HexaPoint(Col, Row + 1);
//                var bottomLeft = new HexaPoint(Col - 1, Row );
//                var bottomRight = new HexaPoint(Col + 1, Row);
//                var topLeft = new HexaPoint(Col - 1, Row+1);
//                var topRight = new HexaPoint(Col + 1, Row+1);
//                var list = new List<HexaPoint>
//            {
//                bottom,
//                top,
//                topLeft,
//                topRight,
//                bottomLeft,
//                bottomRight
//            };
//                list.RemoveAll(x => x.Col < 0 || x.Col > totalCol - 1 || x.Row < 0 || x.Row > totalRow - 1);
//                return list;
//            }
//        }
//    }
//
//    public HexaPoint()
//    {
//
//    }
//
//    public HexaPoint(int col, int row) : base(col, row)
//    {
//        
//    }
//
//    public Vector3 GetPosHexa(PointAlign align = PointAlign.Center)
//    {
//        float hexaPointSize = 0.86604f * PointSize.y;
//
//        if (HexaType == HexaType.Horizonal)
//        {
//            var totalCols = 0; // ~ BottomLeft
//            var totalRows = 0; // ~ BottomLeft
//            if (align == PointAlign.Center)
//            {
//                totalCols = TotalCols;
//                totalRows = TotalRows;
//            }
//
//            Vector3 localPos = Vector3.zero;
//            localPos.x = ((float)-totalCols / 2 + this.Col) * PointSize.x + PointSize.x / 2;
//            localPos.y = ((float)-totalRows / 2 + this.Row) * hexaPointSize + hexaPointSize / 2;
//
//            if (Row % 2 == 0)
//                localPos.x += Mathf.CeilToInt(Col - totalCols * 1f / 2) * Gap.x;
//            else
//                localPos.x += Mathf.CeilToInt(Col - totalCols * 1f / 2) * Gap.x + PointSize.x / 2;
//
//            if (align == PointAlign.Center)
//                if ((totalCols + 1) / 2 % 2 == 0)
//                    localPos.x -= PointSize.x / 2;
//
//            localPos.y += Mathf.CeilToInt(Row - totalRows * 1f / 2) * Gap.y;
//            return localPos;
//        }
//
//        if (HexaType == HexaType.Vertical)
//        {
//            var totalCols = 0; // ~ BottomLeft
//            var totalRows = 0; // ~ BottomLeft
//            if (align == PointAlign.Center)
//            {
//                totalCols = TotalCols;
//                totalRows = TotalRows;
//            }
//
//            Vector3 localPos = Vector3.zero;
//            localPos.x = ((float)-totalCols / 2 + this.Col) * hexaPointSize + hexaPointSize / 2;
//            localPos.y = ((float)-totalRows / 2 + this.Row + (0.86604f * 0.86604f)) * PointSize.y - PointSize.y / 2;
//
//            if (Col % 2 == 0)
//                localPos.y += Mathf.CeilToInt(Row - totalCols * 1f / 2) * Gap.y;
//            else
//                localPos.y += Mathf.CeilToInt(Row - totalCols * 1f / 2) * Gap.y + PointSize.y / 2 + Gap.y/2;
//
//
//            localPos.x += Mathf.CeilToInt(Col - totalCols * 1f / 2) * Gap.x;
//            return localPos;
//        }
//
//
//        Debug.LogError(HexaType + " are not available");
//        return Vector3.zero;
//    }
//}
