using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CampusCrates
{
    /// <summary>
    /// Thick, rounded candy outline plus a soft drop under it. Unity's Outline effect only
    /// copies the glyphs four times, which leaves gaps on thick strokes; eight copies stay round.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class TextStroke : BaseMeshEffect
    {
        public Color StrokeColor = new Color32(11, 79, 74, 255);
        public float Width = 3f;
        public Color ShadowColor = new Color(0f, 0f, 0f, .28f);
        public Vector2 ShadowOffset = new Vector2(0f, -4f);

        private static readonly List<UIVertex> Source = new List<UIVertex>();
        private static readonly List<UIVertex> Output = new List<UIVertex>();
        private static readonly Vector2[] Directions =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(.707f, .707f), new Vector2(-.707f, .707f), new Vector2(.707f, -.707f), new Vector2(-.707f, -.707f)
        };

        public void Set(Color stroke, float width, float shadowDrop = 4f, float shadowAlpha = .28f)
        {
            StrokeColor = stroke;
            Width = width;
            ShadowOffset = new Vector2(0f, -shadowDrop);
            ShadowColor = new Color(stroke.r * .5f, stroke.g * .5f, stroke.b * .5f, shadowAlpha);
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive())
                return;

            Source.Clear();
            Output.Clear();
            helper.GetUIVertexStream(Source);
            if (Source.Count == 0)
                return;

            if (ShadowColor.a > 0f)
                foreach (Vector2 direction in Directions)
                    Append(ShadowOffset + direction * Width, ShadowColor);
            foreach (Vector2 direction in Directions)
                Append(direction * Width, StrokeColor);
            Output.AddRange(Source);

            helper.Clear();
            helper.AddUIVertexTriangleStream(Output);
        }

        private static void Append(Vector2 offset, Color color)
        {
            for (int index = 0; index < Source.Count; index++)
            {
                UIVertex vertex = Source[index];
                vertex.position += (Vector3)offset;
                Color32 tint = color;
                tint.a = (byte)(tint.a * (vertex.color.a / 255f));
                vertex.color = tint;
                Output.Add(vertex);
            }
        }
    }
}
