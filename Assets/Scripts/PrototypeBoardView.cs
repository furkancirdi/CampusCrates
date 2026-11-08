using System.Collections.Generic;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DeviceScreen = UnityEngine.Device.Screen;

namespace CampusCrates
{
    public sealed class PrototypeBoardView : MonoBehaviour
    {
        private readonly SupplyKind[][] layout = OpeningLevel.Create();

        private static readonly Color32 Background = new Color32(27, 48, 56, 255);
        private static readonly Color32 BoardSurface = new Color32(255, 246, 226, 255);
        private static readonly Color32 Behind = new Color32(58, 76, 85, 255);
        private static readonly Color32 Ink = new Color32(24, 47, 56, 255);
        private static readonly Color32 Paper = new Color32(255, 253, 244, 255);
        private static readonly Color32 CoveredTile = new Color32(196, 215, 204, 255);
        private static readonly Color32 TableEdge = new Color32(149, 103, 68, 255);
        private static readonly Color32 Muted = new Color32(72, 93, 100, 255);
        private static readonly Color32 Mint = new Color32(169, 225, 196, 255);
        private static readonly Color32 Gold = new Color32(250, 215, 140, 255);
        private static readonly Color32 Lilac = new Color32(222, 202, 246, 255);
        private static readonly Color32 Peach = new Color32(253, 199, 178, 255);
        private static readonly Color32 Positive = new Color32(28, 97, 74, 255);
        private static readonly Color32 Error = new Color32(150, 46, 30, 255);
        private static readonly Color32 DarkGlass = new Color32(23, 48, 49, 225);
        private static readonly Color32 TrayRecess = new Color32(111, 70, 38, 198);
        private static readonly Rect Fill = new Rect(0, 0, 1, 1);
        private static readonly Rect TrayArtworkCrop = new Rect(.011f, .211f, .978f, .578f);

        private CardDisplay[][] cards;
        private readonly Image[] slots = new Image[PackingBoard.TrayCapacity];
        private readonly RectTransform[] slotIcons = new RectTransform[PackingBoard.TrayCapacity];
        private readonly SupplyKind?[] slotKinds = new SupplyKind?[PackingBoard.TrayCapacity];
        private readonly Dictionary<SupplyKind, Sprite> supplyArt = new Dictionary<SupplyKind, Sprite>();
        private readonly List<Sprite> generatedArt = new List<Sprite>();
        private RectTransform flyingCard;
        private MotionHandle cardFlight;
        private PackingBoard board;
        private Font font;
        private RectTransform viewport;
        private RectTransform homeScreen;
        private RectTransform gameScreen;
        private Sprite roundedTile;
        private Texture2D roundedTexture;
        private Rect lastSafeArea;
        private int lastWidth;
        private int lastHeight;
        private Text crateCount;
        private Text trayCount;
        private Text feedback;
        private RectTransform completionCard;

        private sealed class CardDisplay
        {
            public Button Button;
            public RectTransform Icon;
            public RectTransform Lock;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OpenApp()
        {
            if (SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity")
                return;

            if (EventSystem.current == null)
                new GameObject("Campus Crates Input", typeof(EventSystem), typeof(InputSystemUIInputModule));

            new GameObject("Campus Crates Board", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PrototypeBoardView));
        }

        private void Awake()
        {
            board = new PackingBoard(layout);
            cards = new CardDisplay[layout.Length][];
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateRoundedTile();
            GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(752, 1280);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var background = Panel(transform, "Backdrop", Fill, Background).rectTransform;
            AddThemeArt(background, "Campus courtyard", "campus", Fill, Fill, AspectRatioFitter.AspectMode.EnvelopeParent);
            Panel(background, "Backdrop tint", Fill, new Color32(16, 43, 43, 38));
            viewport = Box(background, "Safe area", Fill);
            homeScreen = Box(viewport, "Home screen", Fill);
            gameScreen = Box(viewport, "Packing table", Fill);
            BuildHome();
            BuildGame();
            UpdateViewport();
            Restart();
            ShowHome();
        }

        private void Update() => UpdateViewport();

        private void UpdateViewport()
        {
            int width = DeviceScreen.width;
            int height = DeviceScreen.height;
            Rect safeArea = DeviceScreen.safeArea;
            if (width == lastWidth && height == lastHeight && safeArea == lastSafeArea)
                return;

            lastWidth = width;
            lastHeight = height;
            lastSafeArea = safeArea;
            var anchors = PortraitViewport.Anchors(width, height, safeArea.x, safeArea.y, safeArea.width, safeArea.height);
            Place(viewport, At(anchors.Left, anchors.Bottom, anchors.Right, anchors.Top));
        }

        private void BuildHome()
        {
            var title = Label(homeScreen, "Game title", "Campus\nCrates", At(.06f, .718f, .94f, .947f), 107, Paper);
            title.lineSpacing = .81f;
            AddShade(title.gameObject, new Vector2(0, -6));

            if (AddThemeArt(homeScreen, "Event crate", "event-crate", At(.035f, .269f, .965f, .686f), Fill) == null)
            {
                var crate = Panel(homeScreen, "Crate illustration", At(.17f, .33f, .83f, .61f), Gold, true).rectTransform;
                Panel(crate, "Packing tape", At(.43f, .02f, .57f, .98f), Paper);
                SupplyIcon(crate, SupplyKind.Notebook, At(.12f, .16f, .44f, .86f));
                SupplyIcon(crate, SupplyKind.Pencil, At(.56f, .16f, .88f, .86f));
            }

            Button play = MakePackingButton(homeScreen, "Play", At(.105f, .13f, .895f, .239f), Gold);
            Label(play.transform, "Text", "Play", At(.11f, .14f, .89f, .94f), 48, Ink);
            play.onClick.AddListener(StartLevel);
        }

        private void BuildGame()
        {
            Button home = MakePackingButton(gameScreen, "Home", At(.054f, .916f, .219f, .992f), Positive);
            DrawHouse(home.transform);
            home.onClick.AddListener(ShowHome);
            Panel(gameScreen, "Level badge", At(.325f, .926f, .675f, .981f), DarkGlass, true);
            Label(gameScreen, "Level", "Level 1", At(.325f, .926f, .675f, .981f), 30, Paper);
            Button restart = MakePackingButton(gameScreen, "Restart", At(.781f, .916f, .946f, .992f), Positive);
            DrawRestart(restart.transform);
            restart.onClick.AddListener(Restart);

            var progress = Panel(gameScreen, "Packed crates badge", At(.339f, .834f, .661f, .909f), DarkGlass, true).rectTransform;
            var parcel = Panel(progress, "Crate icon", At(.12f, .20f, .38f, .79f), Gold, true).rectTransform;
            Panel(parcel, "Tape", At(.43f, 0f, .57f, 1f), Paper);
            crateCount = Label(progress, "Crates packed", "", At(.40f, .11f, .92f, .89f), 32, Gold);

            for (int column = 0; column < layout.Length; column++)
                CreateColumn(gameScreen, column);

            Panel(gameScreen, "Tray capacity tag", At(.753f, .332f, .935f, .383f), DarkGlass, true);
            trayCount = Label(gameScreen, "Tray capacity", "", At(.758f, .334f, .930f, .381f), 29, Gold);
            if (AddThemeArt(gameScreen, "Cardboard sorting tray", "packing-tray", At(.033f, .193f, .967f, .334f),
                TrayArtworkCrop, AspectRatioFitter.AspectMode.None) == null)
                Panel(gameScreen, "Tray", At(.04f, .202f, .96f, .333f), TableEdge, true);
            for (int index = 0; index < slots.Length; index++)
            {
                float left = .073f + index * .123f;
                slots[index] = Panel(gameScreen, $"Slot {index + 1}", At(left, .224f, left + .108f, .305f), TrayRecess, true);
            }

            var note = Panel(gameScreen, "Feedback surface", At(.23f, .092f, .77f, .151f), BoardSurface, true);
            AddShade(note.gameObject, new Vector2(0, -4));
            feedback = Label(note.transform, "Feedback", "", At(.06f, .08f, .94f, .92f), 28, Ink);

            completionCard = Panel(gameScreen, "Packed result", At(.06f, .423f, .94f, .742f), BoardSurface, true).rectTransform;
            Label(completionCard, "Title", "EVENT READY!", At(.06f, .74f, .94f, .95f), 52, Positive);
            AddThemeArt(completionCard, "Packed event crate", "event-crate", At(.19f, .20f, .81f, .745f), Fill);
            Label(completionCard, "Result", $"{board.TotalCrates} crates packed", At(.07f, .035f, .93f, .20f), 30, Ink);
        }

        private void StartLevel()
        {
            Restart();
            homeScreen.gameObject.SetActive(false);
            gameScreen.gameObject.SetActive(true);
        }

        private void ShowHome()
        {
            CancelFlight();
            gameScreen.gameObject.SetActive(false);
            homeScreen.gameObject.SetActive(true);
        }

        private void CreateColumn(Transform parent, int column)
        {
            cards[column] = new CardDisplay[layout[column].Length];

            // Unity UI draws later siblings over earlier ones. Build the front card last.
            for (int index = cards[column].Length - 1; index >= 0; index--)
            {
                int cardIndex = index;
                SupplyKind kind = layout[column][index];
                Button button = MakeButton(parent, $"Stack {column + 1} / {kind}", CardBounds(column, index), CoveredTile);
                var shadow = button.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.08f, 0.14f, 0.13f, .48f);
                shadow.effectDistance = new Vector2(0, -9);
                cards[column][index] = new CardDisplay
                {
                    Button = button,
                    Icon = SupplyIcon(button.transform, kind, At(.09f, .07f, .91f, .94f)),
                    Lock = DrawLock(button.transform)
                };
                button.onClick.AddListener(() => Select(column, cardIndex, kind));
            }
        }

        private void Select(int column, int cardIndex, SupplyKind kind)
        {
            int packedBefore = board.CratesPacked;
            int slot = board.Tray.Count;
            if (flyingCard != null || !board.TrySelect(column, cardIndex))
                return;

            var source = (RectTransform)cards[column][cardIndex].Button.transform;
            var destination = slots[slot].rectTransform;
            Vector3 from = gameScreen.InverseTransformPoint(source.TransformPoint(source.rect.center));
            Vector3 to = gameScreen.InverseTransformPoint(destination.TransformPoint(destination.rect.center));
            Vector2 startSize = source.rect.size;
            Vector2 endSize = destination.rect.size;

            flyingCard = Box(gameScreen, "Moving supply", Fill);
            flyingCard.anchorMin = flyingCard.anchorMax = new Vector2(.5f, .5f);
            flyingCard.sizeDelta = startSize;
            flyingCard.localPosition = from;
            SupplyIcon(flyingCard, kind, At(.10f, .06f, .90f, .94f));
            source.gameObject.SetActive(false);

            cardFlight = LMotion.Create(0f, 1f, .19f)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() => CompleteSelection(packedBefore))
                .Bind(progress =>
                {
                    flyingCard.localPosition = Vector3.LerpUnclamped(from, to, progress);
                    flyingCard.sizeDelta = Vector2.LerpUnclamped(startSize, endSize, progress);
                });
        }

        private void CompleteSelection(int packedBefore)
        {
            RemoveFlyingCard();
            if (board.Status == PackingStatus.Won)
                SetFeedback("", Ink);
            else if (board.Status == PackingStatus.Lost)
                SetFeedback("Tray full", Error);
            else if (board.CratesPacked > packedBefore)
                SetFeedback("+1 crate", Positive);
            else if (board.Tray.Count == PackingBoard.TrayCapacity - 1)
                SetFeedback("1 slot left", Error);
            else
                SetFeedback("", Ink);

            Refresh();
        }

        private void Restart()
        {
            CancelFlight();
            board.Reset();
            SetFeedback("Match 3 to pack", Ink);
            Refresh();
        }

        private void SetFeedback(string message, Color color)
        {
            feedback.text = message;
            feedback.color = color;
            feedback.transform.parent.gameObject.SetActive(message.Length != 0);
        }

        private void Refresh()
        {
            completionCard.gameObject.SetActive(board.Status == PackingStatus.Won);
            for (int column = 0; column < cards.Length; column++)
            {
                int front = board.FrontIndex(column);
                for (int index = 0; index < cards[column].Length; index++)
                {
                    CardDisplay card = cards[column][index];
                    bool visible = index >= front;
                    bool ready = index == front && board.Status == PackingStatus.Playing;
                    card.Button.gameObject.SetActive(visible);
                    card.Button.interactable = ready;
                    if (visible)
                        Place((RectTransform)card.Button.transform, CardBounds(column, index - front));
                    card.Button.image.color = ready ? Paper : CoveredTile;
                    card.Lock.gameObject.SetActive(!ready);
                    Place(card.Icon, ready ? At(.09f, .07f, .91f, .94f) : At(.06f, .616f, .76f, .985f));
                }
            }

            for (int index = 0; index < slots.Length; index++)
            {
                SupplyKind? kind = index < board.Tray.Count ? board.Tray[index] : (SupplyKind?)null;
                if (kind == slotKinds[index])
                    continue;
                if (slotIcons[index] != null)
                {
                    slotIcons[index].gameObject.SetActive(false);
                    Destroy(slotIcons[index].gameObject);
                }

                slotKinds[index] = kind;
                slotIcons[index] = kind.HasValue
                    ? SupplyIcon(slots[index].transform, kind.Value, At(.05f, .05f, .95f, .95f))
                    : null;
            }

            crateCount.text = $"{board.CratesPacked} / {board.TotalCrates}";
            trayCount.text = $"{board.Tray.Count} / {PackingBoard.TrayCapacity}";
            trayCount.color = board.Tray.Count >= 6 ? Peach : Gold;
        }

        private static Rect CardBounds(int column, int depth)
        {
            var bounds = StackTileLayout.Bounds(column, depth);
            return At(bounds.Left, bounds.Bottom, bounds.Right, bounds.Top);
        }

        private RectTransform DrawLock(Transform parent)
        {
            var mark = Box(parent, "Locked", At(.76f, .63f, .94f, .94f));
            Panel(mark, "Shackle", At(.25f, .43f, .75f, .92f), Muted, true);
            Panel(mark, "Shackle cutout", At(.39f, .49f, .61f, .75f), CoveredTile);
            Panel(mark, "Lock body", At(.13f, .12f, .87f, .59f), Muted, true);
            return mark;
        }

        private RectTransform SupplyIcon(Transform parent, SupplyKind kind, Rect bounds)
        {
            var container = Box(parent, kind + " symbol", bounds);
            var mark = Box(container, "Art", Fill);
            var aspect = mark.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 1;

            Sprite sprite = GetSupplyArt(kind);
            if (sprite != null)
            {
                Image image = Panel(mark, "Supply sprite", Fill, Color.white);
                image.sprite = sprite;
                image.preserveAspect = true;
                return container;
            }

            if (kind == SupplyKind.Notebook)
            {
                Panel(mark, "Cover", At(.15f, .08f, .86f, .94f), Positive, true);
                Panel(mark, "Paper edge", At(.25f, .10f, .85f, .22f), Paper);
                Panel(mark, "Binding", At(.15f, .13f, .26f, .91f), Ink);
                Panel(mark, "Label", At(.37f, .49f, .73f, .71f), Paper, true);
                Panel(mark, "Elastic", At(.71f, .13f, .78f, .91f), Gold);
            }
            else if (kind == SupplyKind.Pencil)
            {
                var pencil = Box(mark, "Pencil", At(.24f, .04f, .76f, .96f));
                pencil.localRotation = Quaternion.Euler(0, 0, -37);
                Panel(pencil, "Lead", At(.40f, .02f, .60f, .23f), Ink, true);
                Panel(pencil, "Body", At(.24f, .18f, .76f, .82f), Gold, true);
                Panel(pencil, "Glint", At(.33f, .27f, .45f, .73f), Paper);
                Panel(pencil, "Band", At(.24f, .75f, .76f, .85f), Ink);
                Panel(pencil, "Eraser", At(.25f, .84f, .75f, .98f), Peach, true);
            }
            else if (kind == SupplyKind.Cup)
            {
                Panel(mark, "Handle", At(.65f, .35f, .96f, .75f), Ink, true);
                Panel(mark, "Handle gap", At(.72f, .44f, .87f, .65f), Paper, true);
                Panel(mark, "Ceramic body", At(.10f, .20f, .74f, .77f), Lilac, true);
                Panel(mark, "Rim", At(.10f, .67f, .74f, .84f), Ink, true);
                Panel(mark, "Reflection", At(.22f, .29f, .33f, .61f), Paper, true);
            }
            else
            {
                var pin = Panel(mark, "Enamel badge", At(.17f, .17f, .83f, .83f), Peach, true).rectTransform;
                pin.localRotation = Quaternion.Euler(0, 0, 45);
                var star = Box(mark, "Star mark", At(.28f, .28f, .72f, .72f));
                Panel(star, "Vertical", At(.38f, .04f, .62f, .96f), Ink, true);
                Panel(star, "Horizontal", At(.04f, .38f, .96f, .62f), Ink, true);
            }

            return container;
        }

        private Sprite GetSupplyArt(SupplyKind kind)
        {
            if (supplyArt.TryGetValue(kind, out Sprite sprite))
                return sprite;

            string path = $"CampusCrates/Supplies/{kind.ToString().ToLowerInvariant()}";
            sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
                    generatedArt.Add(sprite);
                }
            }

            supplyArt.Add(kind, sprite);
            return sprite;
        }

        private void CancelFlight()
        {
            if (flyingCard == null)
                return;
            cardFlight.TryCancel();
            RemoveFlyingCard();
        }

        private void RemoveFlyingCard()
        {
            flyingCard.gameObject.SetActive(false);
            Destroy(flyingCard.gameObject);
            flyingCard = null;
        }

        private void CreateRoundedTile()
        {
            const int size = 48;
            const int radius = 12;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Max(0, radius - Mathf.Min(x, size - 1 - x));
                int dy = Mathf.Max(0, radius - Mathf.Min(y, size - 1 - y));
                float coverage = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * coverage));
            }
            roundedTexture.SetPixels32(pixels);
            roundedTexture.Apply(false, false);
            roundedTile = Sprite.Create(roundedTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
        }

        private void OnDestroy()
        {
            if (flyingCard != null)
                cardFlight.TryCancel();
            foreach (Sprite sprite in generatedArt)
                Destroy(sprite);
            Destroy(roundedTile);
            Destroy(roundedTexture);
        }

        private static Rect At(float left, float bottom, float right, float top) => Rect.MinMaxRect(left, bottom, right, top);

        private static void Place(RectTransform rect, Rect bounds)
        {
            rect.anchorMin = bounds.min;
            rect.anchorMax = bounds.max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform Box(Transform parent, string objectName, Rect bounds)
        {
            var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, bounds);
            return rect;
        }

        private static RawImage AddThemeArt(Transform parent, string name, string resource, Rect bounds, Rect uv,
            AspectRatioFitter.AspectMode mode = AspectRatioFitter.AspectMode.FitInParent)
        {
            var texture = Resources.Load<Texture2D>("CampusCrates/Theme/" + resource);
            if (texture == null)
                return null;

            var frame = Box(parent, name, bounds);
            if (mode == AspectRatioFitter.AspectMode.EnvelopeParent)
                frame.gameObject.AddComponent<RectMask2D>();

            var art = Box(frame, "Illustration", Fill).gameObject.AddComponent<RawImage>();
            art.texture = texture;
            art.uvRect = uv;
            art.raycastTarget = false;
            if (mode != AspectRatioFitter.AspectMode.None)
            {
                var aspect = art.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = mode;
                aspect.aspectRatio = texture.width * uv.width / (texture.height * uv.height);
            }
            return art;
        }

        private static void AddShade(GameObject target, Vector2 distance)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.07f, 0.16f, 0.15f, .53f);
            shadow.effectDistance = distance;
        }

        private Button MakePackingButton(Transform parent, string name, Rect bounds, Color faceColor)
        {
            Button button = MakeButton(parent, name, bounds, TableEdge);
            AddShade(button.gameObject, new Vector2(0, -8));
            var face = Panel(button.transform, "Pressed paper face", At(.04f, .105f, .96f, .974f), faceColor, true);
            button.targetGraphic = face;
            Panel(face.transform, "Top glint", At(.09f, .89f, .91f, .965f), new Color32(255, 244, 206, 103), true);
            return button;
        }

        private RectTransform ButtonGlyph(Transform button)
        {
            var frame = Box(button, "Icon frame", At(.15f, .19f, .85f, .84f));
            var glyph = Box(frame, "Icon", Fill);
            var ratio = glyph.gameObject.AddComponent<AspectRatioFitter>();
            ratio.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ratio.aspectRatio = 1f;
            return glyph;
        }

        private void DrawHouse(Transform button)
        {
            var icon = ButtonGlyph(button);
            Stroke(icon, new Vector2(.11f, .49f), new Vector2(.50f, .84f), .11f);
            Stroke(icon, new Vector2(.50f, .84f), new Vector2(.89f, .49f), .11f);
            Panel(icon, "House", At(.24f, .15f, .76f, .53f), Paper, true);
            Panel(icon, "Door", At(.43f, .15f, .57f, .36f), Positive, true);
        }

        private void DrawRestart(Transform button)
        {
            var icon = ButtonGlyph(button);
            var center = new Vector2(.50f, .50f);
            const int segments = 10;
            const float radius = .30f;
            Vector2 previous = center;
            float angle = 0;
            for (int index = 0; index <= segments; index++)
            {
                angle = (-55f - 290f * index / segments) * Mathf.Deg2Rad;
                Vector2 current = center + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (index > 0)
                    Stroke(icon, previous, current, .09f);
                previous = current;
            }

            var direction = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
            var side = new Vector2(-direction.y, direction.x);
            Vector2 tail = previous - .18f * direction;
            Stroke(icon, tail + .11f * side, previous, .09f);
            Stroke(icon, tail - .11f * side, previous, .09f);
        }

        private void Stroke(Transform parent, Vector2 start, Vector2 end, float thickness)
        {
            Vector2 delta = end - start;
            Vector2 center = (start + end) * .5f;
            float halfWidth = (delta.magnitude + thickness * .35f) * .5f;
            float halfHeight = thickness * .5f;
            var line = Panel(parent, "Icon stroke", At(center.x - halfWidth, center.y - halfHeight,
                center.x + halfWidth, center.y + halfHeight), Paper);
            line.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private Image Panel(Transform parent, string objectName, Rect bounds, Color color, bool rounded = false)
        {
            var image = Box(parent, objectName, bounds).gameObject.AddComponent<Image>();
            if (rounded)
            {
                image.sprite = roundedTile;
                image.type = Image.Type.Sliced;
            }
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Button MakeButton(Transform parent, string objectName, Rect bounds, Color color)
        {
            Image image = Panel(parent, objectName, bounds, color, true);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.disabledColor = Color.white;
            colors.highlightedColor = new Color(0.91f, 0.95f, 0.97f);
            colors.pressedColor = new Color(0.78f, 0.87f, 0.90f);
            button.colors = colors;
            return button;
        }

        private Text Label(Transform parent, string objectName, string value, Rect bounds,
            int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var label = Box(parent, objectName, bounds).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.RoundToInt(size * .73f);
            label.resizeTextMaxSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = alignment;
            label.color = color;
            label.text = value;
            label.raycastTarget = false;
            return label;
        }
    }
}
