using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle
{
    public class PopupCalendarSelect : BasePopup
    {
        [SerializeField] private JigsawBoard jigsawBoard;
        [SerializeField] protected PoolCollection<LevelSelect> selects;
        [SerializeField] private Transform container;
        [SerializeField] private bool setAvatar = true;

        private LevelSelect[] _levels;
        protected override void Register()
        {
            base.Register();
            selects.Init(container,-1);
            selects.ViewFirst(0,container);
            var collection = BlockPuzzleShortcut.currentGameSetting.JigsawCollection;
            _levels = new LevelSelect[collection.levels.Count];
            for (var i = 0; i < collection.levels.Count; i++)
            {
                var rent = selects.pools[0].RentLocalZero();
                rent.onClick += HandlerClick;
                rent.SetLevel(i);
                _levels[i] = rent;
            }
        }
        
        private void HandlerClick(LevelSelect select)
        {
            Debug.Log("select level "+select.Level);
            if ((select.State & LevelSelectState.Unlocked) != LevelSelectState.None)
            {
                ScreenRoot.Show<PlayScreen2>(true);
                jigsawBoard.StartLevel(select.Level);
            }
        }

        public override void OnShowHandler(WhenPopupClose whenPopupClosed)
        {
            base.OnShowHandler(whenPopupClosed);
            CheckInitilize();
            for (var i = 0; i < _levels.Length; i++)
                _levels[i].Setup(GetState(i));

            LevelSelectState GetState(int level)
            {
                var state = LevelSelectState.None;
                if (level < PlayerData.days)
                {
                    state |= LevelSelectState.Unlocked;
                    bool b = PlayerData.levelProgress[level] > 0;
                    if(b)
                        state |= LevelSelectState.Passed;
                    if (level == PlayerData.days -1)
                        state |= LevelSelectState.Current;
                }
                else
                    state |= LevelSelectState.Locked;

                return state;
            }
        }
    }
}