using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;


// ReSharper disable Unity.InefficientPropertyAccess


namespace BlockPuzzle
{
    [Serializable]
    public class ArrayInt
    {
        public int[] values;

        public ArrayInt(int[] values)
        {
            this.values = values;
        }

        public ArrayInt(int value1, int value2, int value3, int value4)
        {
            values = new[] {value1, value2, value3, value4};
        }

        public ArrayInt Clone()
        {
            return new ArrayInt(values.ToArray());
        }

        public int Color => values[0];
        public int RootPoint => values[1];
        public int SpawnPoint => values[2];

        public int GetRandom()
        {
            if (values.Length == 1)
                return 0;

            float totalWeight = 0;
            float[] weightValues = new float[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                totalWeight += values[i];
                weightValues[i] = totalWeight;
            }

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);

            for (int i = 0; i < weightValues.Length; i++)
                if (randomValue < weightValues[i])
                    return i;

            return -1;
        }
    }

    [Serializable]
    public class ListInt
    {
        public List<int> values;

        public ListInt(IEnumerable<int> values)
        {
            this.values = values.ToList();
        }

        public ListInt(int color, int rootPoint, int spawnPoint)
        {
            values = new List<int>
            {
                color, rootPoint, spawnPoint
            };
        }

        public ListInt Clone()
        {
            return new ListInt(values.ToArray());
        }
    }

    [Serializable]
    public class ArrayBool
    {
        public bool[] values;
    }

    [Serializable]
    public class BlockItem
    {
        public int color;
        public Point rootPoint;
        public Point spawnPoint;
        public List<Point> relativePoints;
        public bool active;
        [NonSerialized]
        public int rotate;
        public bool canRotate = true;
        public int[] stars;

        public bool IsOneLine()
        {
            var horizontal = true;
            foreach (var relativePoint in relativePoints)
            {
                if (relativePoint.col != 0)
                {
                    horizontal = false;
                    break;
                }
            }
            var vertical = true;
            foreach (var relativePoint in relativePoints)
            {
                if (relativePoint.row != 0)
                {
                    vertical = false;
                    break;
                }
            }

            return horizontal || vertical;
        }


        public BlockItem(ListPoint listRelativePoint, int color, bool canRotate,IList<int> stars)
        {
            rootPoint = Point.Zero;
            relativePoints = listRelativePoint.points.Select(x => rootPoint.GetRelativeToWorld(x, PointType.Square))
                .ToList();
            this.color = color;
            this.active = true;
            this.canRotate = canRotate;
            if(stars != null)
                this.stars = stars.ToArray();
            else
                this.stars = new int[0];
        }

        public BlockItem()
        {
        }

        public BlockItem Clone()
        {
            return new BlockItem()
            {
                color = color,
                spawnPoint = spawnPoint.Clone(),
                rootPoint = rootPoint.Clone(),
                relativePoints = relativePoints.Select(x => x.Clone()).ToList(),
                active = active,
                rotate = rotate,
                canRotate = canRotate,
            };
        }

        public IEnumerable<Point> CalculatePlacedPoints(Point placePoint)
        {
            yield return placePoint.GetRelativeToWorld(Point.Zero, PointType.Square);

            if (rotate > 0)
            {
                var rotatePoints = relativePoints.Select(x => x.Clone());
                var enumerable = rotatePoints as Point[] ?? rotatePoints.ToArray();
                foreach (var rotatePoint in enumerable)
                    rotatePoint.Rotate90(rotate);

                foreach (var relativePoint in enumerable)
                    yield return placePoint.GetRelativeToWorld(relativePoint, PointType.Square);
            }
            else
            {
                foreach (var relativePoint in relativePoints)
                    yield return placePoint.GetRelativeToWorld(relativePoint, PointType.Square);
            }
        }
    }


    [Serializable]
    public class ListPointWithTwoValue
    {
        public List<PointParameter> points;

        public ListPointWithTwoValue Clone()
        {
            return new ListPointWithTwoValue()
            {
                points = points.Select(x => x.CloneCurrent()).ToList()
            };
        }
    }
}