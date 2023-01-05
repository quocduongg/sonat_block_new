using System;

namespace BlockPuzzle
{
    public enum NavigationEnum
    {
        None = 0,
        PlayJigsaw = 1,
    }

    public enum GameCheckerEnum
    {
        PlayGameMode1,
        PlayGameMode2,
        PlayGameMode3,
    }

    public enum NavigateDialogEnum
    {
        Jigsaw = 1,
        BoxPreview = 2,
        NoAds = 3,
    }

    public enum TutorialActionEnum
    {
        WaitForDrag = -2,
        FinishedTutorial = -1,
        DragBlock = 0,
        ClickButton = 1,
        RotateItem = 2,
        DragToSaveSlot = 3,
    }
    
    public enum TutorialActionParam
    {
        Normal = 0,
        FromSaveSlot = 1,
    }

    [Flags]
    public enum TutorialEnableFlag
    {
        None =  (1 << 0),
        Lock = (1 << 1),
        Rotate = (1 << 2),
        Jigsaw = (1 << 3),
    }


   

    public enum CustomPlayerDataProperty
    {
        Rotate = 0,
        BoxClaimed = 1,
        Star = 2,
        LoseTimes = 3,
        WinTimes = 4,
    }

    public enum CustomDailyDataProperty
    {
        Spin,
        CheckShowJigsaw,
    }

    public enum CustomSessionDataProperty
    {
        ShopTab = 0,
        Upgrade = 1,
    }

//    public enum CustomWeeklyDataProperty
//    {
//        NONE = 0,
//    }
//
//    

//
//
//    public enum CustomWaveDataProperty
//    {
//        NONE = 0,
//    }


    public enum GameSaveKey : int
    {
        StartBestScore,
        UseBoosterCount,
        NewRecord,
        TimeSeconds,
        Moves,
//        BoxClaimed,
    }

    public enum GameRootButtonEnum : int
    {
        OpenStarBox,
    }

    public enum SoundEnum
    {
        None = 0,
        ButtonClick = 1,
        PopupShow = 2,
        PopupHide = 3,
        GetReward = 4,

        PopupShow2 = 5,
        PopupHide2 = 6,

        Cool = 10,
        Great = 11,
        Perfect = 12,

        BoardFinish = 102,
        WinPopupSound = 103,
        Firework = 104,

        ItemPickup = 106,
        ItemPlace = 107,
        ItemFail = 108,
        BlockRotate = 109,
        BombExplode = 110,
        GameOver = 111,
        NewRecord = 112,
        ScoreSound = 113,
        ItemReborn = 114,
        lucky_wheel_needle_sound = 115,
        AudioGetReward2 = 116,
        CoinHitPanelSound = 117,
        LineEliminate = 118,
        UI_Move = 119,
        PlayerRecordPlaced = 120,
        BoardStart = 121,
        WinJigsawPopupSound = 122,
        OpenChest = 123,
        StarSound = 124,
    }

    public enum GlobalError
    {
        PlayFabNameTaken = 0
    }

    public enum TextUpdateKey
    {
        Tutorial,
    }

    public enum ProductGameAction
    {
        SpawnSpecial = 0,
        Undo = 1,
        HintJigsaw = 2,
        DestroyByHammer = 3,
        Continue = 4,
        Double = 5,
        OutOfMove = 6,
        DoubleIt = 7,
        ClearWaveCoin = 8,
        UnlockJigsawLevel = 9,
        OpenBox = 10,
    }

    public enum ItemTypeEnum
    {
        Rainbow,
        Bomb
    }

    public enum EffectEnum
    {
        CoinHit = 0,
        WhenFiringFx = 1,
        BombEffect = 2,
        Combo = 3,
        BlockSwapFx = 4,
        ComboX = 5,
        MaxBlock = 6,
        Click = 7,
        BlockRemoved = 8,
        BlockDestroyByHammer = 9,
        BlockDestroyByOutOfMove = 10,
//    Explode2 = 1,
//    Explode3 = 2,
//    Hit = 4,
    }

    public enum PoolItemEnum
    {
        Star
    }

    public enum GlobalEffectEnum
    {
        None = 0,
        ShakeCamera = 1,
        StarHitBox = 2,
        CoinHit = 3,
        RotateSub = 4,
    }

    public enum LogPlacement
    {
        Undefined = 0,
        Shop = 1,
        Spin = 2,
        DailyReward = 3,
        Achievement = 4,
        Quest = 5,
        CompleteLevel = 6,
        Subscription = 7,
        Home = 8,
        GamePlay = 9,
    }
    
    public enum LogItemType
    {
        Undefined = 0,
        Ads = 1,
        DailyReward = 2,
        Achievement = 3,
        Quest = 4,
        Spin = 5,
        Shop = 6,
        CompleteLevel = 7,
        Subscription = 8,
        BuyIap = 9,
        
        Collecting = 100
    }

    [Flags]
    public enum RewardTypeFlag
    {
        // Decimal     // Binary
        ClaimQuietly = 0, 
        ClaimOnPopup = 1,
        ClaimMultipleAdsPopup = 2, 
        ClaimOnCurrencyPopup = 4,
        IsReward = 8,
        IsOpenBox = 16
    }

    public enum ParameterGameActionEnum
    {
        SetToggleRotate
    }


    public enum BlockEnum
    {
        Normal = 0,
        Rainbow = 1,

//    Jump = 3,
        Bomb = 2
    }

    public enum ShopItemKey
    {
        NoAds = 0,
        PackageGem1 = 1,
        PackageGem2 = 2,
        PackageGem3 = 3,
        PackageGem4 = 4,
        PackageGem5 = 5,
        PackageGem6 = 6,
        PackageGem7 = 7,
        VipMember = 10,
        PackageVip2 = 11,
        BuyRotate1 = 20,
        BuyRotate2 = 21,
        BuyRotate3 = 22,
        BuyRotate4 = 23,
        BuyRotate1_h = 25,
        BuyRotate2_h = 26,
        BuyRotate3_h = 27,
        BuyRotate4_h = 28,
        UseHammer = 30,
        UseSwap = 31,
        Undo = 32,
        DoubleBlock = 33,
        OutOfMove = 34,
        DoubleIt = 35,

        StartBundle = 37,
        UnlockAllTheme = 101,
    }

    public enum RewardEnum
    {
        ClassicBox = 0,
        JigsawProgress = 1,
        JigsawProgress2 = 3,
    }

    public enum CustomGameCondition
    {
        None = 0,
        Undo = 1,
    }

    public enum CurrentRemoteSettingKey
    {
        remote_spawn_setting,
    }

    public enum MiniPopEnum
    {
        Best = 0,
        Notify = 1,
        Goal = 2,
    }


    public enum AdsItemKey
    {
        DoubleRewardGameOver = 0,
        DailyGift = 1,
        ShopAdsGem = 2,
        NewBlockGem = 3,
        PlayGameGem = 4,
        StartFromAds = 5,
        NextGoalAds = 6,
        Spin = 7,
        BoxOpenX2 = 8,
        ShopAdsRotate = 9,
        HintJigsaw = 10,
        UnlockJigsawLevel = 11,
    }

    public enum SubscriptionKey
    {
        Vip1,
        Vip2,
    }

    public enum ToggleLink
    {
        None = 0,
        BoosterDialog = 1,
    }

    public enum BlockDestroyBy
    {
        None = 0,
        Hammer = 1,
        RemoveBlock = 2,
        OutOfMove = 3,
    }


    public enum QuestEnum
    {
        WatchAds = 0,
        SpendGold = 1,
        SpendGem = 2,
        CompleteQuest = 3,
        MergeBlock = 10,
        CreateBlock2048 = 11,
        UseBooster = 12,
        UseHammer = 13,
        UseSwap = 14,
        FinishLevel = 15,
        Test = 100,
    }
}