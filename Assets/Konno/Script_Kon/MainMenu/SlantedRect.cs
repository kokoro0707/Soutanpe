using UnityEngine;
using UnityEngine.UI;

namespace PersonaMenuUI
{
    /// <summary>
    /// 平行四辺形(斜めカット)を描画するUI Graphic。
    /// スプライトを用意しなくても、上辺だけをX方向にずらしたクアッドを
    /// メッシュとして生成することで、ペルソナ風メニューの斜めハイライトや
    /// スラッシュ演出に使える形状を作る。
    ///
    /// 使い方:
    ///   1. UIのImageの代わりにこのコンポーネントをGameObjectに追加する
    ///      (CanvasRendererは自動で追加される)。
    ///   2. RectTransformのWidth/Heightで見た目のサイズを、
    ///      Skew Angleで傾きの強さを調整する。
    ///   3. Color欄で色を指定(グラデーションが欲しい場合はTextureに
    ///      横長のグラデーションテクスチャを指定する)。
    /// </summary>
    [AddComponentMenu("UI/Persona Menu/Slanted Rect")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class SlantedRect : MaskableGraphic
    {
        [Tooltip("傾ける角度(度)。正の値にすると上辺が右にずれる(例の「SKILL」ハイライトのような向き)。負の値で逆向きになる。")]
        [SerializeField]
        private float skewAngle = 18f;

        [Tooltip("塗りに使うテクスチャ。未設定の場合はColorのみの単色塗りになる。")]
        [SerializeField]
        private Texture texture;

        /// <summary>傾き角度(度)。実行時に変更すると即座に形状へ反映される。</summary>
        public float SkewAngle
        {
            get => skewAngle;
            set
            {
                skewAngle = value;
                SetVerticesDirty();
            }
        }

        /// <summary>塗り用テクスチャ。</summary>
        public Texture Texture
        {
            get => texture;
            set
            {
                texture = value;
                SetMaterialDirty();
            }
        }

        public override Texture mainTexture => texture == null ? s_WhiteTexture : texture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = GetPixelAdjustedRect();

            // 高さと角度から、上辺をどれだけ水平にずらすかを計算する。
            float offset = r.height * Mathf.Tan(Mathf.Deg2Rad * skewAngle);

            Vector2 bottomLeft = new Vector2(r.xMin, r.yMin);
            Vector2 topLeft = new Vector2(r.xMin + offset, r.yMax);
            Vector2 topRight = new Vector2(r.xMax + offset, r.yMax);
            Vector2 bottomRight = new Vector2(r.xMax, r.yMin);

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = bottomLeft;
            vertex.uv0 = new Vector2(0f, 0f);
            vh.AddVert(vertex);

            vertex.position = topLeft;
            vertex.uv0 = new Vector2(0f, 1f);
            vh.AddVert(vertex);

            vertex.position = topRight;
            vertex.uv0 = new Vector2(1f, 1f);
            vh.AddVert(vertex);

            vertex.position = bottomRight;
            vertex.uv0 = new Vector2(1f, 0f);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif
    }
}
