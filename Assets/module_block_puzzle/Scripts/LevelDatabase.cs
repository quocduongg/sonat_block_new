using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace BlockPuzzle
{
    [CreateAssetMenu(fileName = "LevelDatabase.asset", menuName = "Duong/BlockPuzzle/" + nameof(LevelDatabase))]
    public class LevelDatabase : BaseLevelDatabase<Level>
    {
    }

    [System.Serializable]
    public class Level : BaseLevel
    {
        public Point size;
        public List<PointParameter> blocks;

        public Level()
        {
        }

        public Level(MapListPoint mapListPoint)
        {
            blocks = mapListPoint.map.points.Select(x => x.CloneCurrent()).ToList();
        }

        public override BaseLevel Clone()
        {
            return new Level()
            {
                size = size.Clone(),
                blocks = blocks.Select(x => x.CloneCurrent()).ToList()
            };
        }
    }

    [System.Serializable]
    public class BoardState : BaseBoardState
    {
        public int score;
        public PointParameter[] points;
        public BlockItem[] spawns;

        public override BaseBoardState Clone()
        {
            return new BoardState()
            {
                score = score,
                points = points.Select(x => x.CloneCurrent()).ToArray(),
                spawns = spawns.Select(x => x.Clone()).ToArray(),
            };
        }

        public int Score => score;
    }

    [System.Serializable]
    public class JigsawCollection
    {
        public List<JigsawLevel> levels = new List<JigsawLevel>();
    }

    [System.Serializable]
    public class JigsawLevel
    {
        public Vector3Int size;
        public string fileBg;
        public string file;
//        public List<RelativePoints> parts = new List<RelativePoints>();
        public List<ListPoint> parts2 = new List<ListPoint>();

        public void ConvertData()
        {
//            parts2 = new List<ListPoint>();
//            for (var i = 0; i < parts.Count; i++)
//            {
//                parts2.Add(new ListPoint());
//                parts2[i].points = parts[i].relatives.Select(x => x.Clone2()).ToList();
//                parts2[i].customParameter = new CustomParameter()
//                {
//                    intValue = parts[i].root.col,
//                    intValue2 = parts[i].root.row,
//                    intValue3 = parts[i].index,
//                };
//                parts2[i].points.Insert(0, parts[i].root);
//            }
        }

        public JigsawLevel Clone()
        {
            return new JigsawLevel()
            {
                size = size,
                file = file,
                fileBg = fileBg,
//                parts = parts.Select(x => x.Clone()).ToList(),
                parts2 = parts2.Select(x => x.Clone()).ToList(),
            };
        }

        public void TryAdd(Point point, int i, bool shift, bool ctrl)
        {
            if (Exist(point) != null)
            {
                var exist = Exist(point);
                exist.value = Convert.ToInt32(shift) << 0 |
                              Convert.ToInt32(ctrl) << 1;
                return;
            }

            var part = parts2.Find(x => x.customParameter.intValue3 == i);
            if (part == null)
            {
                parts2.Add(new ListPoint()
                {
                    customParameter = new CustomParameter()
                    {
                        intValue3 = i
                    },
                    points = new List<PointParameter>()
                    {
                        new PointParameter(point.col, point.row, Convert.ToInt32(shift) << 0 |
                                                                    Convert.ToInt32(ctrl) << 1, 0)
                    }
                });
            }
            else
            {
                part.points.Add(new PointParameter(point.col, point.row, Convert.ToInt32(shift) << 0 |
                                                                            Convert.ToInt32(ctrl) << 1, 0));
            }
        }

        public void Alter(Point point)
        {
            var find = Exist(point);
            if (find != null)
                find.value = find.value == 0 ? 1 : 0;
        }

        private PointParameter Exist(Point point)
        {
            foreach (var part in parts2)
            {
                for (var i = 0; i < part.Count; i++)
                {
                    if (part.points[i] == point)
                        return part.points[i];
                }
            }

            return null;
        }

        public void TryRemove(Point point, int i)
        {
            for (var index = 0; index < parts2.Count; index++)
            {
                var part = parts2[index];
                for (var i1 = 0; i1 < part.Count; i1++)
                {
                    if (part.points[i1] == point)
                    {
                        if (i1 == 0)
                        {
                            parts2.RemoveAt(index);
                            return;
                        }
                        else
                        {
                            part.points.RemoveAt(i1);
                        }
                    }
                }
            }
        }
    }
}