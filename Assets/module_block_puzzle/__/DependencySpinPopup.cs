using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UniRx;
using UnityEngine.UI;


namespace BlockPuzzle
{
    public enum SpinTab
    {
        Normal,
        Special
    }


    public class DependencySpinPopup : BasePopupDependency
    {
        [SerializeField] private RewardViewProduct[] rewardViews;
        [SerializeField] private Transform spinContainer;
        [SerializeField] private AnimationCurve spinCurve;
        [SerializeField] private float rotateAdjustment = 0.67f;
        [SerializeField] private float spinTime = 5f;
        [SerializeField] private float spinRound = 3f;
        [SerializeField] private ToggleScript[] onSpins;
        [SerializeField] private AdsItemView adsItemViewToClose;
        
        private float _mDeltaEuler;
        [MyButtonInt(nameof(Spin), nameof(Arrange), nameof(RefreshSpin))] [SerializeField]
        private int testSpin;

        public override void LogCustom()
        {
            base.LogCustom();
            var x = rewardViews[rewardViews.Length - 1].Product;
            for (var i = rewardViews.Length - 1; i >= 1; i--)
            {
                rewardViews[i].product = rewardViews[i - 1].Product;
            }

            rewardViews[0].product = x;
        }

        protected override void Register()
        {
            base.Register();
            _mDeltaEuler = 360f / rewardViews.Length;
            RefreshSpin();
            Arrange();
        }

        protected override void OnKernelLoaded()
        {
            base.OnKernelLoaded();
            SubjectController.ProductSystemActionEvent
                .Where(x => ((ProductSystemAction) x.index) == ProductSystemAction.Spin)
                .Subscribe(data =>
                {
                    Spin();
                });
        }

        public override void OnShow()
        {
            CheckRegister();
            onSpins.OnChanged(false);
            _showed = true;
        }

        private bool _showed;
        public override bool ReadyToShow()
        {
            adsItemViewToClose.CheckRegister();
            return !_showed && (DailyData.customPropertyList[(int)CustomDailyDataProperty.Spin].Value > 0 || adsItemViewToClose.GetState() == AdsItemState.WatchAds);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public void Spin()
        {
            StartCoroutine(IeSpin());
        }

        private IEnumerator IeSpin()
        {
            onSpins.OnChanged(true);
            ScreenRoot.TurnRayCast(false);
            float fromRotate = spinContainer.transform.localEulerAngles.z;
            float addRotate = spinRound * 360;
            float t = 0f;
            int rewardsIndex = Random.Range(0,rewardViews.Length);
            var destinationEuler = Random.Range
                                   (-_mDeltaEuler / 2 + rewardsIndex * _mDeltaEuler,
                                       _mDeltaEuler / 2 + rewardsIndex * _mDeltaEuler) - rotateAdjustment;
            if (destinationEuler < 0)
                destinationEuler = 360 + destinationEuler;

            addRotate += 360 - (destinationEuler - fromRotate);
            _lastRewardIndex = CalculateReward();
            while (t < spinTime)
            {
                t += Time.deltaTime;
                var angle = Mathf.Lerp(0f, addRotate, spinCurve.Evaluate(t / spinTime));
                spinContainer.localEulerAngles = new Vector3(0, 0, fromRotate - angle);

                int currentReward = CalculateReward();
                for (var i = 0; i < rewardViews.Length; i++)
                {
                    rewardViews[i].SetClaimed(i == currentReward);
                }

                if (_lastRewardIndex != currentReward)
                {
                    _lastRewardIndex = currentReward;
                    ((int) SoundEnum.lucky_wheel_needle_sound).PlaySound();
                }

                yield return new WaitForEndOfFrame();
            }
            
            for (var i = 0; i < rewardViews.Length; i++)
                rewardViews[i].SetClaimed(i == rewardsIndex);
            ((int) SoundEnum.AudioGetReward2).PlaySound();
            
            ScreenRoot.ShowReward(rewardViews[rewardsIndex].Product,new Product(Quantity.Spin,-1,0,0,0));
            ScreenRoot.TurnRayCast(true);
            onSpins.OnChanged(false);
            Kernel.Resolve<FireBaseController>().LogEvent("lucky_wheel_spinned");

//            if(adsItemViewToClose.GetState() != AdsItemState.WatchAds)
//            {
//                yield return new WaitForSeconds(1);
//                ScreenRoot.TurnRayCast(false);
//                ScreenRoot.popupManager.CloseCurrentPopupAsObservable().Subscribe(data =>
//                    {
//                        ScreenRoot.TurnRayCast(true);
//                    });
//            }
        }

        public void RefreshSpin()
        {
            for (var i = 0; i < rewardViews.Length; i++)
                rewardViews[i].SetData(rewardViews[i].Product);
        }

        public void Arrange()
        {
            for (var i = 0; i < rewardViews.Length; i++)
            {
          //      rewardViews[i].transform.localEulerAngles = new Vector3(0, 0, -i * (360f / rewardViews.Length));
            }
        }

        private int _lastRewardIndex;

        private int CalculateReward()
        {
            var angle = spinContainer.transform.localEulerAngles.z;
            var select =
                Mathf.FloorToInt((angle + 360f / rewardViews.Length / 2) / (360f / rewardViews.Length)) %
                rewardViews.Length;

            return select;
        }
    }
}