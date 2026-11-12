using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CampusCrates
{
    // Shared look: candy outlined headlines, cream sticker panels and pills that match the
    // glossy button artwork. Every builder goes through these helpers so the style stays one system.
    public sealed partial class PrototypeBoardView
    {
        private static readonly Color32 TextWhite = new Color32(255, 253, 244, 255);
        private static readonly Color32 StrokeTeal = new Color32(11, 79, 74, 255);
        private static readonly Color32 StrokeBrown = new Color32(168, 86, 18, 255);
        private static readonly Color32 StrokeCoral = new Color32(158, 52, 32, 255);
        private static readonly Color32 CoinBrown = new Color32(122, 64, 14, 255);

        private readonly Dictionary<string, Sprite> uiSprites = new Dictionary<string, Sprite>();
        private Font titleFont;

        private enum Surface { Panel, Pill, PillTeal, PillGold, BarTrack, BarFill }

        private Sprite UiSprite(string name, Vector4 border)
        {
            if (uiSprites.TryGetValue(name, out Sprite cached))
                return cached;
            var texture = Resources.Load<Texture2D>("CampusCrates/Theme/UI/" + name);
            Sprite sprite = null;
            if (texture != null)
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f),
                    100f, 0, SpriteMeshType.FullRect, border);
                generatedArt.Add(sprite);
            }
            uiSprites[name] = sprite;
            return sprite;
        }

        private Sprite SurfaceSprite(Surface surface) => surface switch
        {
            Surface.Panel => UiSprite("panel", new Vector4(62, 66, 62, 60)),
            Surface.Pill => UiSprite("pill", new Vector4(50, 54, 50, 50)),
            Surface.PillTeal => UiSprite("pill-teal", new Vector4(50, 54, 50, 50)),
            Surface.PillGold => UiSprite("pill-gold", new Vector4(50, 54, 50, 50)),
            Surface.BarTrack => UiSprite("bar-track", new Vector4(28, 30, 28, 30)),
            _ => UiSprite("bar-fill", new Vector4(28, 30, 28, 30))
        };

        /// <summary>Cream sticker card or pill; falls back to the flat rounded panel without art.</summary>
        private Image Sticker(Transform parent, string name, Rect bounds, Surface surface = Surface.Pill)
        {
            Sprite sprite = SurfaceSprite(surface);
            if (sprite == null)
                return Panel(parent, name, bounds, surface == Surface.PillTeal ? (Color)DarkGlass : BoardSurface, true);

            Image image = Panel(parent, name, bounds, Color.white);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            var fit = image.gameObject.AddComponent<SlicedFit>();
            bool card = surface == Surface.Panel;
            fit.HeightFraction = card ? .5f : .98f;
            fit.MaxBorder = card ? 84f : 9999f;
            return image;
        }

        private Image IconImage(Transform parent, string name, string sprite, Rect bounds)
        {
            Sprite art = UiSprite(sprite, Vector4.zero);
            Image image = Panel(parent, name, bounds, Color.white);
            image.sprite = art;
            image.preserveAspect = true;
            if (art == null)
                image.color = Color.clear;
            return image;
        }

        private Text Headline(Transform parent, string name, string value, Rect bounds, int size, Color fill,
            Color stroke, float width = 3.5f, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            Text label = Label(parent, name, value, bounds, size, fill, alignment);
            label.font = titleFont;
            if (width > 0f)
                Stroke(label, stroke, width);
            return label;
        }

        private static TextStroke Stroke(Graphic graphic, Color stroke, float width, float drop = 4f)
        {
            var shadow = graphic.GetComponent<Shadow>();
            if (shadow != null)
                Destroy(shadow);
            var effect = graphic.GetComponent<TextStroke>();
            if (effect == null)
                effect = graphic.gameObject.AddComponent<TextStroke>();
            effect.Set(stroke, width, drop);
            return effect;
        }

        /// <summary>White candy label on a glossy pill button, outlined in the button's own rim color.</summary>
        private Text ButtonText(Button button, string value, Rect bounds, int size, bool gold)
            => Headline(button.transform, "Text", value, bounds, size, TextWhite, gold ? StrokeBrown : StrokeTeal,
                size >= 44 ? 4f : 3f);

        /// <summary>Ribbon banner with an outlined title, used for screen and dialog headers.</summary>
        private Text Ribbon(Transform parent, string name, string value, Rect bounds, int size)
        {
            Image ribbon = IconImage(parent, name, "ribbon", bounds);
            ribbon.preserveAspect = false;
            return Headline(ribbon.transform, "Title", value, At(.15f, .30f, .85f, .84f), size, TextWhite, StrokeCoral, 3.5f);
        }

        /// <summary>Cream pill showing a coin and an amount.</summary>
        private Text CoinPill(Transform parent, string name, Rect bounds, int size)
        {
            Image pill = Sticker(parent, name, bounds);
            IconImage(pill.transform, "Coin", "coin", At(-.06f, -.08f, .30f, 1.08f));
            Text amount = Label(pill.transform, "Amount", "0", At(.29f, .14f, .90f, .86f), size, CoinBrown);
            amount.font = titleFont;
            return amount;
        }
    }
}
