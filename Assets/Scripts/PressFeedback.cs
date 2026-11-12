using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CampusCrates
{
    /// <summary>Springy squash on press so every button feels tactile.</summary>
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressedScale = .92f;
        private Selectable selectable;
        private bool pressed;
        private float scale = 1f;
        private float velocity;

        private void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable == null || selectable.IsInteractable())
                pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pressed)
                return;
            pressed = false;
            velocity += 1.2f;
        }

        public void OnPointerExit(PointerEventData eventData) => pressed = false;

        private void OnDisable()
        {
            pressed = false;
            scale = 1f;
            velocity = 0f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float target = pressed ? PressedScale : 1f;
            if (!pressed && Mathf.Abs(scale - 1f) < .0005f && Mathf.Abs(velocity) < .002f)
            {
                if (scale != 1f)
                {
                    scale = 1f;
                    velocity = 0f;
                    transform.localScale = Vector3.one;
                }
                return;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            velocity += (target - scale) * 520f * dt;
            velocity *= Mathf.Exp(-19f * dt);
            scale += velocity * dt;
            transform.localScale = Vector3.one * scale;
        }
    }
}
