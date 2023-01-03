using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle
{
   public class BoardSaverLoader : MonoBehaviour
   {
      public GameController gameController;
      public MapScriptable map1;
      public MapScriptable map2;


      [MyButtonInt(nameof(StartMap1), nameof(StartMap2))]
      public int test;
      public void StartMap1()
      {
         StartMap1(map1);
      }
      
      
      public void StartMap2()
      {
         StartMap1(map2);
      }

      private void StartMap1(MapScriptable map)
      {
         gameController.LoadState(FromMap(map));
         gameController.SpawnItemFromTut(map.items);
      }
      
      
      public BoardState FromMap(MapScriptable map)
      {
         var state = new BoardState();
         state.points = map.map.map.points.ToArray();
         
         return state;
      }
   }
}