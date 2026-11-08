using System;

namespace CampusCrates
{
    public static class PortraitViewport
    {
        private const float MaxPortraitAspect = 0.68f;

        public static (float Left, float Bottom, float Right, float Top) Anchors(
            int width, int height, float safeX, float safeY, float safeWidth, float safeHeight)
        {
            if (width <= 0 || height <= 0)
                return (0f, 0f, 1f, 1f);

            float left = Math.Max(0, safeX);
            float right = Math.Min(width, safeX + safeWidth);
            float bottom = Math.Max(0, safeY);
            float top = Math.Min(height, safeY + safeHeight);
            if (right <= left || top <= bottom)
            {
                left = bottom = 0;
                right = width;
                top = height;
            }

            float contentWidth = Math.Min(right - left, (top - bottom) * MaxPortraitAspect);
            float contentLeft = (left + right - contentWidth) * .5f;
            return (contentLeft / width, bottom / height, (contentLeft + contentWidth) / width, top / height);
        }
    }
}
