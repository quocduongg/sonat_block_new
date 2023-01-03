using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle
{
    public class ProgressBarStarGameOver : CurrentGameView
    {
        [SerializeField] private TweenDisplayScript starProgress;
        [SerializeField] private TMP_Text tmpText;

        [SerializeField] private float delayTweenProgress = 2;

        // Start is called before the first frame update
        void OnEnable()
        {
            starProgress.SetTargets(CurrentGameSetting.starBoxProgress);
            starProgress.SetTo(0);
            tmpText.text = $"{0}/{CurrentGameSetting.starBoxProgress}";
            delayTweenProgress.Timer(() =>
            {
                starProgress.TweenToWithAction(PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Star].Value,
                    t =>
                    {
                        tmpText.text =
                            $"{Mathf.Min(t, CurrentGameSetting.starBoxProgress[CurrentGameSetting.starBoxProgress.Length-1])}/{CurrentGameSetting.starBoxProgress}";
                    });
            });
        }
    }
}