using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UniRx;
using UnityEngine;
using Random = System.Random;


namespace BlockPuzzle
{
   

    public class BlockTileView : CurrentGameView
    {
        

        public BlockItemView ParentBlock { get; private set; }
        [SerializeField] private IndexBindingScript[] valueBindings;
        [SerializeField] private ToggleScript[] onActives;
        [SerializeField] private ToggleScript[] onEnables;
        [SerializeField] public BaseRxAnimator onDrag;
        [SerializeField] private IndexBindingSpriteChange blockSpriteChange;
        
        [SerializeField] public IntReactiveProperty sortOrder; // 0 = on Spawn, 1 = Drag, 2 onBoard
        [SerializeField] private IndexBindingScript[] sortOrderBindings;
       
        [SerializeField] private SpriteRenderer spriteRenderer;

        public bool IsActive { get; private set; }

        public Point Point { get; private set; }
        public int DelayToDestroy { get; set; }
        private Material _dfMat;
        [SerializeField] BoolReactiveProperty star = new BoolReactiveProperty();
        [SerializeField] private ToggleScript[] starBindings;
        [SerializeField] private ToggleScript[] onSelects;

        public bool GetStar() => star.Value;
        protected override void Register()
        {
            base.Register();
            star.Subscribe(starBindings.OnChanged);
            sortOrder.Subscribe(data =>
            {
                sortOrderBindings.OnChanged(data);
            });

//        spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));
            if (spriteRenderer != null)
                _dfMat = spriteRenderer.sharedMaterial;
        }
        public void OnSelect(bool select,int sortOder)
        {
            onSelects.OnChanged(select);
            sortOrder.Value = select ? 1 : sortOder;
            SetSprite(!select);
            if (select)
                transform.rotation = Quaternion.identity;
        }

        public void SetSprite(bool onSpawn)
        {
            blockSpriteChange.SetIndex2(onSpawn ? 1 : 0);
        }
        
        public override void OnRent()
        {
            base.OnRent();
            IsActive = true;
            if (onDrag != null)
                onDrag.SetAtIn();

            _starActive = false;
            DeactiveStar();
            foreach (var toggleScript in starBindings)
                toggleScript.transform.rotation = Quaternion.identity;

            transform.rotation = Quaternion.identity;
            if (spriteRenderer != null)
                spriteRenderer.sharedMaterial = _dfMat;
            transform.rotation = Quaternion.identity;
            onSelects.OnChanged(false);
        }

        [SerializeField] private bool destroyWithSetIndex;
        public override void DestroyView()
        {
            IsActive = false;
            Observable.Timer(TimeSpan.FromSeconds(DelayToDestroy * CurrentGameSetting.delayDestroy +
                                                  CurrentGameSetting.itemPlaceTween))
                .Subscribe(data =>
                {
                    try
                    {
                        base.DestroyView();
                        if (star.Value)
                        {
                            var fx = GameController.genericPoolItem.pools[0].RentWorld(null, transform.position);
                            fx.StartStepAnimation();
                        }

                        if (DelayToDestroy < GameController.explodeSoundCheck.Length &&
                            !GameController.explodeSoundCheck[DelayToDestroy])
                        {
                            GameController.explodeSoundCheck[DelayToDestroy] = true;
                            ((int) SoundEnum.BombExplode).PlaySound();
                        }

//                        if (destroyWithSetIndex)
                            GameController.blockEffects.pools[0].Rent(null, transform.position).SetIndex2(CurrentColor);
//                        else
//                            GameController.blockEffects.pools[CurrentColor].Rent(null, transform.position)
//                                .SetIndex2(CurrentColor);
                       
                    }
                    catch (Exception e)
                    {
                        
                        Kernel.Resolve<FireBaseController>().LogCrashException(e);
                        if(GameController.explodeSoundCheck == null)
                            Kernel.Resolve<FireBaseController>().LogEvent("explodeSoundCheck null");
                    }
                });
        }

        public override void Return()
        {
            base.Return();
            ParentBlock = null;
            SetActive(false);
            SetEnableBinding(true);
            transform.rotation = Quaternion.identity;
            transform.SetParent(null);
        }

        public void SetEnableBinding(bool value)
        {
            foreach (var baseToggleBindingAction in onEnables)
                baseToggleBindingAction.OnChanged(value);
        }

        public void SetParentBlock(BlockItemView parent, int color)
        {
            ParentBlock = parent;
            SetColor(color);
        }

        public void ClearParent()
        {
            ParentBlock = null;
        }

        public int Color { get; private set; }
        public int CurrentColor { get; private set; }

        public void SetColor(int color, bool overwrite = true)
        {
            if (overwrite)
                Color = color;
            foreach (var binding in valueBindings)
                binding.OnChanged(color);
            CurrentColor = color;
        }

        public void SetColor()
        {
            foreach (var binding in valueBindings)
                binding.OnChanged(Color);
        }

        public void SetActive(bool active)
        {
            foreach (var binding in onActives)
                binding.OnChanged(active);
        }

        [SerializeField] private ProgressScript grayOutScript;

        [MyButton(nameof(Jump),nameof(TestStar))] public bool testJump;
        public void Jump()
        {
//            spriteRenderer.material = currentGameSetting.grayMaterial;
            SetEnableBinding(false);
            star.Value = false;
            grayOutScript.OnProgress100(0);
            UnityEngine.Random.Range(0f, 1f).Timer(() =>
            {
                DOTween.To(() => 0, x => { grayOutScript.OnProgress100(x); }, 100, 1);
            });
            
        }

        public void TestStar()
        {
            var fx = GameController.genericPoolItem.pools[0].RentWorld(null, transform.position);
            fx.StartStepAnimation();
        }
        
        public void SetPos(Point point)
        {
            SetPos(point, false);
        }
        
        public void SetPos(Point point, bool tween)
        {
            Point = point;
            if (!tween)
                transform.localPosition =  Point.GetGamePos();
            else
            {
                ReActiveStar();
                transform.DOLocalMove(  Point.GetGamePos(), CurrentGameSetting.itemPlaceTween);
            }
        }

        public void DeactiveStar()
        {
            star.Value = false;
        }
        
        public void ReActiveStar()
        {
            star.Value = _starActive;
            foreach (var toggleScript in starBindings)
                toggleScript.transform.rotation = Quaternion.identity;
        }

        private bool _starActive;
        public void ActiveStar()
        {
            star.Value = true;
            _starActive = true;
            foreach (var toggleScript in starBindings)
                toggleScript.transform.rotation = Quaternion.identity;
        }
    }
}