using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sonat;
using UnityEngine;
using UniRx;
using UnityEngine.Serialization;

namespace BlockPuzzle
{
// ReSharper disable Unity.InefficientPropertyAccess
    [Serializable]
    public class TimeSetting
    {
        public float delayDecorFinishTut = 0.07f;
        public float delayNextDcor = 0.07f;
        public float delayStartGame = 0.1f;
    }

    public abstract class CurrentGameView : BasePoolItemGameView<GameController, CurrentGameSetting>
    {
    }


    public static class BlockPuzzleShortcut
    {
        public static CurrentGameSetting currentGameSetting;
    }

    public partial class GameController : GameSaveController<BoardState, Level>
    {
        [SerializeField] private bool useRemoteConfig = true;
        [SerializeField] private CurrentGameSetting currentGameSetting;
        [SerializeField] private LevelDatabase levelDatabase;
        [SerializeField] private TimeSetting timeSetting;

        public bool[] explodeSoundCheck;
        public IntReactiveProperty rotateOn;

        [NonSerialized] public IntReactiveProperty RotateValid;
        [SerializeField] IntReactiveProperty _freeToRotate = new IntReactiveProperty();

        public override IntReactiveProperty GetRootViewProperty(string propertyName)
        {
            var getProperty = typeof(GameController).GetField(propertyName);
            return (IntReactiveProperty) getProperty.GetValue(this);
        }

        [SerializeField] private PlayerPrefRemoteArrayInt logLevelStart = new PlayerPrefRemoteArrayInt(
            "log_level_start_array", new[]
            {
                1, 5, 10, 15, 30
            });

        protected override ScriptableObject CurrentGameSetting => currentGameSetting;


        protected override void Register()
        {
            base.Register();
            BlockPuzzleShortcut.currentGameSetting = currentGameSetting;
            if (PlayerData.tutorialStep > 0)
            {
                DeleteGameSave();
                PlayerData.tutorialStep = 0;
            }

//            RotateValid = new IntReactiveProperty(rotateOn,PlayerData.GetCustomProperty((int)CustomPlayerDataProperty.Rotate));
            RotateValid = new IntReactiveProperty(new[]
                {rotateOn, PlayerData.GetCustomProperty((int) CustomPlayerDataProperty.Rotate), _freeToRotate});
        }

        protected override void Retry()
        {
            base.Retry();
            var logEvent2 = $"level_end";
            Kernel.Resolve<FireBaseController>().LogEvent(logEvent2, new LogParameter[]
            {
                new LogParameter("mode", "classic"),
                new LogParameter("type", "retry"),
                new LogParameter("level", PlayerData.playTimes.ToString()),
                new LogParameter("score", WaveData.currentScore.Value),
                new LogParameter("use_booster_count", DeletedCurrentGameSave[(int) GameSaveKey.UseBoosterCount]),
                new LogParameter("play_time", DeletedCurrentGameSave[(int) GameSaveKey.TimeSeconds]),
            });
            var timeSeconds = DeletedCurrentGameSave[(int) GameSaveKey.TimeSeconds];
            Debug.Log(timeSeconds);
        }

        public override void OnDailyDataCustomPropertyCreated(ref IntReactiveProperty[] customPropertyList)
        {
            base.OnDailyDataCustomPropertyCreated(ref customPropertyList);
            customPropertyList[(int) CustomDailyDataProperty.Spin].Value = 1;
        }

        [SerializeField] [MyButtonInt(nameof(TestAddStar))]
        private int testBoard;

        public void TestAddStar()
        {
            var fx = genericPoolItem.pools[0].RentWorld(null, transform.position);
            fx.StartStepAnimation();
        }

        protected override void OnKernelLoaded()
        {
            base.OnKernelLoaded();
        }

        protected override void RegisterCurrentGame()
        {
            BasePoolItemGameView<GameController, CurrentGameSetting>.Set(this, currentGameSetting);
        }

#if UNITY_EDITOR
        public override void TestGameOver(int score)
        {
            ChangeScore(score, SetDataType.SetThenTween);
            CheckBest();
            3f.Timer(GameOver);
        }
#endif


        protected override void HandleGlobalEffect(int value)
        {
            if ((GlobalEffectEnum) value == GlobalEffectEnum.None)
                return;

            switch ((GlobalEffectEnum) value)
            {
                case GlobalEffectEnum.None:
                    break;
                case GlobalEffectEnum.ShakeCamera:
                    break;
                case GlobalEffectEnum.StarHitBox:
                    PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Star].Value++;
                    starProgress.TweenTo(PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Star].Value);
//                    if (WaveData.coin >= currentGameSetting.starBoxProgress)
//                        1f.Timer(() =>
//                        {
//                            if (WaveData.coin >= currentGameSetting.starBoxProgress)
//                            {
//                                starProgress.SetTo(0);
//                                WaveData.coin = WaveData.coin % currentGameSetting.starBoxProgress;
//                                starProgress.TweenTo(WaveData.coin);
//                            }
//                        });

                    break;
                case GlobalEffectEnum.CoinHit:
                    ((int) SoundEnum.CoinHitPanelSound).PlaySound();
                    //    PlayerData.Claim(currentGameSetting.starRewards);
                    break;
            }
        }

        protected override void StartLevel(Level input)
        {
            base.StartLevel(input);
        }

        protected override Level GetLevel(int i)
        {
            return levelDatabase.GetLevel(0);
        }

        public override GameSave<BoardState> GetGameSaveFromInput(object input)
        {
            hint_save_slot_times.Value = 0;
            PlayerData.tutorialStep = -1;
            if (!(input is MapListPoint cast)) return null;
            CurrentLevel = new Level()
            {
                size = new Point(cast.map.customParameter.vector2.x, cast.map.customParameter.vector2.y)
            };

            //  cam.orthographicSize = cast.map.customParameter.vector3.z;
            InitGame();

            var gameSave = new GameSave<BoardState>(typeof(GameSaveKey))
            {
            };
            gameSave.AddState(new BoardState()
            {
                points = cast.map.points.Select(x => x.ToPointWithTwoValue(x.value, 0)).ToArray(),
            });
            return gameSave;
        }


        protected override void StartPlayerDataLevel()
        {
            base.StartPlayerDataLevel();
        }

        protected override void StartCurrentGameSave()
        {
            HandlerResume();
            Kernel.Resolve<AdsManager>().CheckShowBanner();
            Clear();
            ScreenRoot.playScreen.SetupStartLevel(PlayerData.currentLevel.Value);
//            TweenCoin(TextTweenType.Set, WaveData.coin);
//            TweenGem(TextTweenType.Set, PlayerData.gem.Value);
            timeSetting.delayStartGame.Timer(StartGame);

            void StartGame()
            {
                InitGame();
                var state = CurrentGameSave.GetLastState();
                WaveData.currentScore.Value = state.Score;

                //    PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Star].Value = 50;
                //    PlayerData.Save();
                starProgress.SetAll(currentGameSetting.starBoxProgress,
                    PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Star].Value,
                    PlayerData.customPropertyList[(int) CustomPlayerDataProperty.BoxClaimed].Value - 1,
                    true);


                TweenScore(TextTweenType.Set, WaveData.currentScore.Value);
                if (!InitTut())
                    LoadState(state);
//                Kernel.LogLevel(PlayerData.playTimes + 1);
                StartInput(0.1f);

                if (PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Star].Value >=
                    currentGameSetting.starBoxProgress[currentGameSetting.starBoxProgress.Length - 1])
                {
                    Observable.Interval(TimeSpan.FromSeconds(3))
                        .Where(x => ValidInput())
                        .Take(1)
                        .Subscribe(data =>
                        {
                            ProductGameActionEventHandler(new ProductAction()
                            {
                                index = (int) ProductGameAction.OpenBox,
                            });
                        })
                        .AddToGameDisposable();
                }
            }
        }


        protected override GameSave<BoardState> CreateGameSaveFromCurrentLevel()
        {
            var gameSave = new GameSave<BoardState>(typeof(GameSaveKey));
            gameSave[(int) GameSaveKey.StartBestScore] = PlayerData.bestScore.Value;
            gameSave.AddState(CreateStateFromLevel(CurrentLevel));
            SaveLevel();
            SaveBoard();
            return gameSave;
        }

        public override void UpdateUserProperty()
        {
            var block = (int) Mathf.Pow(2, PlayerData.newValue + 1);
            var firebase = Kernel.Resolve<FireBaseController>();
//            firebase.SetUserProperty("num_diamond", PlayerData.gem.Value.ToString());
//            firebase.SetUserProperty("num_crown", block.ToString());
//            firebase.SetUserProperty("rank_pos", Kernel.Resolve<PlayFabManager2>().Rank.ToString());
//            firebase.SetUserProperty("play_count", PlayerData.playTimes.ToString());
//            firebase.SetUserProperty("highest_score", PlayerData.bestScore.Value.ToString());
        }

        protected override int[] GetGameSaveDisplayPropertyRemap()
        {
            return new int[]
            {
//                (int) EnumGameSave.LargestValue,
//                (int) EnumGameSave.TargetValue
            };
        }

        protected override bool IsWin()
        {
            return false;
        }

        protected override void ClearMoves()
        {
        }

        public override void HandlerButton(int key)
        {
            switch ((GameRootButtonEnum) key)
            {
                case GameRootButtonEnum.OpenStarBox:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, null);
            }
        }


        public override BoardState CreateStateFromLevel(Level level)
        {
            return new BoardState
            {
                points = new PointParameter[0],
                spawns = null
            };
        }

        protected override Type[] LogTypes => new[]
        {
            typeof(ShopItemKey),
            typeof(AdsItemKey),
            typeof(QuestEnum),
            typeof(ProductGameAction),
            typeof(ProductSystemAction),
            typeof(SoundEnum),
        };

        public override bool IsOfferOkayToShow(int key)
        {
            switch ((ShopItemKey) key)
            {
                case ShopItemKey.Undo:
                    break;
                case ShopItemKey.NoAds:
                    break;
                case ShopItemKey.PackageGem1:
                    break;
                case ShopItemKey.PackageGem2:
                    break;
                case ShopItemKey.PackageGem3:
                    break;
                case ShopItemKey.PackageGem4:
                    break;
                case ShopItemKey.PackageGem5:
                    break;
                case ShopItemKey.PackageGem6:
                    break;
                case ShopItemKey.PackageGem7:
                    break;
                case ShopItemKey.UseHammer:
                    break;
                case ShopItemKey.UseSwap:
                    break;
                case ShopItemKey.DoubleBlock:
                    break;
                case ShopItemKey.OutOfMove:
                    break;
                case ShopItemKey.DoubleIt:
                    break;
                case ShopItemKey.BuyRotate1:
                    break;
                case ShopItemKey.StartBundle:
                    return PlayerData.tutorialStep < 0;
                case ShopItemKey.UnlockAllTheme:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, null);
            }

            return false;
        }


        protected override void DeleteGameSave()
        {
            base.DeleteGameSave();
        }

        protected override void HandlerGameOver()
        {
            Pause.Value = true;
//            if (WaveData.currentScore.Value > DeletedCurrentGameSave[(int) GameSaveKey.StartBestScore]
//                && WaveData.currentScore.Value > 100
//                && Kernel.IsInternetConnection() 
//                && Kernel.Resolve<PlayFabManager2>().Logged 
//                && Kernel.Resolve<PlayFabManager2>().AroundPeople != null
//                )
//            {
//                if (ScreenRoot.popupManager.ShowPopup<PopupNewBest>() == null)
//                    ScreenRoot.popupManager.ShowPopup<PopupGameOver>();
//            }
//            else
            ScreenRoot.popupManager.ShowPopup<PopupGameOver>();

            new SonatLogLevelEnd()
            {
                level = PlayerData.playTimes.ToString(),
                mode = "classic",
                use_booster_count = DeletedCurrentGameSave[(int) GameSaveKey.UseBoosterCount],
                play_time = DeletedCurrentGameSave[(int) GameSaveKey.TimeSeconds],
                success = false,
                score = WaveData.currentScore.Value,
                highest_score = DeletedCurrentGameSave[(int) GameSaveKey.StartBestScore],
                lose_cause = "full",
                is_first_play = false,
            }.Post();
        }


        protected override void OnSubscriptionActivate(int subscriptionKey)
        {
            switch ((SubscriptionKey) subscriptionKey)
            {
                case SubscriptionKey.Vip1:
                    Kernel.Resolve<AdsManager>().CheckNoAds();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(subscriptionKey), subscriptionKey, null);
            }
        }

        public override bool CustomNoAds()
        {
            return IsSubscriptionActive((int) SubscriptionKey.Vip1);
        }

        public override string GetLogName(Product product)
        {
            switch (product.quantity)
            {
                case Quantity.CustomProperty:
                    return ((CustomPlayerDataProperty) product.index) + "_" + product.amount;
                default:
                    return base.GetLogName(product);
            }
        }

        public override string GetQuantityName(Product product)
        {
            switch (product.quantity)
            {
                case Quantity.CustomProperty:
                    return ((CustomPlayerDataProperty) product.index).ToString().ToLower();
                case Quantity.None:
                    break;
                case Quantity.Gold:
                    return product.quantity.ToString().ToLower();
                case Quantity.Diamond:
                    return product.quantity.ToString().ToLower();
                case Quantity.Energy:
                    return product.quantity.ToString().ToLower();
                case Quantity.Spin:
                    return product.quantity.ToString().ToLower();
                case Quantity.Medal:
                    return product.quantity.ToString().ToLower();
                case Quantity.Item:
                    break;
                case Quantity.WatchAds:
                    break;
                case Quantity.IapBuying:
                    break;
                case Quantity.Subscription:
                    break;
                case Quantity.Achievement:
                    break;
                case Quantity.Quest:
                    break;
                case Quantity.Free:
                    break;
                case Quantity.SystemAction:
                    break;
                case Quantity.GameAction:
                    break;
                case Quantity.SystemGameAction:
                    break;
                case Quantity.TutorialSkip:
                    break;
                default:
                    return base.GetQuantityName(product);
            }

            return base.GetQuantityName(product);
        }

        public override bool CheckNavigation(int nav, bool skipScreenCheck = false)
        {
            switch ((NavigationEnum) nav)
            {
                case NavigationEnum.PlayJigsaw:
                {
                    if (PlayerData.navigationChecker.Contains(nav))
                        return false;
                    if (skipScreenCheck || (ScreenRoot.CurrentScreen is HomeScreen))
                    {
                        // condition pass : if ever play level progress or more than level 2 then not indicate anymore
                        if (PlayerData.levelProgress.CountValue != 0 || PlayerData.days > 2)
                        {
                            PlayerData.navigationChecker.Add(nav);
                            return false;
                        }

                        return PlayerData.playTimes >= 1;
                    }

                    break;
                }
            }

            return base.CheckNavigation(nav, skipScreenCheck);
        }

        public override bool CheckShowNavigationDialog()
        {
            if (ScreenRoot.CurrentScreen is HomeScreen)
            {
                if (!DailyData.customPropertyList[(int) CustomDailyDataProperty.CheckShowJigsaw].BoolValue)
                {
                    ScreenRoot.dialogController.Resolve<DialogNavigate>().ShowNavigate((int) NavigateDialogEnum.Jigsaw);
                    DailyData.customPropertyList[(int) CustomDailyDataProperty.CheckShowJigsaw].BoolValue = true;
                    return true;
                }
            }

            return false;
        }

//        protected override void LogSpendVirtualCurrency<T, T2>(Trade<T, T2> trade)
//        {
//            foreach (var price in trade.Prices)
//                if (price.quantity == Quantity.Diamond || price.quantity == Quantity.Gold)
//                    foreach (var product in trade.Products)
//                    {
//                        switch (product.quantity)
//                        {
//                            case Quantity.SystemAction:
////                                Kernel.Resolve<FireBaseController>().EventSpendVirtualCurrency(
////                                    $"{RootView.rootView.gameController.GetLogName(product.index, BuiltInEnumType.ProductSystemAction.ToString(), "SystemAction_")}",
////                                    price.amount, price.quantity.ToString().ToLower());
//                                break;
//                            case Quantity.GameAction:
////                                Kernel.Resolve<FireBaseController>().EventSpendVirtualCurrency(
////                                    $"{RootView.rootView.gameController.GetLogName(product.index, BuiltInEnumType.ProductGameAction.ToString(), "GameAction_")}",
////                                    price.amount, price.quantity.ToString().ToLower());
//                                break;
//                            default:
//                                if (price is ShopPrice)
//                                {
//                                    // is shop item;
//                                    new SonatLogSpendVirtualCurrency()
//                                    {
//                                        virtual_currency_name = price.quantity.ToString().ToLower(),
//                                        value = price.amount,
//                                        placement = ((LogPlacement) trade.Placement).ToString().ToLower(),
//                                        item_type = GetItemType(product),
//                                        item_id = GetItemId(product),
//                                    }.Post();
//                                }
//
////                                else
////                                    Kernel.Resolve<FireBaseController>().EventSpendVirtualCurrency(
////                                        RootView.rootView.gameController.GetLogName(product), price.amount,
////                                        price.quantity.ToString().ToLower());
//                                break;
//                        }
//                    }
//
//            string GetItemType(Product product)
//            {
//                if (product.quantity == Quantity.CustomProperty)
//                {
//                    var custom = (CustomPlayerDataProperty) product.index;
//                    if (custom == CustomPlayerDataProperty.Rotate)
//                        return "booster";
//                    return ((CustomPlayerDataProperty) product.index).ToString().ToLower();
//                }
//
//                return product.quantity.ToString().ToLower();
//            }
//
//            string GetItemId(Product product)
//            {
//                if (product.quantity == Quantity.CustomProperty)
//                {
//                    var custom = (CustomPlayerDataProperty) product.index;
//                    if (custom == CustomPlayerDataProperty.Rotate)
//                        return "rotate";
//                    return GetItemType(product) + "_" + ((CustomPlayerDataProperty) product.index).ToString().ToLower();
//                }
//
//                return product.quantity.ToString().ToLower();
//            }
//        }

        public override void CheckLogVirtualCurrency<T, T2>(Trade<T, T2> trade)
        {
            if (trade.EarnLog != null)
            {
                string virtual_currency_name = GetItemName(trade.Product);
                new SonatLogEarnVirtualCurrency()
                {
                    virtual_currency_name = virtual_currency_name.ToLower(),
                    value = trade.Product.amount,
                    placement = ((LogPlacement) trade.Placement).ToString().ToLower(),
                    item_type = ((LogItemType) trade.EarnLog.itemType).ToString().ToLower(),
                    item_id = trade.EarnLog.itemId
                }.Post();
            }
            
            if (trade.SpendLog != null)
            {
                string virtual_currency_name = GetItemName(trade.Price);
                new SonatLogSpendVirtualCurrency()
                {
                    virtual_currency_name = virtual_currency_name.ToLower(),
                    value = trade.Price.amount,
                    placement = ((LogPlacement) trade.Placement).ToString().ToLower(),
                    item_type = ((LogItemType) trade.SpendLog.itemType).ToString().ToLower(),
                    item_id = trade.SpendLog.itemId
                }.Post();
            }

            string GetItemName(Product product)
            {
                if (product.quantity == Quantity.CustomProperty)
                {
//                    var custom = (CustomPlayerDataProperty) product.index;
//                    if (custom == CustomPlayerDataProperty.Rotate)
//                        return "booster_rotate";
                    return ((CustomPlayerDataProperty) product.index).ToString().ToLower();
                }

                return product.quantity.ToString().ToLower();
            }
        }


        public void Test()
        {
            Debug.Log(1.56f);
        }
    }
}