using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UniRx;
using UnityEngine;

namespace BlockPuzzle
{
    public class JigsawTile : CurrentGameView
    {
        

        private static readonly int Jigaw_MaskTex123 = Shader.PropertyToID("Jigaw_MaskTex123");
        private static readonly int Vector2_Tiling = Shader.PropertyToID("Vector2_547d7ee6001446a5898668c08ea2190d");
        private static readonly int Vector2_Offset = Shader.PropertyToID("Vector2_1ebbb4c2f435444498fd9a6e34c8e0e1");
        [SerializeField] private Material maskMaterial;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Texture2D maskTexture;
        public Vector2Int size = new Vector2Int(10, 10);
        public PointParameter maskPoint;

        [SerializeField] private Sprite[] sprites;
        [SerializeField] private ToggleScript[] onDrags;
        [SerializeField] private ToggleScript[] onBoards;
        [SerializeField] private Collider2D hitCollider;
        public BoolReactiveProperty star = new BoolReactiveProperty();
        [SerializeField] private ToggleScript[] starBindings;

        protected override void Register()
        {
            base.Register();
            star.Subscribe(starBindings.OnChanged);
        }

        public void SetOnDrag(bool onDrag)
        {
            onDrags.OnChanged(onDrag);
        }

        public void SetOnBoard(bool on)
        {
            onBoards.OnChanged(on);
            hitCollider.enabled = !on;
        }


        public override void Return()
        {
            base.Return();
            onDrags.OnChanged(false);
        }

        public Point Point { get; set; }
        public JigsawItem ParentBlock { get; set; }

        public void SetTile(Texture2D tex, Vector2Int sizeTex, PointParameter point, int blockIndex)
        {
            maskTexture = tex;
            size = sizeTex;
            maskPoint = point;
            UpdateTile();
            spriteRenderer.sprite = sprites[blockIndex % sprites.Length];
        }

        private void UpdateTile()
        {
            if ((maskPoint.value & 1 ) == 0)
            {
                spriteRenderer.material = maskMaterial;
                spriteRenderer.material.SetVector(Vector2_Tiling, new Vector4(1f / size.x, 1f / size.y));
                spriteRenderer.material.SetVector(Vector2_Offset,
                    new Vector4(maskPoint.col * 1f / size.x, maskPoint.row * 1f / size.y));
                spriteRenderer.material.SetTexture(Jigaw_MaskTex123, maskTexture);
            }
            else
            {
                spriteRenderer.sharedMaterial = defaultMaterial;
            }
        }

        public override void LogCustom()
        {
            base.LogCustom();
            UpdateTile();
        }


        public void SetPos(Point point)
        {
            SetPos(point, false);
        }

        public void SetPos(Point point, bool tween)
        {
            Point = point;
            if (!tween)
                transform.localPosition = Point.GetGamePos()+GameController.JigsawBoard.PositionAdjust;
            else
                transform.DOLocalMove(Point.GetGamePos()+GameController.JigsawBoard.PositionAdjust, CurrentGameSetting.itemPlaceTween);
        }
    }
}