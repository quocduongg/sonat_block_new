using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace BlockPuzzle
{
    public class JigsawSelector : DependencyLevelSelector
    {
        [SerializeField] private JigsawBoard jigsawBoard;
        [SerializeField] private bool setAvatar = true;

        public override void Initialize()
        {
            base.Initialize();
            SubjectController.ProductGameActionEvent.Where(x => gameObject.activeInHierarchy).Subscribe(data => OnUnlock());
        }

        private void OnUnlock()
        {
//            0.25f.Timer(() =>
//            {
                if (PlayerData.days == 31)
                    PlayerData.days = 1;
                scrollRect.InvokeItemRentAll();
//            });
            scrollRect.InvokeItemRentAll();
        }

        public override void OnShow()
        {
            base.OnShow();
            scrollRect.IndicateIndex(Mathf.Min(PlayerData.days-1,30));
        }

        protected override void HandlerClick(LevelSelect select)
        {
            if ((select.State & LevelSelectState.Unlocked) != LevelSelectState.None)
            {
                ScreenRoot.Show<PlayScreen2>(true);
                jigsawBoard.StartLevel(select.Level);
            }
        }
    }
}