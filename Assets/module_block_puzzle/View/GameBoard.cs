using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sonat;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockPuzzle
{
    [Serializable]
    public struct BoardSetting
    {
        public FxSpawnPos complimentSpawnPos;
        public FxSpawnPos scoreSpawnPos;
    }

    public enum FxSpawnPos
    {
        AtEatPoint,
        Center,
    }

    public enum BoardSpecialState
    {
        None = 0,
        FirstHintDragToSave = 1,
    }

    public partial class GameController
    {
        [SerializeField] private JigsawBoard jigsawBoard;

        private BlockSpawnSetting _remoteSpawnSetting;

        public JigsawBoard JigsawBoard => jigsawBoard;

        [SerializeField] public PoolCollection<BlockTileView> bgTiles;
        [SerializeField] public PoolCollection<BlockTileView> tiles;
        [SerializeField] public PoolCollection<EffectView> blockEffects;
        [SerializeField] public PoolCollection<EffectView> scoreEffects;
        [SerializeField] public PoolCollection<EffectView> complimentEffects;
        [SerializeField] public PoolCollection<EffectView> eatLines1;
        [SerializeField] public PoolCollection<EffectView> eatLines2;
        [SerializeField] public BlockItemView[] spawnItems;
        [SerializeField] private Transform[] itemParents;
        [SerializeField] private GameTutorial gameTutorial;
        [SerializeField] private TweenDisplayScript starProgress;
        [SerializeField] private bool turnOnOnlyItemMatch;


        private IDisposable _navigationDisposable;
        private int _currentNavigation;
        private int _secondsFromLastAction;
        private int _secondsFromAdBreak;
        private int _secondsToAdBreak;

        public BoardSpecialState BoardSpecialState { get; set; }

        private ReactiveProperty<BlockItemView> _onlyItemMatch = new ReactiveProperty<BlockItemView>();

        [SerializeField]
        private IndexBindingScript[] hintDragToSaveSlot; // index because it contains display of first hint

        private List<PointWithVector3> Positions { get; set; }
        private PointWithVector3[,] _bgPoints;
        public PointWithVector3[,] BgPoints => _bgPoints;

        private BlockTileView[,] _viewMap;
        public BlockTileView[,] ViewMap => _viewMap;
        [SerializeField] private ToggleScriptSelectItem tweenSaveSlot;

        [NonSerialized] public IntReactiveProperty notEnoughSpace = new IntReactiveProperty();

        protected override IActiveTutorialComponents IActiveTutorialComponents => gameTutorial;
        protected override string NameSpace => "BlockPuzzle";


        [SerializeField] private BoardSetting boardSetting;
//        public BlockTileView[,] BgViewMap => _bgViewMap;
//        private BlockTileView[,] _bgViewMap;


        private enum BgTileType
        {
            MainBoard = 0,
            IdleHint = 1,
            PlaceHint = 2,
            OnlyMatchHint = 3,
            DecorTile = 4,
        }

        private Pool<BlockTileView> BgTilePoolPlaceHint => bgTiles.pools[(int) BgTileType.PlaceHint];
        public Pool<BlockTileView> BgTilePoolIdleHint => bgTiles.pools[(int) BgTileType.IdleHint];
        private Pool<BlockTileView> BgTilePoolLastMatchHint => bgTiles.pools[(int) BgTileType.OnlyMatchHint];
        private Pool<BlockTileView> BgTilePoolDecor => bgTiles.pools[(int) BgTileType.DecorTile];


        private BlockTileView this[Point point] => _viewMap.GetAtPoint(point);
        private TutorialStep CurrentTutStep => gameTutorial.CurrentTutStep;


        private Point randomAdBreak => DuongExtensions.GetRemotePoint("ad_break_random_from", "ad_break_random_to");

        public int this[GameSaveKey key]
        {
            get => CurrentGameSave[(int) key];
            set => CurrentGameSave[(int) key] = value;
        }

        private BlockItemView SaveSlotItem => spawnItems[3];

        private void LoadRemote()
        {
            var stringKey = CurrentRemoteSettingKey.remote_spawn_setting.ToString();
            if (RemoteConfigController.HasValue(stringKey))
            {
                try
                {
                    _remoteSpawnSetting =
                        JsonUtility.FromJson<BlockSpawnSetting>(RemoteConfigHelper.GetValueString(stringKey));
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
        }

        protected override void RegisterBoard()
        {
            LoadRemote();
            if (!FireBaseController.FireBaseRemoteReady)
                RemoteConfigController.OnInitialized.Action += data => LoadRemote();

            rotateOn.BoolValue = false;
            gameTutorial.Set(this);
            rotateOn.Subscribe(data =>
            {
                if (data == 0)
                    foreach (var blockItemView in spawnItems)
                    {
                        if (!blockItemView.IsUnused() && blockItemView != _draggingItem.Value)
                            blockItemView.ReleaseRotate();
                    }

//                if (data >= 0 && PlayerData.GetCustomProperty((int)CustomPlayerDataProperty.Rotate).Value <= 0)
//                {
//                    rotateOn.Value = 0;
//                }
            });


            bgTiles.Init();
            tiles.Init();
            tiles.Init();
            blockEffects.Init();
            scoreEffects.Init();
            eatLines1.Init();
            eatLines2.Init();
            complimentEffects.Init();
            if (cam == null)
                cam = Camera.main;
            for (var i = 0; i < spawnItems.Length; i++)
                spawnItems[i].SetSpawnParent(itemParents[i]);

            _onlyItemMatch.Subscribe(OnlyItemMatchChanged);

            SubjectController.PopupChanged.Subscribe(data =>
            {
                if (data)
                {
                    if (dragItemState != DragItemState.NotHandler)
                        UnDrag();
                }
            });
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            UnDrag();
        }


        private void CheckOnlyItemMatch()
        {
            CheckPossibleToPlace(out int nValid, out int index, true);
            if (nValid == 1 && PlayerData.tutorialStep == -1)
                _onlyItemMatch.Value = spawnItems[index];
            else
                _onlyItemMatch.Value = null;
        }


        private void OnlyItemMatchChanged(BlockItemView view)
        {
            if (!turnOnOnlyItemMatch)
                return;
            BgTilePoolLastMatchHint.ReturnAll();
            if (view != null)
            {
                var places = IsPossibleToPlace(view.Data, true, out bool moreThanOne);
                if (places != null && !moreThanOne)
                {
                    foreach (var point in places)
                        BgTilePoolLastMatchHint.Rent(null, BgPoints.GetAtPoint(point).value);
                }
            }
        }


        protected override void OnKernelLoadedBoard()
        {
            SetSaveItemChanged();
            if (Kernel.Resolve<PlayFabManager2>().Logged)
            {
                Kernel.Resolve<PlayFabManager2>().LoadPlayerRank(null);
                Kernel.Resolve<PlayFabManager2>().LoadPlayerRankAround(15);
            }

            _draggingItem.Subscribe(data =>
            {
                if (data == null)
                    notEnoughSpace.BoolValue = false;
                else
                    notEnoughSpace.BoolValue = IsPossibleToPlace(data.Data, false, out bool moreThanOne) == null;
            });
        }

        public override bool GetCustomCondition(int customGameCondition)
        {
            if (customGameCondition == 0)
                return true;
            switch ((CustomGameCondition) customGameCondition)
            {
                case CustomGameCondition.None:
                    return true;
                case CustomGameCondition.Undo:
                    return CurrentGameSave != null && CurrentGameSave.Count > 1;
                default:
                    return true;
            }
        }

        public override void Clear()
        {
            base.Clear();
            tiles.ReturnAll();
            for (var i = 0; i < spawnItems.Length; i++)
                spawnItems[i].ClearTiles();
//            foreach (var blockItemView in spawnItems)
//                blockItemView.OnSelect(false,true);
        }

        protected override void InitGame()
        {
            base.InitGame();
            _freeToRotate.BoolValue = true;
            Debug.Log(PlayerData.playTimes);
            BoardSpecialState = BoardSpecialState.None;
            _lose = false;
            tweenSaveSlot.OnChanged(false);
            Positions = new List<PointWithVector3>();
            _bgPoints = new PointWithVector3[CurrentLevel.size.col, CurrentLevel.size.row];
            //  _bgViewMap = new BlockTileView[CurrentLevel.size.col, CurrentLevel.size.row];
            _viewMap = new BlockTileView[CurrentLevel.size.col, CurrentLevel.size.row];
            MyGameSetting.Setup(PointType.Square, CurrentLevel.size.col, CurrentLevel.size.row,
                currentGameSetting.tileDistance, currentGameSetting.tileDistance);

            for (int i = 0; i < CurrentLevel.size.col; i++)
            for (int j = 0; j < CurrentLevel.size.row; j++)
            {
                var point = new Point(i, j);
                CreateBgTile(point);
            }

            void CreateBgTile(Point point)
            {
//                var bgTileView = bgTiles.Rent((int) BgTileType.MainBoard);
//                bgTileView.transform.SetParent(null);
//                bgTileView.SetPos(point);
//                bgTileView.transform.localScale = Vector3.one;
                var positionPoint =
                    new PointWithVector3(point.col, point.row, point.GetGamePos());
                Positions.Add(positionPoint);
                _bgPoints.SetAtPoint(point, positionPoint);
            }

            UpdateActionTime();
            _secondsFromAdBreak = 0;
            _secondsToAdBreak = randomAdBreak.GetRandomBetween();
            _navigationDisposable?.Dispose();
            _navigationDisposable = Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(data =>
                {
                    if (ValidInput() && !IsInTutorial())
                    {
                        if (_currentNavigation < 0)
                            _secondsFromLastAction++;
                        _secondsFromAdBreak++;
//                        Debug.Log(_secondsFromAdBreak+"/"+_secondsToAdBreak);
                        CheckShowAdBreak();
                        _waitForHint++;
                        if (_waitForHint % currentGameSetting.timeToHint == 0)
                            HintPlace();
                    }

                    CheckGameNavigation();
                    PlayerData.logEachGame[(int) LogEachGameEnum.time_play]++;
                })
                .AddToGameDisposable();
        }

        [SerializeField] private float minDragDistance;
        [SerializeField] private Vector2 dragOffset = new Vector2(0, 1);
        [SerializeField] private float dragSpeed = 0.5f;
        [SerializeField] private float returnSpeed = 1;
        [SerializeField] private bool useClickOffset;

        public float DragSpeed() => dragSpeed;


        private float GetMinDistance(BlockItemView drag)
        {
            if (RotateValid.BoolValue && drag.CanRotate())
                return minDragDistance;
            return -1;
        }

//        private bool _itemClicked;
//        private bool _validToDrag;
//        private bool _startDragging;

        [DisplayAsDisable] [SerializeField] private DragItemState dragItemState;

        public override bool IsNotOnAnyAction()
        {
            return dragItemState == DragItemState.NotHandler;
        }

        public override void FreeAction()
        {
            UnDrag();
        }

        [Flags]
        public enum DragItemState
        {
            NotHandler = 0,
            ValidRotate = 1,
            ValidToDrag = 2,
            Dragging = 4,
        }

        private Vector2 _clickOffset;
        private Vector2 _mouseDown;
        private Point _lastPoint;
        private ReactiveProperty<BlockItemView> _draggingItem = new ReactiveProperty<BlockItemView>();
        private Vector3 _oldPos;


        protected override void GameOver()
        {
            base.GameOver();
            PlayerData.customPropertyList[(int) CustomPlayerDataProperty.LoseTimes].Value++;
        }

        protected override void OnStartANewGame()
        {
            base.OnStartANewGame();
            PlayerData.spawnTimes = 50; // after 50 block spawn start spawning star
            _countPlayTime = 0;
            var levels = logLevelStart.Value.ToArray();
            foreach (var level in levels)
            {
                if (PlayerData.playTimes - 1 == level)
                {
                    var logEvent = $"classic_start_level_{(PlayerData.playTimes - 1):D4}";
                    Debug.Log(logEvent);
                    Kernel.Resolve<FireBaseController>().LogEvent(logEvent);
                    Kernel.Resolve<AppFlyerController>().SendEvent(logEvent);
                    break;
                }
            }

            new SonatLogLevelStart()
            {
                level = PlayerData.playTimes.ToString(),
                mode = "classic",
                setUserProperty = true
            }.Post();
        }

        public override void Handler(ParameterTutorialMiniAction action)
        {
            base.Handler(action);
            switch ((ParameterGameActionEnum) action.action)
            {
                case ParameterGameActionEnum.SetToggleRotate:
//                    rotateOn.BoolValue = action.valueBool;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IntReactiveProperty NRotate => PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Rotate];

        private Vector2 _mouseDownPos;

        private void OnMouseDown()
        {
            if (!gameTutorial.IsInTutorial())
                ClearIdleHint();
            bool validToDrag = CurrentTutStep == null ||
                               (CurrentTutStep != null &&
                                (CurrentTutStep.actionEnum == (int) TutorialActionEnum.DragBlock ||
                                 CurrentTutStep.actionEnum == (int) TutorialActionEnum.DragToSaveSlot));

            bool validToClick =
                CurrentTutStep == null || CurrentTutStep.actionEnum == (int) TutorialActionEnum.RotateItem
                                       || CurrentTutStep.actionEnum == (int) TutorialActionEnum.DragBlock ||
                                       CurrentTutStep.actionEnum == (int) TutorialActionEnum.DragToSaveSlot;
            if (validToDrag)
                dragItemState |= DragItemState.ValidToDrag;

            if (validToClick && CurrentTutStep != null &&
                CurrentTutStep.actionParam == (int) TutorialActionParam.FromSaveSlot)
            {
                Vector2 origin = cam.ScreenToWorldPoint(Input.mousePosition);
                var hit = DuongExtensions.RayCast<BlockItemView>(origin);
                if (hit == null)
                {
                    var tile = DuongExtensions.RayCast<BlockTileView>(origin);
                    if (tile != null)
                        hit = tile.ParentBlock;
                }

                validToClick = false;
                for (var i = 0; i < spawnItems.Length; i++)
                {
                    if (spawnItems[i] == hit && i == CurrentTutStep.DragItemIndex)
                        validToClick = true;
                }
            }

            if (validToClick)
            {
                Vector2 origin = cam.ScreenToWorldPoint(Input.mousePosition);
                var hit = DuongExtensions.RayCast<BlockItemView>(origin);
                if (hit == null)
                {
                    var tile = DuongExtensions.RayCast<BlockTileView>(origin);
                    if (tile != null)
                        hit = tile.ParentBlock;
                }

                if (hit != null && !hit.BusyMoving && !hit.BusyRotate) // in boardArranger tile must deactive collider
                {
                    Debug.Log("hit.BusyRotate" + hit.BusyRotate);
                    _mouseDown = cam.WorldToScreenPoint(Input.mousePosition);
                    _draggingItem.Value = hit;
                    dragItemState |= DragItemState.ValidRotate;
                    _clickOffset = (Vector2) _draggingItem.Value.transform.position - origin;
                    _clickOffset.x = 0;
                }
            }
        }

        private void OnMouse()
        {
            // pick up
            if ((dragItemState & DragItemState.ValidToDrag) != 0)
            {
                var distance = (Vector2) cam.WorldToScreenPoint(Input.mousePosition) - _mouseDown;
                if (distance.magnitude >= GetMinDistance(_draggingItem.Value))
                {
//                    dragItemState = dragItemState & ~DragItemState.ValidToDrag;
//                    dragItemState = dragItemState & ~DragItemState.ValidRotate;
                    dragItemState = DragItemState.Dragging;
                    _lastPoint = null;
                    _oldPos = _draggingItem.Value.dragTransform.localPosition;
                    _mouseDownPos = cam.ScreenToWorldPoint(Input.mousePosition);
                    _clickOffset = (Vector2) _draggingItem.Value.transform.position - _mouseDownPos;
                    if (_clickOffset.y > 0)
                        _clickOffset.y = 0;
                    _draggingItem.Value.blockTweenSelectScript.SetAddLocalPos(-_clickOffset);

                    // if (!IsInTutorial())
                    _draggingItem.Value.SetSortOrder(SortOrderEnum.OnSelect);
                    _draggingItem.Value.OnSelect(true, false);
                    _draggingItem.Value.blockTweenSelectScript.OnChanged(true, false);
                    ((int) SoundEnum.ItemPickup).PlaySound();

                    gameTutorial.TurnOffDisplay(false, true);
                }
            }

            if (dragItemState == DragItemState.Dragging)
            {
                var newPos = (Vector2) cam.ScreenToWorldPoint(Input.mousePosition) - _mouseDownPos;
                _draggingItem.Value.dragTransform.localPosition = newPos;
                if (Vector2.Distance(_oldPos, newPos) > 0.05f)
                {
                    _oldPos = newPos;
                    var placePoint = GetDragPoint(_draggingItem.Value.RootTransform);
                    if (placePoint != _lastPoint)
                    {
                        ReleaseColor();
                        _lastPoint = placePoint;
                        //   UIDebugLog.Log("change to" + ((_lastPoint != null) ? _lastPoint.toString() : "null"),LogType.Service);
                        // to hint able-to-place tile
                        BgTilePoolPlaceHint.ReturnAll();
                        if (IsPlaceValid(placePoint, _draggingItem.Value.Data,
                            out IList<PointParameter> placePoints))
                        {
                            for (var i = 0; i < placePoints.Count; i++)
                                CreateHintTile(placePoints[i], placePoints[i].value);

                            ChangeColorOfEat(placePoints);
                        }
                    }

                    Vector2 origin = _draggingItem.Value.RootTransform.position;
                    var saveSlot = DuongExtensions.RayCast<SaveSlotView>(origin);
                    if (saveSlot == null)
                        saveSlot = DuongExtensions.RayCast<SaveSlotView>(_draggingItem.Value.transform.position);
                    bool validToSaveSlot = PlayerData.tutorialStep == -1 || CurrentTutStep == null ||
                                           CurrentTutStep != null && CurrentTutStep.actionEnum ==
                                           (int) TutorialActionEnum.DragToSaveSlot;

                    // handler drag to save slot move
                    tweenSaveSlot.OnChanged(saveSlot && SaveSlotItem.IsUnused() && validToSaveSlot);
                }
            }
        }

        private void OnMouseUp()
        {
            tweenSaveSlot.OnChanged(false);
            BgTilePoolPlaceHint.ReturnAll();
            if ((dragItemState & DragItemState.ValidRotate) != 0)
            {
                bool validToRotate = CurrentTutStep == null ||
                                     CurrentTutStep.actionEnum == (int) TutorialActionEnum.RotateItem;
                if (RotateValid.BoolValue && validToRotate &&
                    (NRotate.Value > 0 || CurrentTutStep == null || CurrentTutStep.actionEnum ==
                     (int) TutorialActionEnum
                         .RotateItem) && _draggingItem.Value.AddRotate())
                {
                    GameActionEvent.SaveBoard.OnNext();
                    GameActionEvent.SavePlayerData.OnNext();
                    if (_onlyItemMatch.Value != null) // recalculate last item match
                        OnlyItemMatchChanged(_onlyItemMatch.Value);
                    if (CurrentTutStep != null && CurrentTutStep.actionEnum == (int) TutorialActionEnum.RotateItem)
                    {
                        _draggingItem.Value.ClearRotateForTut();
                        NextTut(CurrentTutStep.nextTutAdd, CurrentTutStep.actionBeforeNext);
                    }
                }
            }

            if (dragItemState == DragItemState.Dragging)
            {
                // check if it is drag to save slot
                Vector2 origin = _draggingItem.Value.RootTransform.position;
                var saveSlot = DuongExtensions.RayCast<SaveSlotView>(origin);
                if (saveSlot == null)
                    saveSlot = DuongExtensions.RayCast<SaveSlotView>(_draggingItem.Value.transform.position);
                bool validToSaveSlot = PlayerData.tutorialStep == -1 || CurrentTutStep == null ||
                                       CurrentTutStep != null && CurrentTutStep.actionEnum ==
                                       (int) TutorialActionEnum.DragToSaveSlot;


                // handler drag to save slot move
                if (saveSlot && SaveSlotItem.IsUnused() && validToSaveSlot)
                {
                    CurrentGameSave[(int) GameSaveKey.Moves]++;
                    var data = _draggingItem.Value.Data;
                    SaveSlotItem.Setup(data);
                    _draggingItem.Value.ClearTiles();
                    SaveSlotItem.SetActive(true);
                    _draggingItem.Value.SetActive(false);
                    SaveSlotItem.Appear();
                    _draggingItem.Value = null;
                    CheckSpawn();
                    GameActionEvent.SaveBoard.OnNext();
                    if (CurrentTutStep != null &&
                        CurrentTutStep.actionEnum == (int) TutorialActionEnum.DragToSaveSlot)
                        NextTut(CurrentTutStep.nextTutAdd, CurrentTutStep.actionBeforeNext);
                    AddSave();
                }
                else
                {
                    var placePoint = GetDragPoint(_draggingItem.Value.RootTransform);
                    bool validPlace = PlayerData.tutorialStep == -1 || CurrentTutStep == null ||
                                      CurrentTutStep != null && (CurrentTutStep.actionEnum ==
                                                                 (int) TutorialActionEnum.DragBlock)
                                                             && placePoint == CurrentTutStep.PlacePoint;

                    if (validPlace && IsPlaceValid(placePoint, _draggingItem.Value.Data,
                            out IList<PointParameter> placePoints))
                    {
                        CurrentGameSave[(int) GameSaveKey.Moves]++;
                        if (PlayerData.tutorialStep >= 0)
                        {
                            SubjectController.TextUpdater.OnNext(new TextUpdateData((int) TextUpdateKey.Tutorial,
                                CurrentTutStep.doneMessage));
                            gameTutorial.StopHand();
                        }

                        ChangeScore(placePoints.Count, SetDataType.AddThenTween);

                        if (_draggingItem.Value.Data.rotate > 0 &&
                            !(_draggingItem.Value.Data.rotate % 2 == 0 && _draggingItem.Value.Data.IsOneLine()))
                        {
                            NRotate.Value--;
                            SubjectController.OnGlobalEffect.OnNext((int)GlobalEffectEnum.RotateSub);
                            CurrentGameSave[(int) GameSaveKey.UseBoosterCount]++;
                            if (NRotate.Value == 0)
                            {
                                TurnIndicateBuyRotate(true);
                            }

                            new SonatLogUseBooster()
                            {
                                level = PlayerData.playTimes.ToString(),
                                mode = "classic",
                                name = "rotate"
                            }.Post();
                        }
                        // turn off rotate


                        _draggingItem.Value.ReleaseTile(placePoints, null, _viewMap);
                        CheckWin();
                        ((int) SoundEnum.ItemPlace).PlaySound();


                        _draggingItem.Value.ResetToSpawn();
                        _draggingItem.Value.SetActive(false);
                        _draggingItem.Value = null;

                        if (PlayerData.tutorialStep >= 0)
                        {
                            PlayerData.tutorialStep++;
                            gameTutorial.TurnOffDisplay(false);
                        }

                        if (PlayerData.tutorialStep >= 0)
                            ClearIdleHint();


                        _onlyItemMatch.Value = null;
                        Observable.FromCoroutine(() => EatCoroutine(placePoints))
                            .Subscribe(__ =>
                            {
                                if (PlayerData.tutorialStep < 0 || !InitTut())
                                    CheckSpawn();

                                // when having waiting node
                                if (PlayerData.tutorialStep < -1 && CurrentTutStep == null)
                                {
                                    var waitStep = gameTutorial.GetWaitStep(PlayerData.tutorialStep);
                                    if (waitStep.actionEnum == (int) TutorialActionEnum.DragToSaveSlot &&
                                        !SaveSlotItem.gameObject.activeSelf &&
                                        spawnItems.Count(x => x.gameObject.activeSelf) == 1)
                                    {
                                        for (var i = 0; i < 3; i++)
                                        {
                                            if (spawnItems[i].gameObject.activeInHierarchy &&
                                                IsPossibleToPlace(spawnItems[i].Data, false, out bool moreThanOne) ==
                                                null)
                                                gameTutorial.CheckToTurnDragToSaveSlot();
                                        }
                                    }
                                }

                                CheckBest();
                            }).AddToGameDisposable();
                    }
                    else
                    {
                        ((int) SoundEnum.ItemFail).PlaySound();
                        if (CurrentTutStep != null)
                            gameTutorial.TurnOffDisplay(true);

                        if (!IsInTutorial())
                            _draggingItem.Value.SetSortOrder(SortOrderEnum.OnSpawn);
                        _draggingItem.Value.ReturnToRightPlace(returnSpeed).Subscribe(data => { });
                        for (var i = 0; i < _viewMap.GetLength(0); i++)
                        for (var i1 = 0; i1 < _viewMap.GetLength(1); i1++)
                            if (_viewMap[i, i1] != null)
                                _viewMap[i, i1].SetColor();
                        CheckOnlyItemMatch();
                    }
                }
            }
        }

        [SerializeField] private ToggleScript[] indicateRotateShop;

        private void TurnIndicateBuyRotate(bool b)
        {
            indicateRotateShop.OnChanged(b);
            if (b)
                foreach (var blockItemView in spawnItems)
                {
                    if (!blockItemView.IsUnused() && blockItemView != _draggingItem.Value)
                        blockItemView.ReleaseRotate();
                }
        }

        private float _countPlayTime;

        protected override void OnInputUpdate()
        {
            _countPlayTime += Time.deltaTime;
            if (_countPlayTime > 5)
            {
                _countPlayTime -= 5;
                CurrentGameSave[(int) GameSaveKey.TimeSeconds] += 5;
            }


            if (Input.GetMouseButtonDown(0))
            {
                OnMouseDown();
                TurnIndicateBuyRotate(false);
            }

            if (Input.GetMouseButton(0) && _draggingItem.Value != null)
                OnMouse();
            if (Input.GetMouseButtonUp(0))
            {
                if (_draggingItem.Value != null)
                    OnMouseUp();
                _draggingItem.Value = null;
                dragItemState = DragItemState.NotHandler;
            }
        }

        public void UnDrag()
        {
            dragItemState = DragItemState.NotHandler;
            if (_draggingItem.Value == null)
                return;
            _draggingItem.Value.ReturnToRightPlace(returnSpeed).Subscribe(data => { });
            _draggingItem.Value = null;
            for (var i = 0; i < _viewMap.GetLength(0); i++)
            for (var i1 = 0; i1 < _viewMap.GetLength(1); i1++)
                if (_viewMap[i, i1] != null)
                    _viewMap[i, i1].SetColor();
            BgTilePoolPlaceHint.ReturnAll();
        }

        public void SpawnItemFromTut(ListPoint[] items)
        {
            for (var i = 0; i < 3; i++)
                spawnItems[i].SetActive(false);
            var spawns = items.Select(listPoint =>
                    listPoint == null
                        ? null
                        : new BlockItem(listPoint, listPoint.customParameter.intValue,
                            listPoint.customParameter.boolValue, null))
                .ToArray();
            for (var i = 0; i < spawns.Length; i++)
            {
                if (spawns[i] != null) // && spawns[i].active)
                {
                    spawnItems[i].SetActive(true);
                    spawns[i].canRotate = false;
                    spawnItems[i].Setup(spawns[i]);
                }
                else
                    spawnItems[i].SetActive(false);
            }

            SetSaveItemChanged();
        }

        private void SetSaveItemChanged()
        {
            for (var i = 0; i < spawnItems.Length; i++)
                spawnItems[i].SetIsSavedItem(i == 3);
        }


        private void CheckSpawn()
        {
            Debug.Log("check spawn");
            bool readyToSpawn = true;
            for (var i = 0; i < 3; i++)
                if (!spawnItems[i].IsUnused())
                    readyToSpawn = false;

            if (readyToSpawn)
            {
                Spawn(null, true);
                GameActionEvent.SaveBoard.OnNext();
            }

            if (!CheckPossibleToPlace())
                GameActionEvent.BoardLose.OnNext();

            CheckOnlyItemMatch();
        }

        protected override void HandlerBoardLose()
        {
            if (!_lose)
            {
                base.HandlerBoardLose();
                UnDrag();
            }
        }

        private bool _lose;

        protected override IEnumerator IeBoardLose()
        {
            _lose = true;
            Pause.Value = true;
            StopInput();
            UnDrag();
            Kernel.Resolve<PlayFabManager2>().LoadPlayerRankAround(15);
            Time.timeScale = 1;
            yield return new WaitForSeconds(1);
            ((int) SoundEnum.BoardFinish).PlaySound();

            for (var i1 = 0; i1 < spawnItems.Length; i1++)
            {
                if (!spawnItems[i1].IsUnused())
                    spawnItems[i1].SetPossibleToPlace(false);
                spawnItems[i1].ClearRotate();
            }

            for (var i = 0; i < _viewMap.GetLength(0); i++)
            for (var i1 = 0; i1 < _viewMap.GetLength(1); i1++)
                if (_viewMap[i, i1] != null)
                    _viewMap[i, i1].Jump();


            yield return new WaitForSeconds(currentGameSetting.waitBoardLose);
            yield return new WaitForSeconds(currentGameSetting.waitLoseClearScreen);

            Pause.Value = false;
            GameOver();
            _lose = false;
        }

        public bool CheckPossibleToPlace()
        {
            return CheckPossibleToPlace(out int nValid, out int i);
        }

        public bool CheckPossibleToPlace(out int total, out int index, bool checkTutSaveSlot = false)
        {
            total = 0;
            index = 0;
            int countActive = 0;
            var possible = false;
            for (var i = 0; i < spawnItems.Length; i++)
            {
                var item = spawnItems[i];
                if (!item.IsUnused())
                {
                    countActive++;
                    var places = IsPossibleToPlace(item.Data, false, out bool moreThanOne);
                    item.SetPossibleToPlace(places != null);
                    if (places != null)
                    {
                        possible = true;
                        index = i;
                        total++;
                    }
                }
            }

            hintDragToSaveSlot.OnChanged(0);
            if (!IsInTutorial())
            {
                if (checkTutSaveSlot && (!possible && SaveSlotItem.IsUnused()))
                {
                    if (countActive == 1 && _lastMoves != CurrentGameSave[(int) GameSaveKey.Moves])
                    {
                        hint_save_slot_times.Value++;
                        _lastMoves = CurrentGameSave[(int) GameSaveKey.Moves];
                    }

                    BoardSpecialState = BoardSpecialState.FirstHintDragToSave;
                    foreach (var blockItemView in spawnItems)
                        blockItemView.SetSortOrder(SortOrderEnum.OnSpawn);
                    if (hintDragToSaveSlot != null)
                        hintDragToSaveSlot.OnChanged(hint_save_slot_times.Value != 1 ? 2 : 1);
                    gameTutorial.StartHandToSaveSlot(true);
                }
                else if (BoardSpecialState == BoardSpecialState.FirstHintDragToSave)
                {
                    BoardSpecialState = BoardSpecialState.None;
                    foreach (var blockItemView in spawnItems)
                        blockItemView.SetSortOrder(SortOrderEnum.OnSpawn);
                    if (hintDragToSaveSlot != null)
                        hintDragToSaveSlot.OnChanged(0);
                    gameTutorial.StartHandToSaveSlot(false);
                }
            }
            else
            {
                if(CurrentTutStep.actionEnum == (int)(TutorialActionEnum.DragToSaveSlot) && (hintDragToSaveSlot != null))
                    hintDragToSaveSlot.OnChanged(2);
            }

            return possible || SaveSlotItem.IsUnused();
        }


        // for net recheck drag to save until item changed
        private int _lastMoves = -1;
        private PlayerPrefInt hint_save_slot_times = new PlayerPrefInt("hint_save_slot_times");

        private IList<PointParameter> IsPossibleToPlace(BlockItem desc, bool checkMoreThanOne, out bool moreThanOne)
        {
            moreThanOne = false;
            if (!checkMoreThanOne)
            {
                foreach (var point in Positions)
                    if (IsPlaceValid(point, desc, out IList<PointParameter> points))
                        return points;
            }
            else
            {
                int c = 0;
                IList<PointParameter> result = null;
                foreach (var point in Positions)
                    if (IsPlaceValid(point, desc, out IList<PointParameter> points))
                    {
                        c++;
                        result = points;
                        if (c == 2)
                        {
                            moreThanOne = true;
                            return points;
                        }
                    }

                return result;
            }


            return null;
        }

        private void ChangeScore(int score, SetDataType setDataType)
        {
            if (setDataType.HasFlag(SetDataType.Set))
                WaveData.currentScore.Value = score;
            if (setDataType.HasFlag(SetDataType.Add) && (!IsInTutorial() || currentGameSetting.scoringInTutorial))
                WaveData.currentScore.Value += score;
            if (setDataType.HasFlag(SetDataType.Clear))
                WaveData.currentScore.Value = 0;
            if (setDataType.HasFlag(SetDataType.ClearAll))
            {
                PlayerData.bestScore.Value = 0;
                WaveData.currentScore.Value = 0;
            }

            if (setDataType.HasFlag(SetDataType.Tween))
                TweenScore(TextTweenType.TweenTo, WaveData.currentScore.Value);
        }

        private void CheckBest()
        {
            if (WaveData.currentScore.Value > PlayerData.bestScore.Value)
            {
                PlayerData.bestScore.Value = WaveData.currentScore.Value;
                if (CurrentGameSave[(int) GameSaveKey.NewRecord] == 0
                    && PlayerData.bestScore.Value >= CurrentGameSave[(int) GameSaveKey.StartBestScore]
                    && PlayerData.tutorialStep < 0
                    && CurrentGameSave[(int) GameSaveKey.StartBestScore] > 10
                    && ValidInput())
                {
                    CurrentGameSave[(int) GameSaveKey.NewRecord] = 1;
                    Debug.Log("show popup new record");
                    UnDrag();
                    ScreenRoot.dialogController.Resolve<DialogMiniPop>().ShowMiniPop((int) MiniPopEnum.Best);
                    
                    new SonatLogPostScore()
                    {
                        level = PlayerData.playTimes.ToString(),
                        score =  WaveData.currentScore.Value,
                        mode = "classic"
                    }.Post();
       
                }
            }
        }

        public void TestPopupBest()
        {
            CurrentGameSave[(int) GameSaveKey.NewRecord] = 1;
            Debug.Log("show popup new record");
            UnDrag();
            ScreenRoot.dialogController.Resolve<DialogMiniPop>().ShowMiniPop((int) MiniPopEnum.Best);
        }

        private bool _isEating;
        private bool _busy;

        protected override void OnApplicationFocus(bool hasFocus)
        {
            base.OnApplicationFocus(hasFocus);
            UnDrag();
        }

        public override void LogCustom()
        {
            base.LogCustom();

            WaveData.currentScore.Value = CurrentGameSave[(int) GameSaveKey.StartBestScore] + 1;
            SubjectController.GameActionEvent.OnNext(GameActionEvent.BoardLose);

//            var fx = scoreEffects.pools[0].Rent(null, BgPoints.GetAtPoint(new Point(4, 4)).value);
//            fx.SetIndex(10); // set score
//            fx.SetIndex2(0);
//
//            .5f.Timer(() =>
//            {
//                complimentEffects.pools[0].Rent(null, Vector3.zero).SetIndex(UnityEngine.Random.Range(0, 3));
//                SubjectController.OnGlobalEffect.OnNext((int) AnimatorCallbackEnum.ShakeCamera);
//            });
        }


        private IEnumerator EatCoroutine(IList<PointParameter> points)
        {
            var eat = GetEat(points).ToArray();
            if (eat.Length == 0)
            {
//            UpdateCheckMap();
//            if (checkMap.MyAll(x => x))
//            {
//                SubjectController.OnGameActionEvent.OnNext(GameActionEvent.ForceLose);
//                yield break;
//            }
            }
            else
            {
                ((int) SoundEnum.LineEliminate).PlaySound();
                explodeSoundCheck = new bool[currentGameSetting.soundCount];
                int eatScore = eat.Length * 10;
                ChangeScore(eatScore, SetDataType.AddThenTween);

                List<Point> eatPoints = new List<Point>();
                foreach (var point in points)
                {
                    if (eat.Any(x => x.ContainsPoint(point)))
                        eatPoints.Add(point);
                }

                var orderPoints = eatPoints.OrderBy(x => x.MaxStepDistance(eatPoints, PointType.Square)).ToArray();
                var eatPoint = orderPoints[0];
                var fx = scoreEffects.pools[0].Rent(null, BgPoints.GetAtPoint(eatPoint).value);
                fx.SetIndex(eatScore); // set score
                fx.SetIndex2(points[0].value);

                if (eat.Length >= 2 && !IsInTutorial())
                {
                    int complimentIndex = Mathf.Min(eat.Length - 2, 2);
                    var pos = BgPoints.GetAtPoint(eatPoint).value;

                    .25f.Timer(() =>
                    {
                        for (var i = 0; i < eat.Length; i++)
                        {
                            var e = eat[i];
                            switch (e.Type)
                            {
                                case EatType.Column:
                                    eatLines1.pools[complimentIndex].Rent(null,
                                        new Vector3(Point.Create(e.Value, 0).GetGamePos().x, 0));
                                    break;
                                case EatType.Row:
                                    eatLines2.pools[complimentIndex].Rent(null,
                                        new Vector3(0, Point.Create(e.Value, 0).GetGamePos().x));
                                    break;
                                case EatType.Column2:
                                    break;
                                case EatType.Row2:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    });

                    0.25f.Timer(() =>
                    {
                        pos.x = Mathf.Clamp(pos.x, -myBoardSettings.complimentClampPosX,
                            myBoardSettings.complimentClampPosX);
                        if (boardSetting.complimentSpawnPos == FxSpawnPos.AtEatPoint)
                            complimentEffects.pools[0].Rent(null, pos).SetIndex(complimentIndex);
                        if (boardSetting.complimentSpawnPos == FxSpawnPos.Center)
                            complimentEffects.pools[0].Rent(null, Vector3.zero).SetIndex(complimentIndex);
                        SubjectController.OnGlobalEffect.OnNext((int) GlobalEffectEnum.ShakeCamera);
                    });
                }

                // get line then eliminate
                foreach (var eatData in eat)
                {
                    switch (eatData.Type)
                    {
                        case EatType.Column:
                            var placeRow = points.Where(x => x.col == eatData.Value).Select(x => x.row).ToList();
                            var nearestCenter = placeRow.OrderBy(v => Math.Abs((long) v - CurrentLevel.size.col / 2))
                                .First();

                            for (int i = 0; i < CurrentLevel.size.row; i++)
                            {
                                var tile = _viewMap[eatData.Value, i];
                                if (tile != null)
                                {
                                    tile.DelayToDestroy = Mathf.Abs(nearestCenter - i);
                                    tile.DestroyView();
                                    _viewMap[eatData.Value, i] = null;
                                }
                            }

                            break;
                        case EatType.Row:
                            var placeCol = points.Where(x => x.row == eatData.Value).Select(x => x.col).ToList();
                            var nearestCenter2 = placeCol.OrderBy(v => Math.Abs((long) v - CurrentLevel.size.row / 2))
                                .First();


                            for (int i = 0; i < CurrentLevel.size.col; i++)
                            {
                                var tile = _viewMap[i, eatData.Value];
                                if (tile != null)
                                {
                                    tile.DelayToDestroy = Mathf.Abs(nearestCenter2 - i);
                                    tile.DestroyView();
                                    _viewMap[i, eatData.Value] = null;
                                }
                            }

                            break;
                        case EatType.Column2:
                            break;
                        case EatType.Row2:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                yield return new WaitForSeconds(currentGameSetting.waitEating);
            }

            GameActionEvent.AddSave.OnNext();
            _isEating = false;
        }


        private bool IsPlaceValid(Point placePoint, BlockItem item, out IList<PointParameter> hintPoints)
        {
            if (placePoint == null)
            {
                hintPoints = null;
                return false;
            }

            if (item == null)
            {
                Debug.LogError("item is null", gameObject);
                hintPoints = null;
                return false;
            }

            var calculate = item.CalculatePlacedPoints(placePoint);
            var placePoints = calculate.Select(x => x.ToPointWithTwoValue(item.color, 0))
                .ToArray();
            foreach (var point in placePoints)
            {
                if (!_viewMap.Contains(point) || _viewMap.GetAtPoint(point) != null)
                {
                    hintPoints = null;
                    return false;
                }
            }

            hintPoints = placePoints;
            return true;
        }

        private void CreateHintTile(Point point, int color)
        {
            var hintTile = BgTilePoolPlaceHint.Rent();
            hintTile.transform.SetParent(null);
            hintTile.SetPos(point);
            hintTile.SetColor(color);
        }

        private void ReleaseColor()
        {
            for (var i = 0; i < _viewMap.GetLength(0); i++)
            for (var i1 = 0; i1 < _viewMap.GetLength(1); i1++)
                if (_viewMap[i, i1] != null)
                    _viewMap[i, i1].SetColor();
        }

        private void ChangeColorOfEat(IList<PointParameter> points)
        {
            ReleaseColor();
            var eat = GetEat(points).ToArray();
            foreach (var eatData in eat)
            {
                switch (eatData.Type)
                {
                    case EatType.Column:
                        for (int i = 0; i < CurrentLevel.size.row; i++)
                            if (_viewMap[eatData.Value, i] != null)
                                _viewMap[eatData.Value, i].SetColor(points[0].value, false);
                        break;
                    case EatType.Row:
                        for (int i = 0; i < CurrentLevel.size.col; i++)
                            if (_viewMap[i, eatData.Value] != null)
                                _viewMap[i, eatData.Value].SetColor(points[0].value, false);

                        break;
                    case EatType.Column2:
                        break;
                    case EatType.Row2:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        enum EatType
        {
            Column,
            Row,
            Column2,
            Row2,
        }


        IEnumerable<EatData> GetEat<T>(IList<T> placePoints) where T : Point
        {
            bool[,] checkMap = new bool[_viewMap.GetLength(0), _viewMap.GetLength(1)];
            foreach (var placePoint in placePoints)
                checkMap.SetAtPoint(placePoint, true);

            for (var i = 0; i < _viewMap.GetLength(0); i++)
            for (var i1 = 0; i1 < _viewMap.GetLength(1); i1++)
                checkMap[i, i1] = _viewMap[i, i1] != null | checkMap[i, i1];

            for (int i = 0; i < CurrentLevel.size.col; i++)
                if (checkMap.GetCol(i).All(x => x))
                {
                    yield return new EatData()
                    {
                        Type = EatType.Column,
                        Value = i,
                    };
                }

            for (int i = 0; i < CurrentLevel.size.row; i++)
                if (checkMap.GetRow(i).All(x => x))
                {
                    yield return new EatData()
                    {
                        Type = EatType.Row,
                        Value = i,
                    };
                }
        }

        private class EatData
        {
            public EatType Type;
            public int Value;

            public bool ContainsPoint(Point point)
            {
                switch (Type)
                {
                    case EatType.Column:
                        return point.col == Value;
                    case EatType.Row:
                        return point.row == Value;
                    case EatType.Column2:
                        return point.col == Value;
                    case EatType.Row2:
                        return point.row == Value;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return false;
            }
        }


        private Point GetDragPoint(Transform dragTransform)
        {
            switch (MyGameSetting.PointSetup.PointType)
            {
                case PointType.Square:
                    return GetClosestPoint(dragTransform, MyGameSetting.PointSetup.GetDistance().x * 0.75f);
                case PointType.HexaHorizontal:
                    return GetClosestPoint(dragTransform, MyGameSetting.PointSetup.GetDistance().x * 0.75f);
                case PointType.HexaVertical:
                    return GetClosestPoint(dragTransform, MyGameSetting.PointSetup.GetDistance().x * 0.75f);
                case PointType.Triangle:
                    return GetClosestPoint(dragTransform, MyGameSetting.PointSetup.GetDistance().x * 0.75f);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Point GetClosestPoint(Transform dragTransform, float validDistance)
        {
            var position = dragTransform.position;
            Point point = null;
            var min = validDistance;
            for (var i = 0; i < Positions.Count; i++)
            {
                var distance = Vector2.Distance(position, Positions[i].value);
                if (distance < min)
                    point = Positions[i];
            }

            return point;
        }

        private int _waitForHint;

        public void ClearIdleHint()
        {
            BgTilePoolIdleHint.ReturnAll();
            _waitForHint = 0;
        }

        private void HintPlace()
        {
            if (_viewMap.MyCount(x => x == null) < 20 && PlayerData.tutorialStep < 0 && _onlyItemMatch.Value == null)
            {
                CheckPossibleToPlace(out int totalMatch, out int index);
                if (totalMatch > 0)
                {
                    ClearIdleHint();
                    for (var i = 0; i < spawnItems.Length; i++)
                        if (!spawnItems[i].IsUnused())
                        {
                            var places = IsPossibleToPlace(spawnItems[i].Data, false, out bool moreThanOne);
                            if (places != null)
                            {
                                foreach (var point in places)
                                    CreateHelpTile(point, point.value);
                                break;
                            }
                        }
                }

                void CreateHelpTile(Point point, int color)
                {
                    var hintTile = BgTilePoolIdleHint.Rent(null, BgPoints.GetAtPoint(point).value);
                    hintTile.SetColor(color);
                }
            }
        }


        public override void LoadState(BoardState state)
        {
            base.LoadState(state);
            tiles.pools[0].ReturnAll();
            FillPoints(state.points);
            SaveSlotItem.ClearTiles();
            Spawn(state.spawns, false);
            CheckPossibleToPlace();
        }

        public override BoardState GetState()
        {
            return new BoardState
            {
                score = WaveData.currentScore.Value,
                points = GetSerialData().ToArray(),
                spawns = GetSpawn().ToArray(),
            };

            IEnumerable<BlockItem> GetSpawn()
            {
                foreach (var blockItemView in spawnItems)
                    yield return blockItemView.GetSave();
            }
        }

        IEnumerable<PointParameter> GetSerialData()
        {
            for (var i = 0; i < _viewMap.GetLength(0); i++)
            for (var i1 = 0; i1 < _viewMap.GetLength(1); i1++)
                if (_viewMap[i, i1] != null)
                    yield return _viewMap[i, i1].Point
                        .ToPointWithTwoValue(_viewMap[i, i1].Color, _viewMap[i, i1].GetStar() ? 1 : 0);
        }


        public override void Undo()
        {
            throw new NotImplementedException();
        }

        public override bool IsInTutorial()
        {
            return gameTutorial.IsInTutorial();
        }

        protected override bool InitTut()
        {
            base.InitTut();
            //    rotateOn.BoolValue = PlayerData.tutorialStep >= 0;
            bool init = gameTutorial.InitTut(IsTutNexted);
            CheckPossibleToPlace();
            return init;
        }

        public override void StopTut(int indexWhenStop)
        {
            base.StopTut(indexWhenStop);
            Kernel.LogFinishTutorial(indexWhenStop);
            gameTutorial.StopHand();
            if (indexWhenStop == -1)
            {
                Debug.Log("finish all tut");
                FxOnFinishTut();
                tiles.ReturnAll();
                for (var i = 0; i < spawnItems.Length; i++)
                    spawnItems[i].ClearTiles();
                _viewMap.Populate(null);
                CurrentGameSave = CreateGameSaveFromCurrentLevel();
                Spawn(null, true);
                ChangeScore(0, SetDataType.ClearAll);
            }
        }

        public void FxOnFinishTut()
        {
            var centerPoints = new[]
            {
                Point.Create(4,4),
                Point.Create(4,5),
                Point.Create(5,4),
                Point.Create(5,5),
            };
          
            foreach (var point in _bgPoints)
                (point.MinRange(centerPoints, PointType.Square) * timeSetting.delayDecorFinishTut).Timer(() => BgTilePoolDecor.Rent().SetPos(point));
            timeSetting.delayNextDcor.Timer(() =>
            {
                foreach (var point in _bgPoints)
                    (point.MinRange(centerPoints, PointType.Square) * timeSetting.delayDecorFinishTut).Timer(() => BgTilePoolDecor.Rent().SetPos(point));
            });
        }

        protected override void OnSkipTutorial()
        {
            PlayerData.bestScore.Value = 0;
            ChangeScore(0, SetDataType.ClearAll | SetDataType.Tween);
            base.OnSkipTutorial();
            ScreenRoot.dialogController.Resolve<DialogMiniPop>().ShowMiniPop((int) MiniPopEnum.Goal);
        }

        private void CheckOutOfMove()
        {
            var noMove = NoMovePossible();
            if (noMove)
                ScreenRoot.popupManager.ShowPopup<PopupOutOfMove>();
        }

        private bool NoMovePossible()
        {
            return false;
        }

        private void UpdateMap()
        {
            _viewMap.Populate(null);
            foreach (var blockView in tiles.pools[0].ActiveItems)
            {
                if (_viewMap.OutOfRange(blockView.Point))
                    Debug.Log("out of range" + blockView.Point.toString(), blockView);
                _viewMap.SetAtPoint(blockView.Point, blockView);
            }
        }

        public void TestSpawn()
        {
            Spawn(null, true);
        }


        public void Spawn(BlockItem[] spawns, bool addSave)
        {
            for (var i = 0; i < 3; i++)
                spawnItems[i].SetActive(false);
            if (spawnItems[3].Data == null)
                spawnItems[3].SetActive(false);


            _freeToRotate.BoolValue = false;
            0.65f.Timer(() => _freeToRotate.BoolValue = true);
            
            if (spawns == null || spawns.Length == 0 || spawns.All(x => !x.active))
                spawns = GetSpawnBlockSonat().ToArray();

            for (var i = 0; i < spawns.Length; i++)
            {
                if (spawns[i] != null && spawns[i].active)
                {
                    spawnItems[i].SetActive(true);
                    spawnItems[i].Setup(spawns[i]);
                }
                else
                    spawnItems[i].SetActive(false);
            }

            CheckSpawn();
            CheckPossibleToPlace();
            if (addSave)
                AddSave();

            SetSaveItemChanged();
        }

//        IEnumerable<BlockItem> GetSpawnBlockDuong()
//        {
//            var clrs = Enumerable.Range(0, currentGameSetting.totalColor).ToList();
//            clrs.Shuffle(new System.Random());
//            var maps = currentGameSetting.map.ToList();
//            for (var i = 0; i < Mathf.Min(itemParents.Length, 3); i++)
//            {
//                var des = maps.TakeByWeight(maps.Select(x => x.values.y).ToList());
//                maps.Remove(des);
//                List<int> stars = new List<int>();
//                // value z for canRotate
//                for (int j = 0; j < des.points.Count; j++)
//                {
//                    PlayerData.spawnTimes--;
//                    if (PlayerData.spawnTimes < 0)
//                    {
//                        PlayerData.spawnTimes = currentGameSetting.randomRange.GetRandom();
//                        stars.Add(j);
//                    }
//                }
//
//                yield return new BlockItem(des, clrs[i], des.values.z > 0, stars);
//            }
//        }

        IEnumerable<BlockItem> GetSpawnBlockSonat()
        {
            var colors = Enumerable.Range(0, currentGameSetting.totalColor).ToList();
            colors.Shuffle(new System.Random());
            for (var i = 0; i < Mathf.Min(itemParents.Length, 3); i++)
            {
                var des = GetRandomDescriptor(WaveData.currentScore.Value);
                List<int> stars = new List<int>();
                // value z for canRotate
                for (int j = 0; j < des.points.Count; j++)
                {
                    PlayerData.spawnTimes--;
                   
                    if (PlayerData.spawnTimes < 0)
                    {
                        PlayerData.spawnTimes = currentGameSetting.randomRange.GetRandom();
                        stars.Add(j);
                    }
                }

                yield return new BlockItem(des, colors[i], des.customParameter.boolValue, stars);
            }
        }

        private ListPoint GetRandomDescriptor(int currentScoreValue)
        {
            if (_remoteSpawnSetting != null)
                return _remoteSpawnSetting.GetRandomDes(currentScoreValue);
            return currentGameSetting.blockSpawnSetting.GetRandomDes(currentScoreValue);
        }

        public void FillPoints<T>(IList<T> points) where T : PointParameter
        {
            foreach (var point in points)
            {
                CreateTile(point);
            }

            void CreateTile(PointParameter point)
            {
                var tile = tiles.Rent(0);
                tile.sortOrder.Value = 0;
                var transform1 = tile.transform;
                transform1.SetParent(null);
                tile.SetPos(point);
                transform1.localScale = Vector3.one;
                if (point.value2 > 0)
                    tile.ActiveStar();
                var positionPoint =
                    new PointWithVector3(point.col, point.row, transform1.position);
                tile.SetColor(point.value);
                _viewMap.SetAtPoint(positionPoint, tile);
            }
        }

        private void CheckShowAdBreak()
        {
            if ((_secondsFromAdBreak - _secondsToAdBreak) % 15 == 0)
                UIDebugLog.Log(_secondsFromAdBreak + "/" + _secondsToAdBreak);
            if (_secondsFromAdBreak >= _secondsToAdBreak && Kernel.Resolve<AdsManager>().IsInterstitialAdsReady() &&
                !BoardBusy.Value)
            {
                if (ScreenRoot.dialogController.HasKey<DialogAdBreak>())
                {
                    ScreenRoot.dialogController.Resolve<DialogAdBreak>().Show();
                    _secondsFromAdBreak = 0;
                    _secondsToAdBreak = randomAdBreak.GetRandomBetween();
                }
                else
                {
                    Debug.LogError("duong : not contains DialogAdBreak");
                }
            }
        }

        protected override void HandlerPause()
        {
            base.HandlerPause();
            TurnIndicateBuyRotate(false);
        }

        protected override void ProductGameActionEventHandler(ProductAction product)
        {
            switch ((ProductGameAction) product.index)
            {
                case ProductGameAction.SpawnSpecial:
                    break;
                case ProductGameAction.Undo:
                    Undo();
                    break;
                case ProductGameAction.HintJigsaw:
                    jigsawBoard.HintJigsaw();
                    break;
                case ProductGameAction.UnlockJigsawLevel:
                    Debug.Log("Unlock jigsaw level");
                    PlayerData.days++;
                    break;
                case ProductGameAction.DestroyByHammer:
                    break;
                case ProductGameAction.Continue:
                    break;
                case ProductGameAction.Double:
                    break;
                case ProductGameAction.OutOfMove:
                    break;
                case ProductGameAction.DoubleIt:
                    break;
                case ProductGameAction.ClearWaveCoin:
                    if (PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Star].Value  >=
                        currentGameSetting.starBoxProgress[currentGameSetting.starBoxProgress.Length - 1])
                    {
                        PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Star].Value = 0;
                        starProgress.SetTo(0);
                        PlayerData.customPropertyList[(int) CustomPlayerDataProperty.BoxClaimed].Value = 0;
                        starProgress.SetAll(currentGameSetting.starBoxProgress,
                            PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Star].Value,
                            PlayerData.customPropertyList[(int) CustomPlayerDataProperty.BoxClaimed].Value - 1, true);
                        GameActionEvent.SaveBoard.OnNext();
                        GameActionEvent.SavePlayerData.OnNext();
                    }
                    else
                    {
                        PlayerData.customPropertyList[(int) CustomPlayerDataProperty.BoxClaimed].Value++;
                        GameActionEvent.SavePlayerData.OnNext();
                    }

                    break;
                case ProductGameAction.OpenBox:
                    if (ValidInput())
                    {
                        UnDrag();
                        var price = new Product(Quantity.GameAction, 1, (int) ProductGameAction.ClearWaveCoin, 0, 0);
                        var rewardTypeFlag = (int) (RewardTypeFlag.IsReward | RewardTypeFlag.ClaimMultipleWithAdsPopup | RewardTypeFlag.IsOpenBox);
                        var currencyLog = new TradeLog((int) LogItemType.Collecting, "star");
                        var trade = new Trade<Product,Product>(price,currentGameSetting.boxRewards[product.parameter],(int)LogPlacement.GamePlay,currencyLog,null);
                        var input = new ShowRewardInput(trade,rewardTypeFlag,1,product.parameter,2f);
                        input.logInterstitial = new SonatLogShowInterstitial()
                        {
                            placement = "classic_claim_progress",
                            level = PlayerData.playTimes,
                            mode = "classic"
                        };
                        //parameter = 1, // 0 = no tween box, 1 2 3 = tween box
                        // parameter2 = product.parameter, // index of box
                        RootView.rootView.screenRoot.ShowReward(input);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }



        [SerializeField] private IndexBindingScript[] navigations;

        public override void UpdateActionTime()
        {
            _secondsFromLastAction = 0;
            ClearGameNavigation();
        }

        private void ClearGameNavigation()
        {
            _currentNavigation = -1;
            navigations.OnChanged(_currentNavigation);
        }

        private void CheckGameNavigation()
        {
            if (_secondsFromLastAction > currentGameSetting.navigationTime && _currentNavigation < 0
                //  && Blocks.Count > currentGameSetting  .numberOfBlockToNavi
            )
                StartNavigation();
        }

        private void StartNavigation()
        {
            if (_currentNavigation < 0 && ValidInput())
            {
                _currentNavigation = UnityEngine.Random.Range(0, 3);
                navigations.OnChanged(_currentNavigation);
            }
        }


        // last 
        [SerializeField] private MyBoardSettingClass myBoardSettings;

        [Serializable]
        public class MyBoardSettingClass
        {
            public float complimentClampPosX = 3.4f;
        }

        public override void LogCustomInt(int value)
        {
            base.LogCustomInt(value);
            var x = new GameSave<BoardState>(typeof(GameSaveKey));
            Debug.Log(x);
            var temp = x;
            x = null;
            Debug.Log(temp);
            Debug.Log(x);
        }
    }
}