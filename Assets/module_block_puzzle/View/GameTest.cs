using System.Collections;
using System.Collections.Generic;
using BlockPuzzle;
using UnityEngine;

public class GameTest : BaseFrameworkView
{
    public int score;
    public int star;
    public bool newBest;

    [EditorDisplayName(
        new []{15,10,15,10}, 
        new []{nameof(PointParameter.col),nameof(PointParameter.row),nameof(PointParameter.value),nameof(PointParameter.value2)}, 
        new []{"Point","","Size","Test"}, 
        new []{30,30,30,30} )]
    public PointParameter pointParameter;
    
    [MyButtonInt(nameof(TestGameLose))] public int test;

    public void TestGameLose()
    {
        StartCoroutine(Lose());
    }

    IEnumerator Lose()
    {
        WaveData.currentScore.Value = score;
        PlayerData.bestScore.Value = WaveData.currentScore.Value - (newBest ? 1 : -1);
        PlayerData.customPropertyList[(int) CustomPlayerDataProperty.Star].Value  = star;
        yield return new WaitForSeconds(1f);
        SubjectController.GameActionEvent.OnNext(GameActionEvent.BoardLose);
    }
}