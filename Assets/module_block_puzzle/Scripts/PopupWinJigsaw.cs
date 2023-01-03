using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle
{
    public class PopupWinJigsaw : BasePopup
    {
        [SerializeField] private IndexBindingScript[] bindsLevel;
        public void SetLevel(int level)
        {
            bindsLevel.OnChanged(level);
        }
    }
}