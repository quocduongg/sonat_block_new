using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable Unity.InefficientPropertyAccess

namespace BlockPuzzle
{
    public enum SortOrderEnum
    {
        OnBoard = 0,
        OnSpawn = 1,
        OnSelect = 2,
        OnTop = 3,
    }

    public class BlockItemView : CurrentGameView
    {
        
        public BlockItem Data { get; private set; }
        public Transform RootTransform => rootTile.transform;
        [SerializeField] private BaseRxAnimator[] onSpawnAnimator;
        private List<BlockTileView> Tiles { get; set; }

        [SerializeField] private Transform rotateTransform;

        [SerializeField] private Transform
            tileContainer;

        [SerializeField] private BoolReactiveProperty possibleToPlace;
        [DisplayAsDisable] public BlockTileView rootTile;
        [SerializeField] private ToggleScript[] rotateBindings;
        [SerializeField] private ToggleScript[] onSelects;
        public ToggleScriptSelectItemOld blockTweenSelectScript;


        private bool _active;

        public void SetActive(bool value)
        {
            _active = value;
            gameObject.SetActive(value);
            rotateBindings.OnChanged(_active && _data != null && _data.canRotate);
            if (!value)
                dragTransform.localPosition = Vector3.zero;
        }

        public void ClearRotate()
        {
            rotateBindings.OnChanged(false);
        }

        public void ClearRotateForTut()
        {
            Data.canRotate = false;
        }

        public void SetPossibleToPlace(bool value)
        {
            possibleToPlace.Value = value;
            if (Tiles == null) return;
            foreach (var blockTileView in Tiles)
                blockTileView.SetEnableBinding(value);
        }

        protected override void Register()
        {
            base.Register();
            possibleToPlace.Subscribe(data =>
            {
                if (Tiles == null) return;
                foreach (var blockTileView in Tiles)
                    blockTileView.SetEnableBinding(data);
            });
            Active = false;
        }

        private BlockItem _data;
        public bool CanRotate() => _data.canRotate;

        public void Setup(BlockItem setupData)
        {
            CheckRegister();
            // custom for instant move
            blockTweenSelectScript.OnChanged(false, true);
            SetSortOrder(SortOrderEnum.OnSpawn);
            if (Tiles != null)
                OnSelect(false, true);
            BusyMoving = false;
            onSelects.OnChanged(false);
            _data = setupData;
            _data.rotate = 0;
            rotateTransform.rotation = Quaternion.identity;
            foreach (var anim in onSpawnAnimator)
                anim.Animator_In();
            ClearTiles();
            Active = setupData != null;
            Data = setupData;
            Tiles = new List<BlockTileView>();
            var rootPos = Data.rootPoint.GetGamePos();
            rootTile = Create(Point.Zero);
            foreach (var relativePoint in Data.relativePoints)
            {
                Create(relativePoint);
#if UNITY_EDITOR
                if (relativePoint == Point.Zero)
                    Debug.LogError("relative cannot be zero");
#endif
            }

            var center = Tiles.Select(x => x.transform.localPosition).ToArray().CenterOfVectorsMaxSize();
            tileContainer.localPosition = -center;
            rotateTransform.localEulerAngles = GetRotate(_data.rotate);
            BusyRotate = false;

            for (int i = 0; i < Data.stars.Length; i++)
                Tiles[Data.stars[i]].ActiveStar();

            rotateBindings.OnChanged(_active && _data != null && _data.canRotate);

            Debug.Log(_data.canRotate);

            BlockTileView Create(Point relativePoint)
            {
                var boardPoint = Data.rootPoint.GetRelativeToWorld(relativePoint, PointType.Square);
                var tile = GameController.tiles.Rent(0);
                tile.sortOrder.Value = 0;
                tile.SetParentBlock(this, Data.color);
                tile.transform.SetParent(tileContainer);
                tile.SetSprite(true);
                Transform transform1;
                (transform1 = tile.transform).localPosition = boardPoint.GetGamePos() - rootPos;
                transform1.localScale = Vector3.one;
                Tiles.Add(tile);
                return tile;
            }
        }
        


        private SortOrderEnum _sortOrder;
        /// <summary>
        /// for stupid layering
        /// </summary>
        /// <param name="sortOrder"></param>
        public void SetSortOrder(SortOrderEnum sortOrder)
        {
            _sortOrder = sortOrder;
            if (GameController.BoardSpecialState == BoardSpecialState.FirstHintDragToSave)
                _sortOrder = SortOrderEnum.OnTop;
            if (Tiles != null)
                foreach (var blockTileView in Tiles)
                    blockTileView.sortOrder.Value =  (int)_sortOrder;
        }

        public void ReleaseTile(IList<PointParameter> placePoints, Transform releaseTo, BlockTileView[,] map)
        {
            SetSortOrder(SortOrderEnum.OnBoard);
            for (var i = 0; i < Tiles.Count; i++)
            {
                Tiles[i].transform.SetParent(releaseTo);
                Tiles[i].transform.localScale = Vector3.one;
                Tiles[i].transform.rotation = Quaternion.identity;
                Tiles[i].SetPos(placePoints[i], true);
                Tiles[i].onDrag.Animator_In();
                Tiles[i].ClearParent();
                Tiles[i].SetSprite(false);
                map.SetAtPoint(placePoints[i], Tiles[i]);
            }

            Tiles.Clear();
            ClearTiles();
        }

        public void ClearTiles()
        {
            if (Tiles != null)
            {
                foreach (var tile in Tiles)
                    tile.Return();
                Tiles.Clear();
            }

            Data = null;
            Active = false;
            BusyRotate = false;
        }

        public override void Return()
        {
            base.Return();
            ClearTiles();
        }

        private Transform _parent;

        public void SetSpawnParent(Transform parent)
        {
            _parent = parent;
            Transform transform1;
            (transform1 = transform).SetParent(_parent);
            transform1.localPosition = Vector3.zero;
        }


        public IObservable<Unit> ReturnToRightPlace(float returnSpeed)
        {
            return Observable.FromCoroutine(ReturnToSpawnCoroutine);
        }

        public Transform dragTransform;
        public bool BusyMoving { get; set; }

        IEnumerator ReturnToSpawnCoroutine()
        {
            BusyMoving = true;
//            (transform1 = transform).SetParent(_parent);
            var distance = dragTransform.localPosition.Magnitude2();
            if (distance > 0.1f)
            {
                blockTweenSelectScript.transform.localPosition += dragTransform.localPosition;
                blockTweenSelectScript.SetAddLocalPos(dragTransform.localPosition);
                dragTransform.localPosition = Vector3.zero;
                blockTweenSelectScript.OnChanged(false, false);
                foreach (var toggleScript in onSelects)
                    toggleScript.OnChanged(false, false);
                yield return null;
                while (blockTweenSelectScript.busy)
                    yield return null;
                
                SetSortOrder(SortOrderEnum.OnSpawn);
                OnSelect(false, false);
            }
            else
                Appear();

            BusyMoving = false;
        }

        public void Appear()
        {
            ResetToSpawn();
            SetSortOrder(SortOrderEnum.OnSpawn);
            OnSelect(false, false);
            blockTweenSelectScript.OnChanged(false, false);
            foreach (var toggleScript in onSelects)
                if (toggleScript != blockTweenSelectScript)
                    toggleScript.OnChanged(false);
        }

        public void ResetToSpawn()
        {
            dragTransform.localPosition = Vector3.zero;
        }

        public void OnSelect(bool value, bool instant)
        {
            foreach (var toggleScript in onSelects)
                toggleScript.OnChanged(value, instant);
            SetSortOrder(_sortOrder);
            if (value)
                rotateBindings.OnChanged(false);
            else
                rotateBindings.OnChanged(_active && _data != null && _data.canRotate);

            if (!instant && Tiles != null)
            {
                if (value)
                {
                    foreach (var blockTileView in Tiles)
                        blockTileView.onDrag.Animator_Out();
                }
                else
                {
                    foreach (var blockTileView in Tiles)
                        blockTileView.onDrag.Animator_In();
                }
            }
            else
            {
                if (value)
                {
                    foreach (var blockTileView in Tiles)
                        blockTileView.onDrag.ResetAnimation();
                }
                else
                {
                    foreach (var blockTileView in Tiles)
                        blockTileView.onDrag.SetAtAnimationIn();
                }
            }
        }

        public BlockItem GetSave()
        {
            if (gameObject.activeSelf)
                return Data;
            return null;
        }

        private IDisposable _rotateDisposable;

        public bool AddRotate()
        {
            if (BusyRotate || !Data.canRotate)
                return false;
            _data.rotate++;
            _data.rotate = _data.rotate % 4;
            _rotateDisposable?.Dispose();
            _rotateDisposable = Observable.FromCoroutine(IeRotate).Subscribe();
            ((int) SoundEnum.BlockRotate).PlaySound();
            return true;
        }

        public bool BusyRotate { get; private set; }

        public IEnumerator IeRotate()
        {
            BusyRotate = true;
            foreach (var blockTileView in Tiles)
            {
                blockTileView.DeactiveStar();
            }

            yield return rotateTransform
                .DOLocalRotate(GetRotate(_data.rotate), CurrentGameSetting.timeRotate)
                .SetEase(Ease.OutSine).WaitForCompletion();
            foreach (var blockTileView in Tiles)
            {
                blockTileView.ReActiveStar();
            }

            BusyRotate = false;
            GameController.CheckPossibleToPlace();
        }

        private static Vector3 GetRotate(int nRotate1)
        {
            return new Vector3(0f, 0f, nRotate1 % 4 * -90f);
        }

        public void ReleaseRotate()
        {
            if (_data.rotate % 4 != 0)
            {
                _data.rotate = 0;
                _rotateDisposable?.Dispose();
                _rotateDisposable = Observable.FromCoroutine(IeRotate).Subscribe();
            }
        }

        public void SetIsSavedItem(bool b)
        {
            blockTweenSelectScript.SetIndex(b ? 1 : 0);
        }
    }
}