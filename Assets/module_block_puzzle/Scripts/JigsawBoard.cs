using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sonat;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle
{
    public class JigsawBoard : CurrentGameView , ILogParameterValueRetriever
    {
        
        [SerializeField] private SpriteRenderer boardFill;
        [SerializeField] private SpriteRenderer board;
        [SerializeField] private PoolCollection<JigsawTile> tiles;
        [SerializeField] private PoolCollection<JigsawItem> items;
        [SerializeField] private float distanceX = 2;
        [SerializeField] private float clampX = -4;
        [SerializeField] private Transform container;
        [SerializeField] private ToggleScript[] onGameFinishes;
        [SerializeField] private float delayPopupWin;
        [SerializeField] private IndexBindingScript[] levelBindings;
        [SerializeField] private Button btnQuit;
        [SerializeField] private Button btnRetry;
        [SerializeField] private Vector3 positionAdjust;
        [SerializeField] private Texture2D[] texture2Ds;
        [SerializeField] private Sprite[] bgs;
        [SerializeField] private Sprite[] bgFills;

        public Texture2D[] mapTextures => texture2Ds;

        public Vector3 PositionAdjust
        {
            get => positionAdjust;
        }

        protected override void Register()
        {
            base.Register();
            board.transform.localPosition = positionAdjust;
            items.Init(null, 0);
            tiles.Init(null, 0);
            SubjectController.ScreenClosed.Where(x => x is PlayScreen2).Subscribe(data =>
            {
                IsPlaying = false;
                StopInput();
                gameObject.SetActive(false);
            });

            if (btnQuit != null)
                btnQuit.onClick.AddListener(() =>
                {
                    Pause.Value = false;
                    Time.timeScale = 1;
                    Kernel.Resolve<AdsManager>().ShowInterstitial("jigsaw_to_home".CreateDefaultLogInterstitial(), false, false);
//                    ScreenRoot.popupManager.CloseAllPopUpInstant();
                    ScreenRoot.Show<HomeScreen>(true);
                    Clear();
                });

            btnRetry.onClick.AddListener(() => { Retry(); });

            SubjectController.ScreenClosed.Where(x => x != null && x is PlayScreen2).Subscribe(data => Clear());
        }

        public void Clear()
        {
            items.ReturnAll();
            tiles.ReturnAll();
        }

        [MyButtonInt(nameof(StartLevel))] [SerializeField]
        private int testLevel;

        private Camera cam;

        private int _levelIndex;

        public void Retry()
        {
            StartLevel(_levelIndex);
        }

        [SerializeField] private PlayerPrefRemoteArrayInt logLevelStart = new PlayerPrefRemoteArrayInt(
            "log_level_start_array_jigsaw", new[]
            {
                1, 5, 10, 15, 30
            });

        public void StartLevel(int i)
        {
            _levelIndex = i;
            levelBindings.OnChanged(i);
            CheckRegister();
            gameObject.SetActive(true);
            onGameFinishes.OnChanged(false);
            var level = CurrentGameSetting.JigsawCollection.levels[i];
            var tex = texture2Ds[i];
            board.sprite = bgs[i];
            boardFill.sprite = bgFills[i];
            MyGameSetting.Setup(PointType.Square, level.size.x, level.size.y,
                CurrentGameSetting.tileDistance, CurrentGameSetting.tileDistance);
            items.ReturnAll();
            tiles.ReturnAll();
            0f.Timer(() =>
            {
                for (var i1 = 0; i1 < level.parts2.Count; i1++)
                {
                    var rent = items.pools[0].Rent(container, Vector3.zero);
                    rent.SetPool(tiles.pools[0]);
                    rent.SetTile(tex, level.size, level.parts2[i1]);
                    rent.IndexInSpawn = i1;
                    rent.SetSpawnPosition(container, distanceX);
//                rent.SetPool(tiles.pools[0]);
                }
            });
            cam = RootView.rootView.gameController.cam;
            InitGame(level);
            StartInput(0);
            JigsawPlayTimes++;
            _boosterCount = 0;
            var levels = logLevelStart.Value.ToArray();
            foreach (var l in levels)
            {
                if (i+1 == l)
                {
                    var logEvent = $"jigsaw_start_level_{i+1:D4}";
                    Debug.Log(logEvent);
                    Kernel.Resolve<FireBaseController>().LogEvent(logEvent);
                    Kernel.Resolve<AppFlyerController>().SendEvent(logEvent);
                    break;
                }
            }

            new SonatLogLevelStart()
            {
                level = i.ToString(),
                mode = "jigsaw",
                setUserProperty = false
            }.Post();
        }


        private JigsawTile[,] _bgViewMap;
        private JigsawTile[,] _viewMap;
        private bool[,] _uncheckMap;

        private List<PointWithVector3> Positions { get; set; }

        private JigsawLevel _level;
        private int _step;

        private int JigsawPlayTimes
        {
            get => PlayerPrefs.GetInt("jigsaw_play_times", 0);
            set => PlayerPrefs.SetInt("jigsaw_play_times", value);
        }

        private float _playSeconds;
        private int _boosterCount;

        private void InitGame(JigsawLevel level)
        {
            _step = 0;
            _level = level;
            Positions = new List<PointWithVector3>();
            _bgViewMap = new JigsawTile[level.size.x, level.size.x];
            _viewMap = new JigsawTile[level.size.x, level.size.x];
            _uncheckMap = new bool[level.size.x, level.size.x];

            _uncheckMap.Populate(true);
            foreach (var listPoint in level.parts2)
                for (var i = 0; i < listPoint.Count; i++)
                    _uncheckMap.SetAtPoint(listPoint.points[i], false);

            for (int i = 0; i < level.size.x; i++)
            for (int j = 0; j < level.size.y; j++)
            {
                var point = new Point(i, j);
                CreateBgTile(point);
            }

            container.localPosition = new Vector3(clampX, 0, 0);

            void CreateBgTile(Point point)
            {
                var bgTileView = tiles.Rent(1);
                bgTileView.transform.SetParent(null);
                bgTileView.SetPos(point);
                var transform1 = bgTileView.transform;
                transform1.localScale = Vector3.one;
                var positionPoint =
                    new PointWithVector3(point.col, point.row, transform1.position);
                Positions.Add(positionPoint);
                _bgViewMap.SetAtPoint(point, bgTileView);
            }

            UpdateViewMapAndSpawnIndex();
        }

        private void OnDisable()
        {
            IsPlaying = false;
            StopInput();
        }

        private IDisposable _inputDisposable;
        public bool IsPlaying { get; set; }
        public void StartInput(float delay)
        {
            if (delay > 0)
                delay.Timer(Input);
            else
                Input();

            void Input()
            {
                IsPlaying = true;
                _inputDisposable?.Dispose();
                _inputDisposable = InputHandle().AddToGameDisposable();
            }
        }

        private float t = 0f;
        public void StopInput()
        {
            t = 0;
            _inputDisposable?.Dispose();
        }
        
        [SerializeField] protected BaseFrameworkViewCheck[] continuouslyChecks;
        private IDisposable InputHandle()
        {
            return this.UpdateAsObservable()
                .Where(c => ValidInput())
                .Subscribe(_ =>
                {
                    t += Time.deltaTime;
                    if (t > 1)
                    {
                        foreach (var baseFrameworkViewCheck in continuouslyChecks)
                            baseFrameworkViewCheck.Check();
                        t = 0;
                    }
                    OnInputUpdate();
                }).AddToGameDisposable();
        }

//        private bool _itemClicked;
//        private bool _startDragging;
        private Vector2 _clickOffset;
        private Vector2 _mouseDown;
        private Point _lastPoint;
        private readonly ReactiveProperty<JigsawItem> _draggingItem = new ReactiveProperty<JigsawItem>();
        private Vector3 _oldPos;
        [SerializeField] private Vector2 minDragDistance;
        [SerializeField] private Vector2 dragOffset = new Vector2(0, 1);
        [SerializeField] private float dragSpeed = 0.5f;
        [SerializeField] private float pointSize = 1;
        [SerializeField] private float returnSpeed = 1;


        public enum DragState
        {
            None,
            Clicked,
            DragItem,
            MoveBoard,
        }

        [SerializeField] [DisplayAsDisable] private DragState dragState;

        private Vector2 _startBoard;
        private float _clampXMax;

        private void CalMaxMoveBoard()
        {
            _clampXMax = (items.pools[0].ActiveItems.Count(x => x.PlacedPoint == null) - 1) * distanceX;
        }

        private Vector2 _mouseDownPos;

        private void OnInputUpdate()
        {
            _playSeconds += Time.deltaTime;
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 origin = cam.ScreenToWorldPoint(Input.mousePosition);
                var hit = DuongExtensions.RayCast<JigsawItem>(origin);
                if (hit == null)
                {
                    var tile = DuongExtensions.RayCast<JigsawTile>(origin);
                    if (tile != null)
                        hit = tile.ParentBlock;
                }

                if (hit != null && !hit.BusyMoving) // in boardArranger tile must deactive collider
                {
                    _mouseDown = cam.ScreenToWorldPoint(Input.mousePosition);
                    _draggingItem.Value = hit;
                    dragState = DragState.Clicked;
                    _clickOffset = _draggingItem.Value.transform.position - (Vector3) _mouseDown;
                    _clickOffset.x = 0;
                }
                else if (DuongExtensions.RayCast<JigsawDragHit>(origin) != null)
                {
                    CalMaxMoveBoard();
                    _mouseDown = cam.ScreenToWorldPoint(Input.mousePosition);
                    if (items.pools[0].ActiveItems.Count(x => x.PlacedPoint == null) > 3)
                    {
                        dragState = DragState.MoveBoard;
                        _startBoard = container.transform.localPosition;
                    }
                }
            }


            if (Input.GetMouseButton(0))
            {
                if (dragState == DragState.Clicked)
                {
                    var distance = (Vector2) cam.ScreenToWorldPoint(Input.mousePosition) - _mouseDown;
                    if (Mathf.Abs(distance.y) >= minDragDistance.y)
                    {
                        dragState = DragState.DragItem;
                        _lastPoint = null;
                        _oldPos = _draggingItem.Value.transform.localPosition;
                        _mouseDownPos = cam.ScreenToWorldPoint(Input.mousePosition);
                        _clickOffset = (Vector2) _draggingItem.Value.transform.position - _mouseDownPos;
                        _clickOffset.x = 0;
                        if (_clickOffset.y > 0)
                            _clickOffset.y = 0;
                        _draggingItem.Value.blockTweenSelectScript.SetAddLocalPos(-_clickOffset);
                        _draggingItem.Value.OnSelect(true, false);
                        _draggingItem.Value.blockTweenSelectScript.OnChanged(true, false);
                        ((int) SoundEnum.ItemPickup).PlaySound();
                    }

                    if (dragState == DragState.Clicked && Mathf.Abs(distance.x) >= minDragDistance.x
                                                       && items.pools[0].ActiveItems.Count(x => x.PlacedPoint == null) >
                                                       3)
                    {
                        dragState = DragState.MoveBoard;
                        _startBoard = container.transform.localPosition;
                        UpdateViewMapAndSpawnIndex();
                    }
                }

                if (dragState == DragState.MoveBoard)
                {
                    var mouseDown2 = _mouseDown - (Vector2) cam.ScreenToWorldPoint(Input.mousePosition);
                    container.localPosition =
                        new Vector3(Mathf.Clamp((_startBoard - mouseDown2).x, -(_clampXMax + clampX), clampX), 0, 0);
                    //container.localPosition = new Vector3(Mathf.Clamp(startBoard.x, -(clampXMax + clampX), clampX), 0, 0);
                }

                if (dragState == DragState.DragItem)
                {
                    var newPos = (Vector2) cam.ScreenToWorldPoint(Input.mousePosition) - _mouseDownPos;
                    _draggingItem.Value.dragTransform.localPosition = newPos;
                    if (Vector2.Distance(_oldPos, newPos) > 0.05f)
                    {
                        _oldPos = newPos;
                        var placePoint = GetDragPoint(_draggingItem.Value.RootTransform);
                        if (placePoint != _lastPoint)
                        {
                            _lastPoint = placePoint;
                            UIDebugLog.Log("change to" + (_lastPoint != null ? _lastPoint.toString() : "null"));
                        }
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (dragState == DragState.DragItem)
                {
                    var placePoint = GetDragPoint(_draggingItem.Value.RootTransform);
                    // if (IsPlaceValid(placePoint, _draggingItem.Data))
                    if (IsPlaceValidDirty(placePoint, _draggingItem.Value.Data))
                    {
                        PlaceAndDisplaceOverlapping(placePoint, _draggingItem.Value.Data);
                        _draggingItem.Value.PlacedPoint = placePoint;
                        _draggingItem.Value.SetBoardPosition(_step);
                        _step++;
                        UpdateViewMapAndSpawnIndex();
                        CheckWin();
                        ((int) SoundEnum.ItemPlace).PlaySound();
                    }
                    else
                    {
                        ((int) SoundEnum.ItemFail).PlaySound();

                        _draggingItem.Value.ReturnToRightPlace(returnSpeed).Subscribe(data =>
                        {
                            _draggingItem.Value = null;
                        });
                        ;
                        _draggingItem.Value.OnSelect(false, true);
                        UpdateViewMapAndSpawnIndex();
                    }

                    dragState = DragState.None;
                }
            }
        }

        public override void LogCustom()
        {
            base.LogCustom();
            Back();
        }

        public void Back()
        {
            JigsawItem last = null;
            foreach (var activeItem in items.pools[0].ActiveItems)
                if (activeItem.PlacedPoint != null && (last == null || last.Step < activeItem.Step))
                    last = activeItem;

            if (last != null)
            {
                last.SetSpawnPosition(container, distanceX);
                UpdateViewMapAndSpawnIndex();
            }
        }


        public void UpdateViewMapAndSpawnIndex()
        {
            int index = 0;
            for (var i = 0; i < items.pools[0].ActiveItems.Count; i++)
            {
                if (items.pools[0].ActiveItems[i].PlacedPoint == null)
                {
                    items.pools[0].ActiveItems[i].IndexInSpawn = index;
                    items.pools[0].ActiveItems[i].SetSpawnPosition(container, distanceX);
                    index++;
                }
            }

            _viewMap.Populate(null);
            for (var i = 0; i < items.pools[0].ActiveItems.Count; i++)
            {
                if (items.pools[0].ActiveItems[i].PlacedPoint != null)
                {
                    var item = items.pools[0].ActiveItems[i];
                    for (var j = 0; j < item.Data.Count; j++)
                    {
                        _viewMap.SetAtPoint(item.Data.points[j] - item.Data.points[0] + item.PlacedPoint, item.Tiles[j]);
                    }
                }
            }

            CalMaxMoveBoard();
            var localPos = container.localPosition;
            var min = -(_clampXMax + clampX);
            var max = clampX;
            if (min > max)
                min = max;
            localPos.x = Mathf.Clamp(localPos.x, min, max);
            container.localPosition = localPos;
        }

        public void CheckWin(bool force = false)
        {
            bool win = true;
            foreach (var activeItem in items.pools[0].ActiveItems)
            {
                if (!(activeItem.PlacedPoint != null && activeItem.PlacedPoint == activeItem.Data.points[0]))
                {
                    win = false;
                }
            }

            win = win | force;
            if (win)
            {
                Debug.LogError("win");
                if (_levelIndex + 1 == PlayerData.days)
                    DailyData.gameChecker[(int) GameCheckerEnum.PlayGameMode1] = true;
                onGameFinishes.OnChanged(true);
                dragState = DragState.None;
                StopInput();
                if (PlayerData.levelProgress[_levelIndex] == 0)
                {
                    PlayerData.levelProgress[_levelIndex] = 1;
                    PlayerData.achievementData[(int) QuestEnum.FinishLevel] += 1;
                }

                delayPopupWin.Timer(() =>
                {
                    ScreenRoot.popupManager.ShowPopup<PopupWinJigsaw>().SetLevel(_levelIndex);
                });

                new SonatLogLevelEnd()
                {
                    level = _levelIndex.ToString(),
                    mode = "jigsaw",
                    use_booster_count = _boosterCount,
                    play_time = (int)_playSeconds,
                    success = true,
                    score = 0,
                    highest_score = 0,
                    is_first_play = false,
                }.Post();
                _playSeconds = 0;
            }
        }

        public void HintJigsaw()
        {
            _boosterCount++;
            
            new SonatLogUseBooster()
            {
                level =_levelIndex.ToString(),
                mode = "jigsaw",
                name = "hint"
            }.Post();
            
          
            for (var i = 0; i < items.pools[0].ActiveItems.Count; i++)
            {
                var itemView = items.pools[0].ActiveItems[i];
                if (itemView.PlacedPoint == null)
                {
                    _draggingItem.Value = itemView;
                    PlaceAndDisplaceOverlapping(_draggingItem.Value.Data.points[0], _draggingItem.Value.Data);
                    _draggingItem.Value.PlacedPoint = _draggingItem.Value.Data.points[0];
                    _draggingItem.Value.SetBoardPosition(_step);
                    _step++;
                    UpdateViewMapAndSpawnIndex();
                    CheckWin();
                    break;
                }
            }
        }


        private bool IsPlaceValid(Point placePoint, ListPoint item)
        {
            if (placePoint == null)
                return false;
            if (_viewMap.OutOfRange(placePoint) || _viewMap.GetAtPoint(placePoint))
                return false;
            if (_uncheckMap.GetAtPoint(placePoint))
                return false;
            for (var i = 1; i < item.Count; i++)
            {
                var relative = placePoint + item.points[i] - item.points[0];
                if (_viewMap.OutOfRange(relative) || _viewMap.GetAtPoint(relative))
                    return false;
                if (_uncheckMap.GetAtPoint(relative))
                    return false;
            }

            return true;
        }

        private bool IsPlaceValidDirty(Point placePoint, ListPoint item)
        {
            if (placePoint == null)
                return false;
            if (_uncheckMap.OutOfRange(placePoint) || _uncheckMap.GetAtPoint(placePoint))
                return false;
            for (var i = 0; i < item.Count; i++)
            {
                var relative = placePoint + item.points[i] - item.points[0];
                if (_uncheckMap.OutOfRange(relative) || _uncheckMap.GetAtPoint(relative))
                    return false;
            }

            return true;
        }

        public bool ignore1;
        public bool ignore2;

        private void PlaceAndDisplaceOverlapping(Point placePoint, ListPoint item)
        {
            if (ignore1)
                return;
            for (var i = 0; i < item.Count; i++)
            {
                if (ignore2)
                    continue;
                var relative = placePoint + item.points[i] - item.points[0];

                if (!_uncheckMap.OutOfRange(relative) && _viewMap.GetAtPoint(relative) != null)
                {
                    _viewMap.GetAtPoint(relative).ParentBlock.SetSpawnPosition(container, distanceX);
                    UpdateViewMapAndSpawnIndex();
                }
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

        public virtual bool ValidInput()
        {
            return !Pause.Value
                   && !RootView.rootView.pauseMoving
                   && !ScreenRoot.popupManager.AnyPopupShow
                   && !ScreenRoot.dialogController.IsBusy();
        }

        public int Level => _levelIndex+1;
        public string Mode => "jigsaw";
    }
}