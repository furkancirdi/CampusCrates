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
        private const string ReachedLevelKey = "CampusCrates.ReachedLevel";
        private const string CompletedLevelKey = "CampusCrates.CompletedLevel";
        private int level;
        private SupplyKind[][] layout;
        private ShapeTile[] shape;

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
        private static readonly Rect NextButtonArtworkCrop = new Rect(0f, .135f, 1f, .735f);
        private static readonly Rect MintButtonArtworkCrop = new Rect(.005f, .09f, .99f, .82f);
        private const float TrailArtAspect = 941f / 1672f;
        private static readonly string[] StopNames = { "First Crate", "Unpack", "Supply Run", "Packing Rush", "Pyramid", "Star" };
        private static readonly Vector2[] TrailStops =
        {
            new Vector2(.490f, .219f), new Vector2(.225f, .394f), new Vector2(.770f, .522f),
            new Vector2(.235f, .662f), new Vector2(.727f, .750f), new Vector2(.536f, .806f)
        };
        private static readonly Vector2[][] TrailBends =
        {
            new[] { new Vector2(.490f, .300f), new Vector2(.325f, .342f) },
            new[] { new Vector2(.273f, .446f), new Vector2(.510f, .464f), new Vector2(.710f, .484f) },
            new[] { new Vector2(.723f, .562f), new Vector2(.465f, .596f), new Vector2(.295f, .625f) },
            new[] { new Vector2(.325f, .704f), new Vector2(.499f, .718f), new Vector2(.668f, .736f) },
            new[] { new Vector2(.706f, .778f), new Vector2(.635f, .800f) }
        };

        private CardDisplay[][] cards;
        private CardDisplay[] tileCards;
        private readonly Image[] slots = new Image[PackingBoard.TrayCapacity];
        private readonly RectTransform[] slotIcons = new RectTransform[PackingBoard.TrayCapacity];
        private readonly SupplyKind?[] slotKinds = new SupplyKind?[PackingBoard.TrayCapacity];
        private readonly Dictionary<SupplyKind, Sprite> supplyArt = new Dictionary<SupplyKind, Sprite>();
        private readonly List<Sprite> generatedArt = new List<Sprite>();
        private RectTransform flyingCard;
        private MotionHandle cardFlight;
        private PackingBoard board;
        private Font font;
        private Font detailFont;
        private Font legacyFont;
        private Sprite tileFace;
        private RectTransform viewport;
        private RectTransform homeScreen;
        private RectTransform mapScreen;
        private RectTransform gameScreen;
        private CampusProgress campusProgress;
        private readonly Button[] levelButtons = new Button[ShapeLevel.Last];
        private readonly RectTransform[] levelLocks = new RectTransform[ShapeLevel.Last];
        private readonly RectTransform[] levelStamps = new RectTransform[ShapeLevel.Last];
        private readonly List<Image>[] trailDashes = new List<Image>[TrailBends.Length];
        private readonly float[] trailLengths = new float[TrailBends.Length];
        private RawImage[] mapBooth;
        private Text standProgress;
        private Text nextStop;
        private RectTransform trailParcel;
        private RectTransform homeCrate;
        private RectTransform resultPanel;
        private float resultOpenedAt;
        private int lastMapStop;
        private int parcelSegment = -1;
        private float parcelStartedAt;
        private CanvasGroup gameControls;
        private Button hintButton;
        private RectTransform settings;
        private RectTransform stackArea;
        private Text guide;
        private RectTransform guideMarker;
        private RectTransform guideArrow;
        private Outline highlightedTile;
        private Sprite roundedTile;
        private Texture2D roundedTexture;
        private Rect lastSafeArea;
        private int lastWidth;
        private int lastHeight;
        private Text crateCount;
        private RectTransform crateBadge;
        private float lastPackAt = -1f;
        private Text trayCount;
        private Text feedback;
        private Text levelLabel;
        private Text homeLevelLabel;
        private RectTransform completionCard;
        private Text completionTitle;
        private Text completionResult;
        private Text completionAction;
        private RawImage crateArtwork;
        private RawImage retryArtwork;
        private RawImage[] resultBooth;
        private bool deliveryEarned;

        private sealed class CardDisplay
        {
            public Button Button;
            public RectTransform Icon;
            public RectTransform Lock;
            public Outline HintOutline;
            public CanvasGroup Shade;
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
            legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            font = Resources.Load<Font>("CampusCrates/Theme/Fredoka") ?? legacyFont;
            detailFont = Resources.Load<Font>("CampusCrates/Theme/Fredoka-Medium") ?? font;
            var tileTexture = Resources.Load<Texture2D>("CampusCrates/Theme/tile-face");
            if (tileTexture != null)
            {
                tileFace = Sprite.Create(tileTexture, new Rect(0f, 0f, tileTexture.width, tileTexture.height),
                    new Vector2(.5f, .5f));
                generatedArt.Add(tileFace);
            }
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
            mapScreen = Box(viewport, "Campus map", Fill);
            gameScreen = Box(viewport, "Packing table", Fill);
            gameControls = gameScreen.gameObject.AddComponent<CanvasGroup>();
            int legacyReached = Mathf.Clamp(PlayerPrefs.GetInt(ReachedLevelKey, 1), 1, ShapeLevel.Last);
            campusProgress = new CampusProgress(PlayerPrefs.GetInt(CompletedLevelKey, legacyReached - 1));
            lastMapStop = campusProgress.UnlockedLevel;
            BuildHome();
            BuildMap();
            BuildGame();
            BuildSettings();
            UpdateViewport();
            LoadLevel(campusProgress.UnlockedLevel);
            ShowHome();
        }

        private void Update()
        {
            UpdateViewport();
            float time = Time.unscaledTime;
            if (guideMarker.gameObject.activeInHierarchy)
                guideArrow.anchoredPosition = Vector2.up * (5f * Mathf.Sin(time * 6f));

            if (homeCrate != null && homeScreen.gameObject.activeSelf)
            {
                homeCrate.anchoredPosition = Vector2.up * (4f * Mathf.Sin(time * 1.9f));
                homeCrate.localRotation = Quaternion.Euler(0f, 0f, .65f * Mathf.Sin(time * 1.6f));
            }

            if (mapScreen.gameObject.activeSelf)
                AnimateTrail(time);
            if (gameScreen.gameObject.activeSelf && lastPackAt >= 0f)
            {
                float t = Mathf.Clamp01((time - lastPackAt) / .34f);
                crateBadge.localScale = Vector3.one * (1f + .16f * Mathf.Sin(t * Mathf.PI));
                if (t >= 1f)
                    lastPackAt = -1f;
            }
            if (completionCard.gameObject.activeInHierarchy)
            {
                float t = Mathf.Clamp01((time - resultOpenedAt) / .38f);
                float scale = Mathf.Lerp(.84f, 1f, Mathf.SmoothStep(0f, 1f, t)) + .12f * Mathf.Sin(t * Mathf.PI);
                resultPanel.localScale = Vector3.one * scale;
            }
        }

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
            if (AddThemeArt(homeScreen, "Campus Crates wordmark", "wordmark", At(.050f, .705f, .950f, .955f), Fill) == null)
            {
                var title = Label(homeScreen, "Game title", "Campus\nCrates", At(.06f, .718f, .94f, .947f), 107, Paper);
                title.lineSpacing = .81f;
                AddShade(title.gameObject, new Vector2(0, -6));
            }

            homeCrate = Box(homeScreen, "Floating event crate", At(.13f, .320f, .87f, .674f));
            if (AddThemeArt(homeCrate, "Event crate", "event-crate", Fill, Fill) == null)
            {
                var crate = Panel(homeCrate, "Crate illustration", At(.12f, .1f, .88f, .9f), Gold, true).rectTransform;
                Panel(crate, "Packing tape", At(.43f, .02f, .57f, .98f), Paper);
                SupplyIcon(crate, SupplyKind.Notebook, At(.12f, .16f, .44f, .86f));
                SupplyIcon(crate, SupplyKind.Pencil, At(.56f, .16f, .88f, .86f));
            }

            Button play = MakePackingButton(homeScreen, "Play", At(.12f, .174f, .88f, .278f), Gold);
            Label(play.transform, "Text", "Play", At(.11f, .416f, .89f, .850f), 52, Ink);
            homeLevelLabel = Label(play.transform, "Current level", "", At(.11f, .200f, .89f, .450f), 29, Ink, detail: true);
            play.onClick.AddListener(StartFromHome);

            Button map = MakePackingButton(homeScreen, "Campus Map", At(.18f, .082f, .82f, .153f), Positive);
            Label(map.transform, "Text", "Campus Map", At(.11f, .18f, .89f, .87f), 38, Paper);
            map.onClick.AddListener(ShowMap);
        }

        private void BuildMap()
        {
            Panel(mapScreen, "Warm map wash", Fill, new Color32(16, 51, 46, 190));
            var composition = Box(mapScreen, "Centered trail composition", At(.018f, .012f, .982f, .99f));
            var column = Box(composition, "Trail column", Fill);
            var fitter = column.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = .515f;

            var mapPlate = Panel(column, "Trail print", At(.015f, .160f, .985f, .893f), BoardSurface, true).rectTransform;
            AddShade(mapPlate.gameObject, new Vector2(0, -6));
            var trailArt = AddThemeArt(mapPlate, "Illustrated campus trail", "campus-trail",
                At(.010f, .006f, .990f, .994f), Fill, AspectRatioFitter.AspectMode.EnvelopeParent);
            Transform trail = trailArt != null ? trailArt.transform : mapPlate;

            for (int segment = 0; segment < trailDashes.Length; segment++)
            {
                var dashes = new List<Image>();
                trailDashes[segment] = dashes;
                for (int leg = 0; leg <= TrailBends[segment].Length; leg++)
                    trailLengths[segment] += TrailDistance(TrailWaypoint(segment, leg), TrailWaypoint(segment, leg + 1));
                int intervals = Mathf.Max(3, Mathf.RoundToInt(trailLengths[segment] / .021f));
                for (int step = 1; step < intervals; step++)
                {
                    Vector2 point = TrailPoint(segment, (float)step / intervals);
                    Image dash = Panel(trail, "Trail stitch", Centered(point, .019f, .019f * TrailArtAspect), Paper, true);
                    dashes.Add(dash);
                }
            }

            for (int index = 0; index < levelButtons.Length; index++)
            {
                int number = index + 1;
                Button stop = MakeArtButton(trail, $"Stop {number} / {StopNames[index]}",
                    Centered(TrailStops[index], .158f, .158f * TrailArtAspect), "trail-pin", Fill, Gold);
                Label(stop.transform, "Stop number", number.ToString(), At(.17f, .20f, .83f, .89f), 52, Ink);
                levelLocks[index] = DrawLock(stop.transform, At(.73f, .075f, .94f, .34f));
                levelStamps[index] = SupplyIcon(stop.transform, SupplyKind.Badge, At(.64f, .04f, .97f, .37f));
                levelButtons[index] = stop;
                stop.onClick.AddListener(() => StartLevel(number));
            }

            trailParcel = Box(trail, "Parcel traveling to next stop", Fill);
            var parcel = Panel(trailParcel, "Kraft parcel", At(.17f, .19f, .83f, .81f), Gold, true);
            Panel(parcel.transform, "Paper tape", At(.40f, .06f, .61f, .94f), Paper);
            AddShade(parcel.gameObject, new Vector2(0, -5));
            trailParcel.gameObject.SetActive(false);

            Button back = MakeArtButton(column, "Back", At(.020f, .911f, .154f, .987f), "trail-pin", Fill, Gold);
            Label(back.transform, "Back arrow", "‹", At(.17f, .20f, .83f, .89f), 58, Ink);
            back.onClick.AddListener(ShowHome);
            var title = Label(column, "Map title", "Festival Trail", At(.18f, .907f, .82f, .991f), 60, Paper);
            AddShade(title.gameObject, new Vector2(0, -3));

            var deliveryTag = Panel(column, "Festival ticket", At(.015f, .024f, .985f, .142f), BoardSurface, true).rectTransform;
            AddShade(deliveryTag.gameObject, new Vector2(0, -6));
            mapBooth = BuildBoothArt(deliveryTag, At(.035f, .09f, .238f, .91f));
            nextStop = Label(deliveryTag, "Next stop", "", At(.270f, .46f, .944f, .86f), 37, Positive, TextAnchor.MiddleLeft);
            standProgress = Label(deliveryTag, "Stand progress", "", At(.270f, .15f, .944f, .47f), 29, Ink,
                TextAnchor.MiddleLeft, detail: true);
        }

        private static Rect Centered(Vector2 center, float width, float height)
            => new Rect(center.x - width * .5f, center.y - height * .5f, width, height);

        private static Vector2 TrailWaypoint(int segment, int index)
        {
            Vector2[] bends = TrailBends[segment];
            if (index == 0)
                return TrailStops[segment];
            return index <= bends.Length ? bends[index - 1] : TrailStops[segment + 1];
        }

        private static float TrailDistance(Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;
            delta.x *= TrailArtAspect;
            return delta.magnitude;
        }

        private Vector2 TrailPoint(int segment, float t)
        {
            float distance = Mathf.Clamp01(t) * trailLengths[segment];
            for (int leg = 0; leg <= TrailBends[segment].Length; leg++)
            {
                Vector2 start = TrailWaypoint(segment, leg);
                Vector2 end = TrailWaypoint(segment, leg + 1);
                float length = TrailDistance(start, end);
                if (distance <= length)
                    return Vector2.Lerp(start, end, distance / length);
                distance -= length;
            }
            return TrailStops[segment + 1];
        }

        private static RawImage[] BuildBoothArt(Transform parent, Rect bounds)
        {
            var stages = new RawImage[CampusProgress.DeliveriesNeeded + 1];
            for (int index = 0; index < stages.Length; index++)
                stages[index] = AddThemeArt(parent, $"Stand stage {index}", $"booth-{index}", bounds, Fill);
            return stages;
        }

        private static void ShowBoothStage(RawImage[] stages, int stage)
        {
            for (int index = 0; index < stages.Length; index++)
                if (stages[index] != null)
                    stages[index].gameObject.SetActive(index == stage);
        }

        private void ShowMap()
        {
            ShowBoothStage(mapBooth, campusProgress.Deliveries);
            standProgress.text = campusProgress.Deliveries == CampusProgress.DeliveriesNeeded
                ? "Festival stand ready!"
                : $"Festival stand  {campusProgress.Deliveries}/{CampusProgress.DeliveriesNeeded}";
            int unlockedLevel = campusProgress.UnlockedLevel;
            nextStop.text = campusProgress.HighestCompleted == ShapeLevel.Last
                ? "Trail complete!"
                : $"Next stop: {StopNames[unlockedLevel - 1]}";

            for (int index = 0; index < levelButtons.Length; index++)
            {
                int number = index + 1;
                bool unlocked = number <= unlockedLevel;
                bool completed = number <= campusProgress.HighestCompleted;
                Button button = levelButtons[index];
                button.interactable = unlocked;
                button.targetGraphic.color = completed ? Mint : unlocked ? Color.white : new Color32(179, 179, 165, 255);
                button.transform.localScale = Vector3.one;
                levelLocks[index].gameObject.SetActive(!unlocked);
                levelStamps[index].gameObject.SetActive(completed);
            }

            for (int segment = 0; segment < trailDashes.Length; segment++)
                foreach (Image dash in trailDashes[segment])
                    dash.color = segment < campusProgress.HighestCompleted
                        ? new Color32(26, 112, 95, 205) : new Color32(255, 246, 218, 155);

            parcelSegment = unlockedLevel > lastMapStop ? unlockedLevel - 2 : -1;
            lastMapStop = unlockedLevel;
            parcelStartedAt = Time.unscaledTime;
            if (parcelSegment >= 0)
                Place(trailParcel, Centered(TrailStops[parcelSegment], .109f, .062f));
            trailParcel.gameObject.SetActive(parcelSegment >= 0);
            homeScreen.gameObject.SetActive(false);
            mapScreen.gameObject.SetActive(true);
        }

        private void AnimateTrail(float time)
        {
            if (campusProgress.HighestCompleted < ShapeLevel.Last)
            {
                Button active = levelButtons[campusProgress.UnlockedLevel - 1];
                active.transform.localScale = Vector3.one * (1.03f + .03f * Mathf.Sin(time * 4f));
            }

            if (parcelSegment < 0)
                return;

            float t = Mathf.Clamp01((time - parcelStartedAt) / 1.1f);
            Vector2 point = TrailPoint(parcelSegment, Mathf.SmoothStep(0f, 1f, t));
            Place(trailParcel, Centered(point, .109f, .062f));
            trailParcel.localRotation = Quaternion.Euler(0f, 0f, 11f * Mathf.Sin(t * 12f));
            if (t < 1f)
                return;

            trailParcel.gameObject.SetActive(false);
            parcelSegment = -1;
        }

        private void BuildGame()
        {
            AddThemeArt(gameScreen, "Level badge", "mint-button", At(.284f, .915f, .716f, .99f), MintButtonArtworkCrop);
            levelLabel = Label(gameScreen, "Level", "", At(.307f, .926f, .693f, .98f), 35, Paper);
            AddShade(levelLabel.gameObject, new Vector2(0, -3));
            Button gear = MakeButton(gameScreen, "Settings", At(.764f, .908f, .956f, .999f), Color.clear);
            RawImage gearArt = AddThemeArt(gear.transform, "Settings gear", "settings", Fill, Fill);
            if (gearArt != null)
                gear.targetGraphic = gearArt;
            else
                Label(gear.transform, "Settings label", "Settings", Fill, 24, Paper);
            gear.onClick.AddListener(() => SetSettingsOpen(true));

            crateBadge = Panel(gameScreen, "Packed crates badge", At(.342f, .838f, .658f, .907f), BoardSurface, true).rectTransform;
            AddShade(crateBadge.gameObject, new Vector2(0, -5));
            AddThemeArt(crateBadge, "Crate icon", "event-crate", At(.045f, -.055f, .49f, 1.06f), Fill);
            crateCount = Label(crateBadge, "Crates packed", "", At(.44f, .085f, .94f, .95f), 39, Positive);

            stackArea = Box(gameScreen, "Tile stacks", Fill);

            var guideTag = Panel(gameScreen, "Opening guide", At(.18f, .760f, .82f, .815f), BoardSurface, true);
            AddShade(guideTag.gameObject, new Vector2(0, -4));
            guide = Label(guideTag.transform, "Next move", "Tap 3 notebooks", At(.03f, .04f, .97f, .96f), 29, Ink);
            guideMarker = Box(gameScreen, "Guide marker", Fill);
            var arrow = Label(guideMarker, "Guide arrow", "▼", Fill, 54, Gold);
            arrow.font = legacyFont;
            AddShade(arrow.gameObject, new Vector2(0, -5));
            guideArrow = arrow.rectTransform;

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
            feedback = Label(note.transform, "Feedback", "", At(.06f, .08f, .94f, .92f), 30, Ink, detail: true);

            hintButton = MakeButton(gameScreen, "Hint", At(.044f, .078f, .203f, .164f), Color.clear);
            RawImage bulb = AddThemeArt(hintButton.transform, "Hint light bulb", "hint", Fill, Fill);
            if (bulb != null)
                hintButton.targetGraphic = bulb;
            else
            {
                hintButton.image.color = Gold;
                Label(hintButton.transform, "Hint text", "Hint", Fill, 25, Ink);
            }
            hintButton.onClick.AddListener(ShowMatchHint);

            BuildCompletionCard();
        }

        private void BuildCompletionCard()
        {
            completionCard = Panel(gameScreen, "Celebration tint", Fill, new Color32(25, 42, 39, 72)).rectTransform;
            var card = Panel(completionCard, "Packed result", At(.055f, .360f, .945f, .817f),
                new Color32(255, 248, 227, 234), true).rectTransform;
            resultPanel = card;
            AddShade(card.gameObject, new Vector2(0, -12));

            completionTitle = Label(card, "Title", "", At(.06f, .806f, .94f, .976f), 54, Positive);
            Panel(card, "Illustration backdrop", At(.20f, .333f, .80f, .789f), new Color32(250, 218, 150, 122), true);
            Rect artBounds = At(.17f, .315f, .83f, .816f);
            crateArtwork = AddThemeArt(card, "Packed event crate", "event-crate", artBounds, Fill);
            retryArtwork = AddThemeArt(card, "Overflowing tray", "tray-full", artBounds, Fill);
            resultBooth = BuildBoothArt(card, artBounds);
            var leftSparkle = Panel(card, "Left sparkle", At(.128f, .609f, .177f, .655f), Peach, true);
            leftSparkle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var rightSparkle = Panel(card, "Right sparkle", At(.810f, .683f, .855f, .725f), Gold, true);
            rightSparkle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            completionResult = Label(card, "Result", "", At(.07f, .223f, .93f, .334f), 34, Ink, detail: true);

            Button next = MakePackingButton(card, "Continue", At(.10f, .045f, .90f, .227f), Gold);
            completionAction = Label(next.transform, "Text", "", At(.08f, .12f, .92f, .94f), 38, Ink);
            next.onClick.AddListener(ContinueFromResults);
        }

        private void BuildSettings()
        {
            var backdrop = Panel(viewport, "Settings overlay", Fill, new Color32(12, 30, 33, 179));
            backdrop.raycastTarget = true;
            settings = backdrop.rectTransform;
            var dialog = Panel(settings, "Settings dialog", At(.105f, .286f, .895f, .743f), BoardSurface, true).rectTransform;
            AddShade(dialog.gameObject, new Vector2(0, -9));
            Label(dialog, "Title", "Settings", At(.08f, .76f, .92f, .96f), 52, Ink);

            Button resume = MakePackingButton(dialog, "Resume", At(.10f, .54f, .90f, .72f), Gold);
            Label(resume.transform, "Text", "Resume", Fill, 36, Ink);
            resume.onClick.AddListener(() => SetSettingsOpen(false));

            Button restart = MakePackingButton(dialog, "Restart", At(.10f, .32f, .90f, .50f), Positive);
            Label(restart.transform, "Text", "Restart", Fill, 36, Paper);
            restart.onClick.AddListener(Restart);

            Button home = MakePackingButton(dialog, "Main Menu", At(.10f, .10f, .90f, .28f), Positive);
            Label(home.transform, "Text", "Main Menu", Fill, 36, Paper);
            home.onClick.AddListener(ShowHome);
        }

        private void SetSettingsOpen(bool open)
        {
            settings.gameObject.SetActive(open);
            gameControls.interactable = !open;
            gameControls.blocksRaycasts = !open;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private void StartFromHome()
        {
            StartLevel(campusProgress.UnlockedLevel);
        }

        private void StartLevel(int number)
        {
            if (number > campusProgress.UnlockedLevel)
                return;

            if (number == level)
                Restart();
            else
                LoadLevel(number);
            homeScreen.gameObject.SetActive(false);
            mapScreen.gameObject.SetActive(false);
            gameScreen.gameObject.SetActive(true);
        }

        private void LoadLevel(int number)
        {
            CancelFlight();
            foreach (Transform tile in stackArea)
            {
                tile.gameObject.SetActive(false);
                Destroy(tile.gameObject);
            }

            level = number;
            shape = level >= ShapeLevel.First ? ShapeLevel.Create(level) : null;
            if (shape == null)
            {
                layout = OpeningLevel.Create(level);
                board = new PackingBoard(layout);
                tileCards = new CardDisplay[board.TileCount];
                cards = new CardDisplay[layout.Length][];
                int offset = 0;
                for (int column = 0; column < layout.Length; column++)
                {
                    CreateColumn(stackArea, column, offset);
                    offset += layout[column].Length;
                }
            }
            else
            {
                board = new PackingBoard(shape);
                tileCards = new CardDisplay[board.TileCount];
                CreateShape();
            }

            levelLabel.text = $"Level {level}";
            Restart();
        }

        private void ContinueFromResults()
        {
            if (board.Status == PackingStatus.Lost)
            {
                Restart();
                return;
            }

            if (board.Status != PackingStatus.Won)
                return;

            if (level < ShapeLevel.Last)
            {
                LoadLevel(level + 1);
            }
            else
            {
                ShowHome();
            }
        }

        private void ShowHome()
        {
            SetSettingsOpen(false);
            CancelFlight();
            gameScreen.gameObject.SetActive(false);
            mapScreen.gameObject.SetActive(false);
            homeLevelLabel.text = $"Level {campusProgress.UnlockedLevel}";
            homeScreen.gameObject.SetActive(true);
        }

        private void CreateColumn(Transform parent, int column, int offset)
        {
            cards[column] = new CardDisplay[layout[column].Length];

            // Unity UI draws later siblings over earlier ones. Build the front card last.
            for (int index = cards[column].Length - 1; index >= 0; index--)
            {
                int tileIndex = offset + index;
                SupplyKind kind = layout[column][index];
                CardDisplay card = CreateTile(parent, $"Stack {column + 1} / {kind}", kind, CardBounds(column, index));
                card.Lock = DrawLock(card.Button.transform, At(.76f, .63f, .94f, .94f));
                if (level == 1)
                    card.HintOutline = CreateHintOutline(card);
                cards[column][index] = card;
                tileCards[tileIndex] = card;
                card.Button.onClick.AddListener(() => Select(card, kind, tileIndex));
            }
        }

        private void CreateShape()
        {
            var frame = Box(stackArea, "Shape frame", At(.047f, .389f, .953f, .832f));
            var square = Box(frame, "Square tile area", Fill);
            var fitter = square.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            float halfTile = ShapeLevel.TileSize * .5f;
            for (int index = 0; index < shape.Length; index++)
            {
                int tileIndex = index;
                ShapeTile tile = shape[index];
                float x = .5f + tile.X * ShapeLevel.Pitch;
                float y = .5f + tile.Y * ShapeLevel.Pitch;
                var bounds = At(x - halfTile, y - halfTile, x + halfTile, y + halfTile);
                CardDisplay card = CreateTile(square, $"Tile {index + 1} / {tile.Kind}", tile.Kind, bounds);
                card.Shade = card.Button.gameObject.AddComponent<CanvasGroup>();
                tileCards[index] = card;
                card.Button.onClick.AddListener(() => Select(card, tile.Kind, tileIndex));
            }
        }

        private CardDisplay CreateTile(Transform parent, string name, SupplyKind kind, Rect bounds)
        {
            Button button = MakeButton(parent, name, bounds, CoveredTile);
            if (tileFace != null)
            {
                button.image.sprite = tileFace;
                button.image.type = Image.Type.Simple;
            }
            var shadow = button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.08f, 0.14f, 0.13f, .48f);
            shadow.effectDistance = new Vector2(0, -9);
            return new CardDisplay
            {
                Button = button,
                Icon = SupplyIcon(button.transform, kind, At(.105f, .12f, .895f, .935f))
            };
        }

        private void Select(CardDisplay card, SupplyKind kind, int tile)
        {
            int packedBefore = board.CratesPacked;
            int slot = board.Tray.Count;
            if (flyingCard != null || !board.TrySelect(tile))
                return;

            if (board.Status == PackingStatus.Won && campusProgress.Complete(level))
            {
                deliveryEarned = level <= CampusProgress.DeliveriesNeeded;
                PlayerPrefs.SetInt(CompletedLevelKey, campusProgress.HighestCompleted);
                PlayerPrefs.Save();
            }

            ClearHighlight();
            var source = (RectTransform)card.Button.transform;
            guideMarker.gameObject.SetActive(false);
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
                    float arc = Mathf.Sin(progress * Mathf.PI);
                    flyingCard.localPosition = Vector3.LerpUnclamped(from, to, progress) + Vector3.up * (55f * arc);
                    flyingCard.localRotation = Quaternion.Euler(0f, 0f, 10f * arc);
                    flyingCard.sizeDelta = Vector2.LerpUnclamped(startSize, endSize, progress);
                });
        }

        private void CompleteSelection(int packedBefore)
        {
            RemoveFlyingCard();
            if (board.CratesPacked > packedBefore)
                lastPackAt = Time.unscaledTime;
            if (board.Status != PackingStatus.Playing)
                SetFeedback("", Ink);
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
            SetSettingsOpen(false);
            CancelFlight();
            board.Reset();
            lastPackAt = -1f;
            crateBadge.localScale = Vector3.one;
            deliveryEarned = false;
            SetFeedback("", Ink);
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
            RefreshResult();
            hintButton.gameObject.SetActive(level > 1 && board.Status == PackingStatus.Playing);
            if (shape == null)
                RefreshColumns();
            else
                RefreshShape();

            RefreshTray();
            crateCount.text = $"{board.CratesPacked} / {board.TotalCrates}";
            trayCount.text = $"{board.Tray.Count} / {PackingBoard.TrayCapacity}";
            trayCount.color = board.Tray.Count >= 6 ? Peach : Gold;
            RefreshOpeningGuide();
        }

        private void RefreshResult()
        {
            bool ended = board.Status != PackingStatus.Playing;
            if (ended && !completionCard.gameObject.activeSelf)
            {
                resultOpenedAt = Time.unscaledTime;
                resultPanel.localScale = Vector3.one * .84f;
            }
            completionCard.gameObject.SetActive(ended);
            if (!ended)
                return;

            bool lost = board.Status == PackingStatus.Lost;
            bool finalLevel = level == ShapeLevel.Last;
            if (crateArtwork != null)
                crateArtwork.gameObject.SetActive(!lost && !deliveryEarned);
            if (retryArtwork != null)
                retryArtwork.gameObject.SetActive(lost);
            ShowBoothStage(resultBooth, deliveryEarned ? campusProgress.Deliveries : -1);

            if (lost)
            {
                completionTitle.text = "Tray full!";
                completionResult.text = "Pack a trio to free up space.";
                completionAction.text = "Try Again";
                return;
            }

            completionTitle.text = finalLevel ? "Event ready!" : "Nicely packed!";
            completionResult.text = deliveryEarned
                ? campusProgress.Deliveries == CampusProgress.DeliveriesNeeded
                    ? "Festival stand ready!"
                    : $"Stand delivery {campusProgress.Deliveries}/{CampusProgress.DeliveriesNeeded}"
                : board.TotalCrates == 1 ? "1 crate packed" : $"{board.TotalCrates} crates packed";
            completionAction.text = finalLevel ? "Main Menu" : "Next Level";
        }

        private void RefreshColumns()
        {
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
                    Place(card.Icon, ready ? At(.105f, .12f, .895f, .935f) : At(.06f, .616f, .76f, .985f));
                }
            }
        }

        private void RefreshShape()
        {
            for (int index = 0; index < shape.Length; index++)
            {
                CardDisplay card = tileCards[index];
                bool ready = board.CanSelect(index);
                bool visible = board.IsPresent(index) && !board.IsPresent(shape[index].FullCover);
                card.Button.gameObject.SetActive(visible);
                card.Button.interactable = ready;
                card.Button.image.raycastTarget = ready;
                card.Button.image.color = ready ? Paper : Behind;
                // Dim the whole blocked tile while leaving its visible edges recognizable.
                card.Shade.alpha = ready ? 1f : .72f;
            }
        }

        private void RefreshTray()
        {
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

        }

        private void RefreshOpeningGuide()
        {
            ClearHighlight();

            var hint = level == 1 ? OpeningLevel.NotebookHint(layout, board) : null;
            guide.transform.parent.gameObject.SetActive(hint.HasValue);
            guideMarker.gameObject.SetActive(hint.HasValue);
            if (!hint.HasValue)
                return;

            int column = hint.Value.Column;
            highlightedTile = cards[column][board.FrontIndex(column)].HintOutline;
            highlightedTile.enabled = true;
            Rect tile = CardBounds(column, 0);
            Place(guideMarker, At(tile.center.x - .07f, tile.yMax + .007f, tile.center.x + .07f, tile.yMax + .064f));
        }

        private void ShowMatchHint()
        {
            if (flyingCard != null || board.Status != PackingStatus.Playing)
                return;

            ClearHighlight();
            int tile = board.FindMatchHint();
            if (tile < 0)
            {
                SetFeedback("No open match yet", Ink);
                return;
            }

            CardDisplay card = tileCards[tile];
            if (card.HintOutline == null)
                card.HintOutline = CreateHintOutline(card);
            highlightedTile = card.HintOutline;
            highlightedTile.enabled = true;
            SetFeedback("", Ink);
        }

        private static Outline CreateHintOutline(CardDisplay card)
        {
            var outline = card.Button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(255, 181, 45, 255);
            outline.effectDistance = new Vector2(13, 13);
            outline.enabled = false;
            return outline;
        }

        private void ClearHighlight()
        {
            if (highlightedTile != null)
                highlightedTile.enabled = false;
            highlightedTile = null;
        }

        private Rect CardBounds(int column, int depth)
        {
            var bounds = StackTileLayout.Bounds(column, depth);
            if (level == 1)
                return At(bounds.Left, .479f, bounds.Right, .607f);

            return At(bounds.Left, bounds.Bottom, bounds.Right, bounds.Top);
        }

        private RectTransform DrawLock(Transform parent, Rect bounds)
        {
            var mark = Box(parent, "Locked", bounds);
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
            bool primary = faceColor == (Color)Gold;
            return MakeArtButton(parent, name, bounds, primary ? "next-level" : "mint-button",
                primary ? NextButtonArtworkCrop : MintButtonArtworkCrop, faceColor);
        }

        private Button MakeArtButton(Transform parent, string name, Rect bounds, string resource, Rect crop, Color fallback)
        {
            Button button = MakeButton(parent, name, bounds, Color.clear);
            RawImage art = AddThemeArt(button.transform, "Button artwork", resource, Fill, crop);
            if (art != null)
                button.targetGraphic = art;
            else
                button.image.color = fallback;
            return button;
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
            int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool detail = false)
        {
            var label = Box(parent, objectName, bounds).gameObject.AddComponent<Text>();
            label.font = detail ? detailFont : font;
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.RoundToInt(size * .73f);
            label.resizeTextMaxSize = size;
            label.fontStyle = label.font == legacyFont && !detail ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = alignment;
            label.alignByGeometry = alignment == TextAnchor.MiddleCenter;
            label.color = color;
            label.text = value;
            label.raycastTarget = false;
            return label;
        }
    }
}
