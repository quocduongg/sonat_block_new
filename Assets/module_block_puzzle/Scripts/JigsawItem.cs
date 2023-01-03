using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

// ReSharper disable Unity.InefficientPropertyAccess

namespace BlockPuzzle
{
    public class JigsawItem : CurrentGameView
    {
        

        private Pool<JigsawTile> _tilePool;
        [SerializeField] private BoolReactiveProperty onDrag = new BoolReactiveProperty();
        [SerializeField] private Collider2D hitCollider;

        public ToggleScriptSelectItemOld blockTweenSelectScript;


        public void SetPool(Pool<JigsawTile> tilesPool)
        {
            _tilePool = tilesPool;
        }

        public ListPoint Data { get; private set; }

//        public Transform RootTransform => rootTile.transform;

        public Transform RootTransform
        {
            get
            {
                if (rootTile == null)
                    Debug.LogError("not have root tile", gameObject);
                return rootTile.transform;
            }
        }

        [SerializeField] private BaseRxAnimator[] onSpawnAnimator;
        [SerializeField] private Transform centered;
        [SerializeField] private Transform tileContainer;
        [DisplayAsDisable] public JigsawTile rootTile;

        public int Step { get; set; }
        public int IndexInSpawn { get; set; }
        public Point PlacedPoint { get; set; }


        public List<JigsawTile> Tiles { get; private set; }

        protected override void OnKernelLoaded()
        {
            base.OnKernelLoaded();
            onDrag.Subscribe(data => { UpdateOnDrag(); });
        }

        private void UpdateOnDrag()
        {
            if (Tiles != null)
                for (var i = 0; i < Tiles.Count; i++)
                    Tiles[i].SetOnDrag(onDrag.Value);
        }

        public void SetTile(Texture2D tex, Vector3Int sizeTex, ListPoint points)
        {
            Data = points;
            blockTweenSelectScript.OnChanged(false, true);

            var rootPos = Point.Zero.GetGamePos();
            Tiles = new List<JigsawTile>();

            List<Vector3> centeredFrom = new List<Vector3>();
            for (var i = 0; i < points.Count; i++)
            {
                var rent = Create(points.points[i]);
                rent.ParentBlock = this;
                rent.SetTile(tex, (Vector2Int) sizeTex, points.points[i], points.customParameter.intValue3);
                Tiles.Add(rent);
                if (i == 0)
                    rootTile = rent;
                if ((points.points[i].value & 2) != 0)
                    centeredFrom.Add(rent.transform.localPosition);
            }

            var center = (centeredFrom.Count == 0)
                ? Tiles.Select(x => x.transform.localPosition).ToArray().CenterOfVectorsMaxSize()
                : centeredFrom.CenterOfVectors();
            centered.localPosition = -center * blockTweenSelectScript.DataSet[0].offScale.x;
            onDrag.Value = false;

            JigsawTile Create(PointParameter relativePoint)
            {
                var tile = _tilePool.Rent(tileContainer, (relativePoint - points.points[0]).GetGamePos() - rootPos);
                tile.transform.localScale = Vector3.one;
                return tile;
            }
        }

        public void OnSelect(bool p0, bool p1)
        {
            onDrag.Value = p0;
        }

        public void SetSpawnPosition(Transform parent, float distanceX)
        {
            PlacedPoint = null;
            Transform transform1;
            (transform1 = transform).SetParent(parent);
            transform1.localPosition = new Vector3(distanceX * IndexInSpawn, 0);
            PlacedPoint = null;
            foreach (var jigsawTile in Tiles)
                jigsawTile.SetOnBoard(false);
            hitCollider.enabled = true;
            blockTweenSelectScript.OnChanged(false, true);
        }

        public void SetBoardPosition(int step)
        {
            transform.SetParent(null);
            onDrag.Value = false;
            Step = step;
            dragTransform.localPosition = Vector3.zero;
            blockTweenSelectScript.Release();

            transform.position = PlacedPoint.GetGamePos() + GameController.JigsawBoard.PositionAdjust -
                                 centered.localPosition;
            foreach (var jigsawTile in Tiles)
                jigsawTile.SetOnBoard(true);
            hitCollider.enabled = false;
        }


        // _____________________________________________________________

        public IObservable<Unit> ReturnToRightPlace(float returnSpeed)
        {
            return Observable.FromCoroutine(ReturnToSpawnCoroutine);
        }

        public Transform dragTransform;
        public bool BusyMoving { get; set; }

        IEnumerator ReturnToSpawnCoroutine()
        {
//            (transform1 = transform).SetParent(_parent);
            var distance = dragTransform.localPosition.Magnitude2();
            if (distance > 0.1f)
            {
                BusyMoving = true;
                blockTweenSelectScript.transform.localPosition += dragTransform.localPosition;
                blockTweenSelectScript.SetAddLocalPos(dragTransform.localPosition);
                dragTransform.localPosition = Vector3.zero;
                blockTweenSelectScript.OnChanged(false, false);
                yield return null;
                while (blockTweenSelectScript.busy)
                    yield return null;

                OnSelect(false, false);

                BusyMoving = false;
            }
            else
            {
                ResetToSpawn();
                OnSelect(false, false);
                blockTweenSelectScript.OnChanged(false, false);
                yield return null;
            }
        }

        public void ResetToSpawn()
        {
            dragTransform.localPosition = Vector3.zero;
        }
    }
}