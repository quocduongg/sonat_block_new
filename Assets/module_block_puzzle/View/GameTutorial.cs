using System;
using System.Collections;
using System.Collections.Generic;
using Sonat;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlockPuzzle
{
    public class GameTutorial : BaseGameTutorial<GameController>
    {
        [SerializeField] private TutorialStep[] tutorialSteps;
        [SerializeField] private IndexBindingScript[] sortOrderByTut;
        [SerializeField] private RxSequenceAnimator2 tutHandAnimator;
        [SerializeField] private MaskPoint maskPoint;
        [SerializeField] private ToggleScript bindingActiveOnTutorial;
        [SerializeField] private ToggleScript onHandEnable;
        [SerializeField] private ToggleScript[] skipables;
        public TutorialStep CurrentTutStep { get; private set; }

        protected override BaseTutorialStep CurrentBaseStep => CurrentTutStep;


        public bool IsInTutorial()
        {
            return PlayerData.tutorialStep >= 0 && PlayerData.tutorialStep < tutorialSteps.Length;
        }

        /// <summary>
        /// to get center of area of all points
        /// </summary>
        private List<Vector3> _lastTutVector3S;

        public TutorialStep GetWaitStep(int waitIndex)
        {
            foreach (var tutorialStep in tutorialSteps)
            {
                if (tutorialStep.waitIndex == waitIndex)
                    return tutorialStep;
            }

            return null;
        }


        public override bool InitTut(bool isNextTut)
        {
            var baseInit = base.InitTut(isNextTut);
            GameController.ClearIdleHint();
            // stop tutorial when it last tutorial
            if (PlayerData.tutorialStep == tutorialSteps.Length)
                return InternalStopTut(true);

            if (PlayerData.tutorialStep == 0)
            {
                new SonatLogTutorialBegin()
                {
                    placement = "start_game",
                }.Post();
            }
            // set current tut
            if (PlayerData.tutorialStep >= 0 && PlayerData.tutorialStep < tutorialSteps.Length)
                CurrentTutStep = tutorialSteps[PlayerData.tutorialStep];
            else
                CurrentTutStep = null;

            // stop tutorial if data from current tutorial set stopWhenNext, it means tutorial will pause then active when a particular condition meets
            if (CurrentTutStep != null && isNextTut && CurrentTutStep.stopWhenNext)
                return InternalStopTut();

            if (CurrentTutStep != null)
            {
                skipables.OnChanged(CurrentTutStep.skipable);
                // update current tutorial text
                SubjectController.TextUpdater.OnNext(new TextUpdateData((int) TextUpdateKey.Tutorial,
                    CurrentTutStep.messages));
//                if (PlayerData.tutorialStep == 0)
//                    gameController.rotateOn.BoolValue = false;
                ScreenRoot.TurnRayCast(!CurrentTutStep.turnOffRayCast);
                sortOrderByTut.OnChanged(CurrentTutStep.sortOrderIndex);

                foreach (var parameterGameAction in CurrentTutStep.actionOnStart)
                    GameController.Handler(parameterGameAction);

                // skip next tutorial because this step doesn't need an action to finish
                if (CurrentTutStep.actionOnly)
                {
                    GameController.NextTut(CurrentTutStep.nextTutAdd, CurrentTutStep.actionBeforeNext);
                    return false;
                }

                RootView.rootView.onTutorial.Value = true;
                if (CurrentTutStep.map.customParameter.boolValue)
                {
                    GameController.ViewMap.ReturnAll();
                    GameController.FillPoints(CurrentTutStep.map.points);
                }

                if (CurrentTutStep.Spawn)
                    GameController.SpawnItemFromTut(CurrentTutStep.Items);
                
                            
                _handDisposable?.Dispose();
                _handDisposable = Observable.FromCoroutine(IeStartHand).Subscribe().AddToGameDisposable();
                return true;
            }


            // ???
            bool InternalStopTut(bool force = false)
            {
                if (CurrentTutStep != null && !force)
                {
                    var indexStop = CurrentTutStep.indexStopTut;
                    CurrentTutStep = null;
                    if (isNextTut)
                        GameController.StopTut(indexStop);
                    else
                    {
                        GameController.StopTut(-1);
                        new SonatLogTutorialComplete()
                        {
                            placement = "start_game",
                        }.Post();
                    }
                }
                else
                {
                    GameController.StopTut(-1);
                    new SonatLogTutorialComplete()
                    {
                        placement = "start_game",
                    }.Post();
                }

                StopTut();
                return false;
            }


            return base.InitTut(isNextTut);
        }

        private IDisposable _handDisposable;
        private IDisposable _handDisposable2;

        public void StartHandToSaveSlot(bool start)
        {
            tutHandAnimator.ResetAnim();
            tutHandAnimator.TurnOff();
            bindingActiveOnTutorial.OnChanged(false);
            onHandEnable.OnChanged(false);
            _handDisposable2?.Dispose();
            if (start)
            {
                _handDisposable2 = Observable.FromCoroutine(IeStartHandToSaveSlot).Subscribe();
            }
        }

        private IEnumerator IeStartHandToSaveSlot()
        {
            bindingActiveOnTutorial.OnChanged(true);
            onHandEnable.OnChanged(true);
            tutHandAnimator.ResetAnim();
            tutHandAnimator.TurnOff();
            
            maskPoint.SetData(new Point(0, 0), new Point(2, 2));
            maskPoint.Set();
            for (int i = 0; i < 3; i++)
                if (GameController.spawnItems[i].gameObject.activeSelf)
                {
                    maskPoint.transform.position =
                        GameController.spawnItems[i].transform.position -
                        new Vector3(Point.PointSize, Point.PointSize);
                    break;
                }
            
            while (!gameObject.activeInHierarchy)
                yield return null;
            
            Debug.Log("aa");
            yield return new WaitForSeconds(0.5f);
            Debug.Log("bb");

            for (int i = 0; i < 3; i++)
                if (GameController.spawnItems[i].gameObject.activeSelf)
                {
                    tutHandAnimator.Inject(new[]
                        {
                            GameController.spawnItems[i].transform.position,
                            GameController.spawnItems[3].transform.position
                        })
                        .Animator_In2(0);
                    break;
                }
        }

        private IEnumerator IeStartHand()
        {
            bindingActiveOnTutorial.OnChanged(true);
            onHandEnable.OnChanged(true);
            tutHandAnimator.ResetAnim();
            tutHandAnimator.TurnOff();

            //prepare
            switch ((TutorialActionEnum) CurrentTutStep.actionEnum)
            {
                case BlockPuzzle.TutorialActionEnum.DragBlock:
                    maskPoint.SetData(CurrentTutStep.hintMask.Point1, CurrentTutStep.hintMask.Point2);
                    _lastTutVector3S = new List<Vector3>();
                    for (int j = 0; j < maskPoint.MapSize.col; j++)
                    for (int k = 0; k < maskPoint.MapSize.row; k++)
                    {
                        var pos = GameController.BgPoints[maskPoint.Point.col + j, maskPoint.Point.row + k].value;
                        _lastTutVector3S.Add(pos);
                    }

                    maskPoint.Set();
                    foreach (var hintPlacePoint in CurrentTutStep.hintPlacePoints.points)
                        GameController.BgTilePoolIdleHint.RentWorld(null, hintPlacePoint.GetGamePos());
                    break;
                case BlockPuzzle.TutorialActionEnum.ClickButton:
                    maskPoint.transform.localScale = Vector3.zero;
                    0.1f.Timer(() => // delay for ui elements anchor first
                    {
                        maskPoint.SetData(new Point(0, 0), new Point(4, 2));
                        maskPoint.Set();
                        var transform1 = maskPoint.transform;
                        transform1.position =
                            tutHandAnimator.Targets[0].position - new Vector3(Point.PointSize * 2, Point.PointSize)
                            + new Vector3(0.7f - 0.1f, 0.35f);
                        transform1.localScale = transform1.localScale * 0.7f;
                    });
                    break;
                case BlockPuzzle.TutorialActionEnum.RotateItem:
                    maskPoint.SetData(new Point(0, 0), new Point(2, 2));
                    maskPoint.Set();
                    maskPoint.transform.position =
                        tutHandAnimator.Targets[1].position - new Vector3(Point.PointSize, Point.PointSize);
                    break;
                case BlockPuzzle.TutorialActionEnum.DragToSaveSlot:
                    maskPoint.SetData(new Point(0, 0), new Point(2, 2));
                    maskPoint.Set();
                    for (int i = 0; i < 3; i++)
                        if (GameController.spawnItems[i].gameObject.activeSelf)
                        {
                            maskPoint.transform.position =
                                GameController.spawnItems[i].transform.position -
                                new Vector3(Point.PointSize, Point.PointSize);
                            break;
                        }

                    // target 2 = save slot
                    break;
                case BlockPuzzle.TutorialActionEnum.WaitForDrag:
                    break;
                case BlockPuzzle.TutorialActionEnum.FinishedTutorial:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            while (!gameObject.activeInHierarchy)
                yield return null;

            var step = CurrentTutStep;
            CheckActiveOnTutorialStep();
            yield return new WaitForSeconds(0.5f);
            
            tutHandAnimator.gameObject.SetActive(true);
            switch ((TutorialActionEnum) step.actionEnum)
            {
                case BlockPuzzle.TutorialActionEnum.DragBlock:
                    tutHandAnimator.StopAnim();
                    tutHandAnimator.Inject(new[]
                        {
                            GameController.spawnItems[step.DragItemIndex].RootTransform.position,
                            GameController.BgPoints.GetAtPoint(step.PlacePoint).value
                        })
                        .Animator_In2(0);
                    break;
                case BlockPuzzle.TutorialActionEnum.ClickButton:
                    tutHandAnimator.StopAnim();
                    tutHandAnimator.Animator_In2(1);
                    this.UpdateAsObservable().Where(x => GameController.rotateOn.BoolValue).Take(1)
                        .TakeUntilDisable(this)
                        .Subscribe(data =>
                        {
                            if (CurrentTutStep != null)
                                GameController.NextTut(CurrentTutStep.nextTutAdd, CurrentTutStep.actionBeforeNext);
                        });
                    break;
                case BlockPuzzle.TutorialActionEnum.RotateItem:
                    tutHandAnimator.Animator_In2(2);
                    break;
                case BlockPuzzle.TutorialActionEnum.DragToSaveSlot:
                    for (int i = 0; i < 3; i++)
                        if (GameController.spawnItems[i].gameObject.activeSelf)
                        {
                            tutHandAnimator.Inject(new[]
                                {
                                    GameController.spawnItems[i].transform.position,
                                    GameController.spawnItems[3].transform.position
                                })
                                .Animator_In2(0);
                            break;
                        }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private bool _last;

        public void TurnOffDisplay(bool b, bool force = false)
        {
            if (_last != b || force)
            {
                onHandEnable.OnChanged(b);
                tutHandAnimator.gameObject.SetActive(b);
                if (b)
                    tutHandAnimator.Animator_In();
                _last = b;
            }
        }

        public void CheckToTurnDragToSaveSlot()
        {
            for (var i = 0; i < tutorialSteps.Length; i++)
                if (tutorialSteps[i].waitIndex == PlayerData.tutorialStep)
                {
                    RootView.rootView.gameController.ForceInitTut(i);
                    break;
                }
        }

        [IndexAsEnum(BuiltInEnumType.EffectEnum)] [SerializeField]
        private int[] returnWhenStop;

        public override void StopTut()
        {
            GameController.ClearIdleHint();
            bindingActiveOnTutorial.OnChanged(false);
           // GameController.Spawn(null, true);
            CurrentTutStep = null;
            TurnOffDisplay(false, true);
            CheckActiveOnTutorialStep();

            foreach (var i in returnWhenStop)
                PoolManager.effects[i].ReturnAll();
        }

        public void StopHand()
        {
            tutHandAnimator.ResetAnim();
            tutHandAnimator.TurnOff();
            foreach (var i in returnWhenStop)
                PoolManager.effects[i].ReturnAll();
        }

       
    }
}