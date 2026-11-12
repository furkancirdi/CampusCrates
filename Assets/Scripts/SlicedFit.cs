using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CampusCrates
{
    /// <summary>
    /// Keeps a 9-slice sprite's rounded ends proportional to the element's height, so the same
    /// pill art works for a small tag and a tall card without stretched or squashed corners.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class SlicedFit : UIBehaviour
    {
        public float HeightFraction = 1f;
        public float MaxBorder = 9999f;

        private Image image;

        protected override void OnEnable()
        {
            base.OnEnable();
            Apply();
        }

        protected override void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (image == null)
                image = GetComponent<Image>();
            if (image == null || image.sprite == null)
                return;

            float height = ((RectTransform)transform).rect.height;
            Vector4 border = image.sprite.border;
            float borderPixels = border.y + border.w;
            if (height <= 1f || borderPixels <= 0f)
                return;

            float wanted = Mathf.Min(height * HeightFraction, MaxBorder);
            image.pixelsPerUnitMultiplier = Mathf.Max(.05f, borderPixels / wanted);
        }
    }
}
