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
    public sealed partial class PrototypeBoardView : MonoBehaviour
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
        private static readonly string[] StopNames =
        {
            "First Crate", "Unpack", "Supply Run", "Badge Day", "Packing Rush",
            "Stack Up", "Pyramid", "Hidden One", "Double Deck", "Star"
        };
        // Ten stops spaced evenly along the painted path, bottom gate to main hall.
        private static readonly Vector2[] TrailStops =
        {
            new Vector2(.490f, .219f), new Vector2(.370f, .330f), new Vector2(.266f, .439f),
            new Vector2(.531f, .466f), new Vector2(.768f, .524f), new Vector2(.534f, .587f),
            new Vector2(.277f, .636f), new Vector2(.420f, .712f), new Vector2(.689f, .741f),
            new Vector2(.536f, .806f)
        };
        private static readonly Vector2[][] TrailBends =
        {
            new[] { new Vector2(.490f, .300f) },
            new[] { new Vector2(.325f, .342f), new Vector2(.225f, .394f) },
            new[] { new Vector2(.510f, .464f) },
            new[] { new Vector2(.710f, .484f) },
            new[] { new Vector2(.723f, .562f) },
            new[] { new Vector2(.465f, .596f), new Vector2(.295f, .625f) },
            new[] { new Vector2(.235f, .662f), new Vector2(.325f, .704f) },
            new[] { new Vector2(.499f, .718f), new Vector2(.668f, .736f) },
            new[] { new Vector2(.727f, .750f), new Vector2(.706f, .778f), new Vector2(.635f, .800f) }
        };

        // Blocked tiles read as the same tile resting in shadow, not as a hole in the board.
        private static readonly Color32 BlockedFace = new Color32(150, 144, 131, 255);
        private static readonly Color32 BlockedIcon = new Color32(128, 125, 118, 255);
        private static readonly Color32 LockCutout = new Color32(147, 137, 117, 255);
        private static readonly Color32 PlayMat = new Color32(18, 58, 54, 132);
        private static readonly Color32 SlotAlarm = new Color32(196, 62, 44, 215);
        private const float FlightTime = .3f;
        private const float StackPeek = .11f;

        private CardDisplay[][] cards;
        private CardDisplay[] tileCards;
        private readonly Image[] slots = new Image[PackingBoard.TrayCapacity];
        private readonly Dictionary<SupplyKind, Sprite> supplyArt = new Dictionary<SupplyKind, Sprite>();
        private readonly List<Sprite> generatedArt = new List<Sprite>();
        private readonly List<Texture2D> generatedTextures = new List<Texture2D>();
        private readonly List<TrayItem> trayItems = new List<TrayItem>();
        private readonly List<MotionHandle> effectMotions = new List<MotionHandle>();
        private RectTransform trayGroup;
        private RectTransform trayLayer;
        private RectTransform effectsLayer;
        private RectTransform crateIcon;
        private Sprite glowSprite;
        private Sprite sparkleSprite;
        private int activeMerges;
        private int displayedCrates;
        private int shownTrayCount = -1;
        private float resultDueAt = -1f;
        private float entranceEndsAt;
        private CardDisplay highlightedCard;
        private MotionHandle screenFade;
        private MotionHandle settingsFade;
        private RectTransform settingsDialog;
        private CanvasGroup settingsGroup;
        private Image completionTint;
        private Image homeGlow;
        private readonly List<RectTransform> homeTwinkles = new List<RectTransform>();
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
            public Image IconImage;
            public RectTransform Lock;
            public Outline HintOutline;
            public bool? WasReady;
            public MotionHandle Anim;
            public MotionHandle Move;
            // Only set for a tile that starts directly under another one.
            public Rect Home;
            public Rect Tucked;
        }

        // The tray is drawn from its own list so items can fly, slide and merge while the
        // rules have already moved on. Rapid taps never wait for an animation.
        private sealed class TrayItem
        {
            public RectTransform Rect;
            public SupplyKind Kind;
            public bool Flying;
            public float Start;
            public Vector2 From;
            public Vector2 FromSize;
            public bool CompletesTrio;
            public MotionHandle Punch;
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
            titleFont = Resources.Load<Font>("CampusCrates/Theme/Fredoka-Bold") ?? font;
            var tileTexture = Resources.Load<Texture2D>("CampusCrates/Theme/tile-face");
            if (tileTexture != null)
            {
                tileFace = Sprite.Create(tileTexture, new Rect(0f, 0f, tileTexture.width, tileTexture.height),
                    new Vector2(.5f, .5f));
                generatedArt.Add(tileFace);
            }
            CreateRoundedTile();
            CreateEffectSprites();
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
            LoadMeta();
            BuildHome();
            BuildMap();
            BuildGame();
            BuildRoom();
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
                guideArrow.anchoredPosition = Vector2.up * (7f * Mathf.Abs(Mathf.Sin(time * 4.2f)));

            if (homeCrate != null && homeScreen.gameObject.activeSelf)
                AnimateHome(time);

            if (mapScreen.gameObject.activeSelf)
                AnimateTrail(time);
            UpdateCoins();
            if (roomScreen.gameObject.activeSelf)
                AnimateRoom(time);

            if (!gameScreen.gameObject.activeSelf)
                return;

            UpdateTray(time);
            PulseHighlight(time);
            if (lastPackAt >= 0f)
            {
                float t = Mathf.Clamp01((time - lastPackAt) / .38f);
                crateBadge.localScale = Vector3.one * (1f + .2f * Mathf.Sin(t * Mathf.PI) * (1f - .4f * t));
                if (t >= 1f)
                {
                    crateBadge.localScale = Vector3.one;
                    lastPackAt = -1f;
                }
            }
            UpdateResultGate(time);
        }

        private void AnimateHome(float time)
        {
            homeCrate.anchoredPosition = Vector2.up * (9f * Mathf.Sin(time * 1.9f));
            homeCrate.localRotation = Quaternion.Euler(0f, 0f, 1.6f * Mathf.Sin(time * 1.6f));
            if (homeGlow != null)
            {
                float breathe = .5f + .5f * Mathf.Sin(time * 1.9f);
                homeGlow.rectTransform.localScale = Vector3.one * (.94f + .1f * breathe);
                homeGlow.color = new Color(1f, .92f, .66f, .42f + .2f * breathe);
            }

            for (int index = 0; index < homeTwinkles.Count; index++)
            {
                float phase = Mathf.Repeat(time * .55f + index * .27f, 1f);
                float pop = Mathf.Sin(Mathf.Clamp01(phase / .35f) * Mathf.PI);
                homeTwinkles[index].localScale = Vector3.one * pop;
                homeTwinkles[index].localRotation = Quaternion.Euler(0f, 0f, phase * 90f);
            }
        }

        private void PulseHighlight(float time)
        {
            if (highlightedCard == null || time < entranceEndsAt)
                return;

            float wave = .5f + .5f * Mathf.Sin(time * 6.5f);
            highlightedCard.Button.transform.localScale = Vector3.one * (1f + .05f * wave);
            Color glow = highlightedCard.HintOutline.effectColor;
            glow.a = .45f + .55f * wave;
            highlightedCard.HintOutline.effectColor = glow;
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
            if (AddThemeArt(homeScreen, "Campus Crates wordmark", "wordmark", At(.050f, .690f, .950f, .925f), Fill) == null)
            {
                var title = Label(homeScreen, "Game title", "Campus\nCrates", At(.06f, .718f, .94f, .947f), 107, Paper);
                title.lineSpacing = .81f;
                AddShade(title.gameObject, new Vector2(0, -6));
            }

            homeGlow = Panel(homeScreen, "Warm crate glow", At(-.08f, .25f, 1.08f, .75f), new Color(1f, .92f, .66f, .5f));
            homeGlow.sprite = glowSprite;
            Vector2[] twinkles = { new Vector2(.20f, .62f), new Vector2(.83f, .58f), new Vector2(.16f, .40f), new Vector2(.86f, .37f) };
            foreach (Vector2 spot in twinkles)
            {
                Image twinkle = Panel(homeScreen, "Twinkle", Centered(spot, .07f, .07f * 752f / 1280f), new Color(1f, .97f, .82f, .95f));
                twinkle.sprite = sparkleSprite;
                twinkle.preserveAspect = true;
                homeTwinkles.Add(twinkle.rectTransform);
            }

            homeCrate = Box(homeScreen, "Floating event crate", At(.13f, .320f, .87f, .674f));
            if (AddThemeArt(homeCrate, "Event crate", "event-crate", Fill, Fill) == null)
            {
                var crate = Panel(homeCrate, "Crate illustration", At(.12f, .1f, .88f, .9f), Gold, true).rectTransform;
                Panel(crate, "Packing tape", At(.43f, .02f, .57f, .98f), Paper);
                SupplyIcon(crate, SupplyKind.Notebook, At(.12f, .16f, .44f, .86f));
                SupplyIcon(crate, SupplyKind.Pencil, At(.56f, .16f, .88f, .86f));
            }

            Button play = MakePackingButton(homeScreen, "Play", At(.12f, .176f, .88f, .282f), Gold);
            ButtonText(play, "Play", At(.11f, .40f, .89f, .88f), 58, true);
            homeLevelLabel = Headline(play.transform, "Current level", "", At(.11f, .19f, .89f, .46f), 28, TextWhite, StrokeBrown, 2.5f);
            play.onClick.AddListener(StartFromHome);

            BuildHomeMeta();
        }

        private void BuildMap()
        {
            Panel(mapScreen, "Warm map wash", Fill, new Color32(16, 51, 46, 190));
            var composition = Box(mapScreen, "Centered trail composition", At(.018f, .012f, .982f, .99f));
            var column = Box(composition, "Trail column", Fill);
            var fitter = column.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = .515f;

            var mapPlate = Sticker(column, "Trail print", At(.005f, .150f, .995f, .897f), Surface.Panel).rectTransform;
            var trailArt = AddThemeArt(mapPlate, "Illustrated campus trail", "campus-trail",
                At(.030f, .040f, .970f, .975f), Fill, AspectRatioFitter.AspectMode.EnvelopeParent);
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
                    Centered(TrailStops[index], .136f, .136f * TrailArtAspect), "trail-pin", Fill, Gold, press: false);
                Headline(stop.transform, "Stop number", number.ToString(), At(.17f, .20f, .83f, .89f), 54, StrokeTeal, TextWhite, 3f);
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
            Headline(back.transform, "Back arrow", "‹", At(.17f, .20f, .83f, .89f), 58, Ink, TextWhite, 3f).font = font;
            back.onClick.AddListener(ShowHome);
            Ribbon(column, "Map ribbon", "Festival Trail", At(.16f, .900f, .84f, .999f), 50);

            var deliveryTag = Sticker(column, "Festival ticket", At(.005f, .012f, .995f, .148f), Surface.Panel).rectTransform;
            mapBooth = BuildBoothArt(deliveryTag, At(.035f, .09f, .238f, .91f));
            nextStop = Label(deliveryTag, "Next stop", "", At(.270f, .46f, .944f, .86f), 37, Positive, TextAnchor.MiddleLeft);
            nextStop.font = titleFont;
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
            ShowScreen(mapScreen);
        }

        private void ShowScreen(RectTransform screen)
        {
            homeScreen.gameObject.SetActive(screen == homeScreen);
            mapScreen.gameObject.SetActive(screen == mapScreen);
            gameScreen.gameObject.SetActive(screen == gameScreen);
            if (roomScreen != null)
                roomScreen.gameObject.SetActive(screen == roomScreen);

            var group = screen.GetComponent<CanvasGroup>();
            if (group == null)
                group = screen.gameObject.AddComponent<CanvasGroup>();
            screenFade.TryCancel();
            group.alpha = 0f;
            screenFade = LMotion.Create(0f, 1f, .24f)
                .WithEase(Ease.OutCubic)
                .Bind(t =>
                {
                    group.alpha = t;
                    screen.localScale = Vector3.one * Mathf.Lerp(1.035f, 1f, t);
                })
                .AddTo(screen.gameObject);
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
            levelLabel = Headline(gameScreen, "Level", "", At(.307f, .926f, .693f, .98f), 37, TextWhite, StrokeTeal, 3f);
            Button gear = MakeButton(gameScreen, "Settings", At(.764f, .908f, .956f, .999f), Color.clear);
            RawImage gearArt = AddThemeArt(gear.transform, "Settings gear", "settings", Fill, Fill);
            if (gearArt != null)
                gear.targetGraphic = gearArt;
            else
                Label(gear.transform, "Settings label", "Settings", Fill, 24, Paper);
            gear.onClick.AddListener(() => SetSettingsOpen(true));

            crateBadge = Sticker(gameScreen, "Packed crates badge", At(.330f, .834f, .670f, .911f)).rectTransform;
            RawImage crateArt = AddThemeArt(crateBadge, "Crate icon", "event-crate", At(.045f, -.055f, .49f, 1.06f), Fill);
            crateIcon = crateArt != null ? (RectTransform)crateArt.transform.parent : crateBadge;
            crateCount = Label(crateBadge, "Crates packed", "", At(.44f, .12f, .90f, .88f), 39, Positive);
            crateCount.font = titleFont;

            // A soft translucent mat separates the cream tiles from the sunny courtyard floor.
            Image mat = Panel(gameScreen, "Play mat", At(.022f, .372f, .978f, .846f), PlayMat, true);
            mat.pixelsPerUnitMultiplier = .38f;
            var matEdge = mat.gameObject.AddComponent<Outline>();
            matEdge.effectColor = new Color(1f, .97f, .88f, .22f);
            matEdge.effectDistance = new Vector2(3f, -3f);

            stackArea = Box(gameScreen, "Tile stacks", Fill);

            var guideTag = Sticker(gameScreen, "Opening guide", At(.16f, .752f, .84f, .822f));
            guide = Label(guideTag.transform, "Next move", "Tap 3 notebooks", At(.06f, .14f, .94f, .86f), 31, Ink);
            guide.font = titleFont;
            guideMarker = Box(gameScreen, "Guide marker", Fill);
            guideArrow = IconImage(guideMarker, "Guide arrow", "arrow", Fill).rectTransform;

            trayGroup = Box(gameScreen, "Tray group", Fill);
            Sticker(trayGroup, "Tray capacity tag", At(.745f, .328f, .945f, .388f), Surface.PillTeal);
            trayCount = Headline(trayGroup, "Tray capacity", "", At(.765f, .337f, .925f, .381f), 29, TextWhite, StrokeTeal, 2f);
            if (AddThemeArt(trayGroup, "Cardboard sorting tray", "packing-tray", At(.033f, .193f, .967f, .334f),
                TrayArtworkCrop, AspectRatioFitter.AspectMode.None) == null)
                Panel(trayGroup, "Tray", At(.04f, .202f, .96f, .333f), TableEdge, true);
            for (int index = 0; index < slots.Length; index++)
            {
                float left = .073f + index * .123f;
                slots[index] = Panel(trayGroup, $"Slot {index + 1}", At(left, .224f, left + .108f, .305f), TrayRecess, true);
            }
            trayLayer = Box(trayGroup, "Tray items", Fill);

            var note = Sticker(gameScreen, "Feedback surface", At(.23f, .088f, .77f, .156f));
            feedback = Label(note.transform, "Feedback", "", At(.08f, .14f, .92f, .86f), 30, Ink);
            feedback.font = titleFont;

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

            effectsLayer = Box(gameScreen, "Effects", Fill);
            BuildCompletionCard();
        }

        private void BuildCompletionCard()
        {
            completionTint = Panel(gameScreen, "Celebration tint", Fill, new Color32(18, 36, 34, 150));
            completionCard = completionTint.rectTransform;
            var card = Sticker(completionCard, "Packed result", At(.045f, .362f, .955f, .800f), Surface.Panel).rectTransform;
            resultPanel = card;

            Image warmth = Panel(card, "Illustration glow", At(.10f, .24f, .90f, .86f), new Color(1f, .84f, .52f, .75f));
            warmth.sprite = glowSprite;
            Rect artBounds = At(.21f, .375f, .79f, .800f);
            crateArtwork = AddThemeArt(card, "Packed event crate", "event-crate", artBounds, Fill);
            retryArtwork = AddThemeArt(card, "Overflowing tray", "tray-full", artBounds, Fill);
            resultBooth = BuildBoothArt(card, artBounds);
            var leftSparkle = Panel(card, "Left sparkle", At(.10f, .58f, .19f, .69f), new Color32(255, 190, 160, 255));
            leftSparkle.sprite = sparkleSprite;
            leftSparkle.preserveAspect = true;
            var rightSparkle = Panel(card, "Right sparkle", At(.80f, .66f, .87f, .75f), new Color32(255, 220, 120, 255));
            rightSparkle.sprite = sparkleSprite;
            rightSparkle.preserveAspect = true;
            completionResult = Label(card, "Result", "", At(.07f, .300f, .93f, .380f), 32, Ink);
            completionResult.font = titleFont;

            Button next = MakePackingButton(card, "Continue", At(.10f, .040f, .90f, .200f), Gold);
            completionAction = ButtonText(next, "", At(.08f, .14f, .92f, .92f), 42, true);
            next.onClick.AddListener(ContinueFromResults);
            completionTitle = Ribbon(card, "Result ribbon", "", At(-.07f, .80f, 1.07f, 1.07f), 50);
            BuildResultReward();
        }

        private void BuildSettings()
        {
            var backdrop = Panel(viewport, "Settings overlay", Fill, new Color32(12, 30, 33, 179));
            backdrop.raycastTarget = true;
            settings = backdrop.rectTransform;
            settingsGroup = backdrop.gameObject.AddComponent<CanvasGroup>();
            var dialog = Sticker(settings, "Settings dialog", At(.095f, .286f, .905f, .723f), Surface.Panel).rectTransform;
            settingsDialog = dialog;
            Ribbon(dialog, "Settings ribbon", "Settings", At(-.08f, .80f, 1.08f, 1.10f), 50);

            Button resume = MakePackingButton(dialog, "Resume", At(.12f, .56f, .88f, .76f), Gold);
            ButtonText(resume, "Resume", At(.08f, .14f, .92f, .90f), 40, true);
            resume.onClick.AddListener(() => SetSettingsOpen(false));

            Button restart = MakePackingButton(dialog, "Restart", At(.12f, .33f, .88f, .52f), Positive);
            ButtonText(restart, "Restart", At(.08f, .14f, .92f, .90f), 38, false);
            restart.onClick.AddListener(Restart);

            Button home = MakePackingButton(dialog, "Main Menu", At(.12f, .10f, .88f, .29f), Positive);
            ButtonText(home, "Main Menu", At(.08f, .14f, .92f, .90f), 38, false);
            home.onClick.AddListener(ShowHome);
        }

        private void SetSettingsOpen(bool open)
        {
            bool opening = open && !settings.gameObject.activeSelf;
            settings.gameObject.SetActive(open);
            gameControls.interactable = !open;
            gameControls.blocksRaycasts = !open;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            settingsFade.TryCancel();
            if (!opening)
                return;
            settingsGroup.alpha = 0f;
            settingsDialog.localScale = Vector3.one * .86f;
            settingsFade = LMotion.Create(0f, 1f, .32f)
                .Bind(t =>
                {
                    settingsGroup.alpha = Mathf.Clamp01(t * 3f);
                    settingsDialog.localScale = Vector3.one * Mathf.LerpUnclamped(.86f, 1f, OutBack(t));
                })
                .AddTo(settings.gameObject);
        }

        private void StartFromHome()
        {
            StartLevel(campusProgress.UnlockedLevel);
        }

        private void StartLevel(int number)
        {
            if (number > campusProgress.UnlockedLevel)
                return;

            ShowScreen(gameScreen);
            if (number == level)
                Restart();
            else
                LoadLevel(number);
        }

        private void LoadLevel(int number)
        {
            ClearTrayAndEffects();
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
            homeLevelLabel.text = $"Level {campusProgress.UnlockedLevel}";
            RefreshHomeMeta();
            ShowScreen(homeScreen);
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

            // A tile lying exactly under another would be invisible. Pull the pair apart a
            // little so the lower one shows as a dark edge and the two read as a stack.
            float peek = ShapeLevel.TileSize * StackPeek;
            var sitsOnATile = new bool[shape.Length];
            foreach (ShapeTile under in shape)
                if (under.FullCover >= 0)
                    sitsOnATile[under.FullCover] = true;

            float halfTile = ShapeLevel.TileSize * .5f;
            for (int index = 0; index < shape.Length; index++)
            {
                int tileIndex = index;
                ShapeTile tile = shape[index];
                float x = .5f + tile.X * ShapeLevel.Pitch;
                float y = .5f + tile.Y * ShapeLevel.Pitch;
                if (sitsOnATile[index])
                    y += peek;
                var bounds = At(x - halfTile, y - halfTile, x + halfTile, y + halfTile);
                CardDisplay card = CreateTile(square, $"Tile {index + 1} / {tile.Kind}", tile.Kind, bounds);
                card.Home = bounds;
                card.Tucked = At(x - halfTile, y - halfTile - peek, x + halfTile, y + halfTile - peek);
                tileCards[index] = card;
                card.Button.onClick.AddListener(() => Select(card, tile.Kind, tileIndex));
            }
        }

        private CardDisplay CreateTile(Transform parent, string name, SupplyKind kind, Rect bounds)
        {
            Button button = MakeButton(parent, name, bounds, BlockedFace, press: false);
            if (tileFace != null)
            {
                button.image.sprite = tileFace;
                button.image.type = Image.Type.Simple;
            }
            var shadow = button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.08f, 0.14f, 0.13f, .48f);
            shadow.effectDistance = new Vector2(0, -9);
            RectTransform icon = SupplyIcon(button.transform, kind, At(.105f, .12f, .895f, .935f));
            return new CardDisplay
            {
                Button = button,
                Icon = icon,
                IconImage = icon.GetComponentInChildren<Image>()
            };
        }

        private void Select(CardDisplay card, SupplyKind kind, int tile)
        {
            int packedBefore = board.CratesPacked;
            if (!board.TrySelect(tile))
                return;

            if (board.Status == PackingStatus.Won)
            {
                bool firstClear = campusProgress.Complete(level);
                if (firstClear)
                {
                    deliveryEarned = level <= CampusProgress.DeliveriesNeeded;
                    PlayerPrefs.SetInt(CompletedLevelKey, campusProgress.HighestCompleted);
                }
                AwardLevelCoins(firstClear);
            }

            ClearHighlight();
            guideMarker.gameObject.SetActive(false);
            var source = (RectTransform)card.Button.transform;
            Vector2 from = trayLayer.InverseTransformPoint(source.TransformPoint(source.rect.center));
            SpawnTilePop(source);

            var item = new TrayItem
            {
                Kind = kind,
                Flying = true,
                Start = Time.unscaledTime,
                From = from,
                FromSize = source.rect.size * .86f,
                CompletesTrio = board.CratesPacked > packedBefore
            };
            item.Rect = Box(trayLayer, kind + " in tray", Fill);
            item.Rect.anchorMin = item.Rect.anchorMax = new Vector2(.5f, .5f);
            item.Rect.sizeDelta = item.FromSize;
            item.Rect.localPosition = from;
            SupplyIcon(item.Rect, kind, At(-.05f, -.05f, 1.05f, 1.05f));

            // Like kinds sit together, so a forming trio is easy to read at a glance.
            int insert = trayItems.FindLastIndex(other => other.Kind == kind);
            trayItems.Insert(insert < 0 ? trayItems.Count : insert + 1, item);

            if (board.Status == PackingStatus.Playing && board.Tray.Count == PackingBoard.TrayCapacity - 1)
                SetFeedback("1 slot left", Error);
            else
                SetFeedback("", Ink);
            Refresh();
        }

        private void Restart()
        {
            SetSettingsOpen(false);
            ClearTrayAndEffects();
            board.Reset();
            lastPackAt = -1f;
            displayedCrates = 0;
            resultDueAt = -1f;
            crateBadge.localScale = Vector3.one;
            trayGroup.anchoredPosition = Vector2.zero;
            foreach (Image slot in slots)
                slot.color = TrayRecess;
            deliveryEarned = false;
            SetFeedback("", Ink);
            if (tileCards != null)
                foreach (CardDisplay card in tileCards)
                    if (card != null)
                        card.WasReady = null;
            Refresh(instant: true);
            PlayEntrance();
        }

        private void PlayEntrance()
        {
            int order = 0;
            foreach (CardDisplay card in tileCards)
            {
                if (card == null || !card.Button.gameObject.activeSelf)
                    continue;
                var rect = (RectTransform)card.Button.transform;
                rect.localScale = Vector3.zero;
                card.Anim.TryCancel();
                card.Anim = LMotion.Create(0f, 1f, .36f)
                    .WithEase(Ease.OutBack)
                    .WithDelay(.08f + order * .03f)
                    .Bind(scale => rect.localScale = Vector3.one * scale)
                    .AddTo(rect.gameObject);
                order++;
            }
            entranceEndsAt = Time.unscaledTime + .08f + order * .03f + .36f;
        }

        // ---------- Tray: flight, sliding, merging ----------

        private Vector2 SlotPosition(int index)
        {
            Vector2 Center(int slot)
            {
                RectTransform rect = slots[slot].rectTransform;
                return trayLayer.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            }

            if (index < slots.Length)
                return Center(index);
            Vector2 last = Center(slots.Length - 1);
            return last + (last - Center(slots.Length - 2)) * (index - slots.Length + 1);
        }

        private void UpdateTray(float time)
        {
            if (trayItems.Count == 0)
            {
                UpdateTrayCount();
                return;
            }

            Vector2 slotSize = slots[0].rectTransform.rect.size;
            float follow = 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime);
            List<TrayItem> landed = null;
            for (int index = 0; index < trayItems.Count; index++)
            {
                TrayItem item = trayItems[index];
                Vector2 target = SlotPosition(index);
                if (!item.Flying)
                {
                    item.Rect.localPosition = Vector2.Lerp(item.Rect.localPosition, target, follow);
                    continue;
                }

                float t = Mathf.Clamp01((time - item.Start) / FlightTime);
                float eased = OutCubic(t);
                float arc = Mathf.Sin(t * Mathf.PI);
                item.Rect.localPosition = Vector2.LerpUnclamped(item.From, target, eased) + Vector2.up * (70f * arc);
                item.Rect.sizeDelta = Vector2.LerpUnclamped(item.FromSize, slotSize, eased);
                item.Rect.localRotation = Quaternion.Euler(0f, 0f, -14f * arc);
                if (t < 1f)
                    continue;

                item.Flying = false;
                item.Rect.localRotation = Quaternion.identity;
                (landed ??= new List<TrayItem>()).Add(item);
            }

            if (landed != null)
                foreach (TrayItem item in landed)
                    Land(item);
            UpdateTrayCount();
        }

        private void UpdateTrayCount()
        {
            if (shownTrayCount == trayItems.Count)
                return;
            shownTrayCount = trayItems.Count;
            trayCount.text = $"{shownTrayCount} / {PackingBoard.TrayCapacity}";
            trayCount.color = shownTrayCount >= PackingBoard.TrayCapacity - 1 ? new Color32(255, 196, 170, 255) : TextWhite;
        }

        private void Land(TrayItem item)
        {
            RectTransform rect = item.Rect;
            item.Punch.TryCancel();
            item.Punch = LMotion.Create(0f, 1f, .3f)
                .Bind(t => rect.localScale = new Vector3(1.24f - .24f * OutBack(t), .8f + .2f * OutBack(t), 1f))
                .AddTo(rect.gameObject);

            if (item.CompletesTrio)
                StartMerge(item);
        }

        private void StartMerge(TrayItem completing)
        {
            var trio = new List<TrayItem> { completing };
            foreach (TrayItem other in trayItems)
                if (other != completing && other.Kind == completing.Kind && !other.Flying && trio.Count < 3)
                    trio.Add(other);

            var starts = new Vector2[trio.Count];
            Vector2 center = Vector2.zero;
            for (int index = 0; index < trio.Count; index++)
            {
                trayItems.Remove(trio[index]);
                trio[index].Punch.TryCancel();
                trio[index].Rect.SetAsLastSibling();
                starts[index] = trio[index].Rect.localPosition;
                center += starts[index];
            }
            center = center / trio.Count + Vector2.up * 30f;

            activeMerges++;
            SupplyKind kind = completing.Kind;
            effectMotions.Add(LMotion.Create(0f, 1f, .36f)
                .WithOnComplete(() =>
                {
                    foreach (TrayItem item in trio)
                        if (item.Rect != null)
                            Destroy(item.Rect.gameObject);
                    Vector2 burstAt = effectsLayer.InverseTransformPoint(trayLayer.TransformPoint(center));
                    Burst(burstAt, KindColor(kind), 1f);
                    FlyCrateToBadge(burstAt);
                })
                .Bind(p =>
                {
                    for (int index = 0; index < trio.Count; index++)
                    {
                        RectTransform rect = trio[index].Rect;
                        if (p < .45f)
                        {
                            float q = OutCubic(p / .45f);
                            rect.localPosition = Vector2.LerpUnclamped(starts[index], center, q);
                            rect.localScale = Vector3.one * (1f + .2f * q);
                        }
                        else
                        {
                            float q = (p - .45f) / .55f;
                            rect.localPosition = center;
                            rect.localScale = Vector3.one * Mathf.Max(0f, 1.2f * (1f - InBack(q)));
                        }
                    }
                }));
        }

        private void FlyCrateToBadge(Vector2 from)
        {
            Vector2 to = effectsLayer.InverseTransformPoint(crateIcon.TransformPoint(crateIcon.rect.center));
            var crate = Box(effectsLayer, "Packed crate", Fill);
            crate.anchorMin = crate.anchorMax = new Vector2(.5f, .5f);
            crate.sizeDelta = Vector2.one * 120f;
            crate.localPosition = from;
            if (AddThemeArt(crate, "Crate", "event-crate", Fill, Fill) == null)
                Panel(crate, "Crate", At(.15f, .15f, .85f, .85f), Gold, true);

            effectMotions.Add(LMotion.Create(0f, 1f, .52f)
                .WithOnComplete(() =>
                {
                    Destroy(crate.gameObject);
                    displayedCrates = Mathf.Min(displayedCrates + 1, board.CratesPacked);
                    crateCount.text = $"{displayedCrates} / {board.TotalCrates}";
                    lastPackAt = Time.unscaledTime;
                    Burst(to, Gold, .55f);
                    activeMerges = Mathf.Max(0, activeMerges - 1);
                })
                .Bind(t =>
                {
                    float eased = InOutCubic(t);
                    crate.localPosition = Vector2.LerpUnclamped(from, to, eased) + Vector2.up * (110f * Mathf.Sin(t * Mathf.PI));
                    float grow = t < .25f ? OutBack(t / .25f) : Mathf.Lerp(1f, .55f, (t - .25f) / .75f);
                    crate.localScale = Vector3.one * Mathf.Max(0f, grow);
                    crate.localRotation = Quaternion.Euler(0f, 0f, 18f * Mathf.Sin(t * Mathf.PI * 2f));
                }));
        }

        private void SpawnTilePop(RectTransform source)
        {
            Vector2 at = effectsLayer.InverseTransformPoint(source.TransformPoint(source.rect.center));
            Image ghost = Panel(effectsLayer, "Tile pop", Fill, Color.white);
            ghost.sprite = tileFace != null ? tileFace : roundedTile;
            ghost.type = Image.Type.Simple;
            RectTransform rect = ghost.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = source.rect.size;
            rect.localPosition = at;
            effectMotions.Add(LMotion.Create(0f, 1f, .2f)
                .WithOnComplete(() => Destroy(rect.gameObject))
                .Bind(t =>
                {
                    rect.localScale = Vector3.one * (1f + .25f * OutCubic(t));
                    ghost.color = new Color(1f, 1f, 1f, 1f - t);
                }));
        }

        private void Burst(Vector2 at, Color tint, float size) => BurstIn(effectsLayer, effectMotions, at, tint, size);

        private void BurstIn(RectTransform layer, List<MotionHandle> motions, Vector2 at, Color tint, float size)
        {
            Image ring = Panel(layer, "Burst glow", Fill, tint);
            ring.sprite = glowSprite;
            RectTransform ringRect = ring.rectTransform;
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(.5f, .5f);
            ringRect.localPosition = at;
            motions.Add(LMotion.Create(0f, 1f, .45f)
                .WithOnComplete(() => Destroy(ringRect.gameObject))
                .Bind(t =>
                {
                    ringRect.sizeDelta = Vector2.one * Mathf.Lerp(70f, 300f, OutCubic(t)) * size;
                    ring.color = new Color(tint.r, tint.g, tint.b, .9f * (1f - t));
                }));

            int count = Mathf.RoundToInt(9 * size) + 1;
            for (int index = 0; index < count; index++)
            {
                Image spark = Panel(layer, "Sparkle", Fill, index % 2 == 0 ? Color.white : tint);
                spark.sprite = sparkleSprite;
                RectTransform rect = spark.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.localPosition = at;
                float angle = (index + Random.value * .6f) / count * Mathf.PI * 2f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float distance = Random.Range(85f, 150f) * size;
                float spin = Random.Range(-200f, 200f);
                float startSize = Random.Range(34f, 52f) * Mathf.Sqrt(size);
                Color color = spark.color;
                motions.Add(LMotion.Create(0f, 1f, Random.Range(.42f, .6f))
                    .WithOnComplete(() => Destroy(rect.gameObject))
                    .Bind(t =>
                    {
                        rect.localPosition = at + direction * (distance * OutCubic(t));
                        rect.sizeDelta = Vector2.one * startSize * (1f - .7f * t);
                        rect.localRotation = Quaternion.Euler(0f, 0f, spin * t);
                        spark.color = new Color(color.r, color.g, color.b, 1f - t * t);
                    }));
            }
        }

        private void ClearTrayAndEffects()
        {
            foreach (MotionHandle handle in effectMotions)
                handle.TryCancel();
            effectMotions.Clear();
            foreach (TrayItem item in trayItems)
                item.Punch.TryCancel();
            trayItems.Clear();
            activeMerges = 0;
            shownTrayCount = -1;
            if (trayLayer != null)
                foreach (Transform child in trayLayer)
                    Destroy(child.gameObject);
            if (effectsLayer != null)
                foreach (Transform child in effectsLayer)
                    Destroy(child.gameObject);
            if (completionCard != null)
                foreach (Transform child in completionCard)
                    if (child.name == "Confetti")
                        Destroy(child.gameObject);
        }

        private static Color KindColor(SupplyKind kind) => kind switch
        {
            SupplyKind.Notebook => new Color32(112, 214, 164, 255),
            SupplyKind.Pencil => new Color32(255, 204, 70, 255),
            SupplyKind.Cup => new Color32(196, 156, 246, 255),
            _ => new Color32(255, 128, 104, 255)
        };

        // ---------- Result ----------

        private void UpdateResultGate(float time)
        {
            if (board.Status == PackingStatus.Playing || completionCard.gameObject.activeSelf)
                return;
            if (activeMerges > 0 || trayItems.Exists(item => item.Flying))
                return;

            bool lost = board.Status == PackingStatus.Lost;
            if (resultDueAt < 0f)
            {
                resultDueAt = time + (lost ? .75f : .3f);
                if (lost)
                    ShakeTray();
                return;
            }
            if (time >= resultDueAt)
                OpenResult();
        }

        private void ShakeTray()
        {
            effectMotions.Add(LMotion.Create(0f, 1f, .55f)
                .WithOnComplete(() => trayGroup.anchoredPosition = Vector2.zero)
                .Bind(t =>
                {
                    trayGroup.anchoredPosition = new Vector2(Mathf.Sin(t * Mathf.PI * 9f) * 16f * (1f - t), 0f);
                    Color flash = Color.Lerp(TrayRecess, SlotAlarm, Mathf.Sin(Mathf.Min(1f, t * 1.4f) * Mathf.PI));
                    foreach (Image slot in slots)
                        slot.color = flash;
                }));
        }

        private void OpenResult()
        {
            bool lost = board.Status == PackingStatus.Lost;
            completionCard.gameObject.SetActive(true);
            RefreshResult();

            Color tint = completionTint.color;
            resultPanel.localScale = Vector3.one * .55f;
            effectMotions.Add(LMotion.Create(0f, 1f, .48f)
                .Bind(t =>
                {
                    completionTint.color = new Color(tint.r, tint.g, tint.b, tint.a * Mathf.Clamp01(t * 2.5f));
                    resultPanel.localScale = Vector3.one * Mathf.LerpUnclamped(.55f, 1f, OutBack(t));
                }));
            ShowResultReward(!lost);
            if (!lost)
                SpawnConfetti();
        }

        private void SpawnConfetti() => SpawnConfettiIn(completionCard, effectMotions);

        private void SpawnConfettiIn(RectTransform parent, List<MotionHandle> motions)
        {
            Color[] palette = { Mint, Gold, Lilac, Peach, new Color32(118, 200, 230, 255), Paper };
            Vector2 size = parent.rect.size;
            for (int index = 0; index < 46; index++)
            {
                Image piece = Panel(parent, "Confetti", Fill, palette[index % palette.Length], true);
                RectTransform rect = piece.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(Random.Range(14f, 22f), Random.Range(24f, 36f));
                Vector2 origin = new Vector2(Random.Range(-.08f, .08f) * size.x, size.y * .2f);
                rect.localPosition = origin;
                float angle = Random.Range(35f, 145f) * Mathf.Deg2Rad;
                Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(650f, 1250f);
                float spin = Random.Range(-720f, 720f);
                float flutter = Random.Range(6f, 12f);
                float duration = Random.Range(1.5f, 2.2f);
                motions.Add(LMotion.Create(0f, duration, duration)
                    .WithDelay(Random.Range(0f, .12f))
                    .WithOnComplete(() => Destroy(rect.gameObject))
                    .Bind(t =>
                    {
                        // Air drag keeps the pieces drifting instead of dropping like stones.
                        float drag = (1f - Mathf.Exp(-2.4f * t)) / 2.4f;
                        Vector2 position = origin + velocity * drag + Vector2.down * (520f * t * t);
                        position.x += Mathf.Sin(t * flutter) * 18f;
                        rect.localPosition = position;
                        rect.localRotation = Quaternion.Euler(0f, 0f, spin * t);
                        rect.localScale = new Vector3(Mathf.Cos(t * flutter), 1f, 1f);
                        piece.color = new Color(piece.color.r, piece.color.g, piece.color.b,
                            Mathf.Clamp01((duration - t) / .45f));
                    }));
            }
        }

        private void SetFeedback(string message, Color color)
        {
            feedback.text = message;
            feedback.color = color;
            feedback.transform.parent.gameObject.SetActive(message.Length != 0);
        }

        private void Refresh(bool instant = false)
        {
            if (board.Status == PackingStatus.Playing)
                completionCard.gameObject.SetActive(false);
            hintButton.gameObject.SetActive(level > 1 && board.Status == PackingStatus.Playing);
            if (shape == null)
                RefreshColumns(instant);
            else
                RefreshShape(instant);

            crateCount.text = $"{displayedCrates} / {board.TotalCrates}";
            UpdateTrayCount();
            RefreshOpeningGuide();
        }

        private void RefreshResult()
        {
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

        private void RefreshColumns(bool instant)
        {
            for (int column = 0; column < cards.Length; column++)
            {
                int front = board.FrontIndex(column);
                for (int index = 0; index < cards[column].Length; index++)
                {
                    CardDisplay card = cards[column][index];
                    bool visible = index >= front;
                    bool ready = index == front && board.Status == PackingStatus.Playing;
                    bool unlocked = index == front;
                    card.Button.gameObject.SetActive(visible);
                    card.Button.interactable = ready;
                    if (visible)
                        MoveCard(card, CardBounds(column, index - front), instant);
                    card.Lock.gameObject.SetActive(!unlocked);
                    Place(card.Icon, unlocked ? At(.105f, .12f, .895f, .935f) : At(.06f, .616f, .76f, .985f));
                    ApplyTileLook(card, unlocked, instant);
                }
            }
        }

        private void MoveCard(CardDisplay card, Rect bounds, bool instant)
        {
            var rect = (RectTransform)card.Button.transform;
            if (instant)
            {
                card.Move.TryCancel();
                Place(rect, bounds);
                return;
            }
            if (rect.anchorMin == bounds.min && rect.anchorMax == bounds.max)
                return;

            Vector2 fromMin = rect.anchorMin;
            Vector2 fromMax = rect.anchorMax;
            card.Move.TryCancel();
            card.Move = LMotion.Create(0f, 1f, .26f)
                .WithEase(Ease.OutBack)
                .Bind(t =>
                {
                    rect.anchorMin = Vector2.LerpUnclamped(fromMin, bounds.min, t);
                    rect.anchorMax = Vector2.LerpUnclamped(fromMax, bounds.max, t);
                })
                .AddTo(rect.gameObject);
        }

        private void RefreshShape(bool instant)
        {
            for (int index = 0; index < shape.Length; index++)
            {
                CardDisplay card = tileCards[index];
                bool ready = board.CanSelect(index);
                int cover = shape[index].FullCover;
                bool tucked = cover >= 0 && board.IsPresent(cover);
                card.Button.gameObject.SetActive(board.IsPresent(index));
                if (cover >= 0)
                {
                    // What the tile is stays a surprise until the one on top is taken.
                    card.Icon.gameObject.SetActive(!tucked);
                    MoveCard(card, tucked ? card.Tucked : card.Home, instant);
                }
                card.Button.interactable = ready;
                card.Button.image.raycastTarget = ready;
                ApplyTileLook(card, ready || (board.Status != PackingStatus.Playing && card.WasReady == true), instant);
            }
        }

        // Freed tiles warm up and hop once so the player notices what just opened.
        private void ApplyTileLook(CardDisplay card, bool open, bool instant)
        {
            Color face = open ? (Color)Paper : BlockedFace;
            Color icon = open ? Color.white : (Color)BlockedIcon;
            bool reveal = !instant && open && card.WasReady == false && card.Button.gameObject.activeSelf;
            card.WasReady = open;
            if (!reveal)
            {
                if (!card.Anim.IsActive() || instant)
                {
                    card.Button.image.color = face;
                    if (card.IconImage != null)
                        card.IconImage.color = icon;
                }
                return;
            }

            Image image = card.Button.image;
            Image iconImage = card.IconImage;
            Color fromFace = image.color;
            Color fromIcon = iconImage != null ? iconImage.color : Color.white;
            var rect = (RectTransform)card.Button.transform;
            card.Anim.TryCancel();
            card.Anim = LMotion.Create(0f, 1f, .32f)
                .WithOnComplete(() => rect.localScale = Vector3.one)
                .Bind(t =>
                {
                    float warm = OutCubic(Mathf.Min(1f, t * 1.8f));
                    image.color = Color.Lerp(fromFace, face, warm);
                    if (iconImage != null)
                        iconImage.color = Color.Lerp(fromIcon, icon, warm);
                    rect.localScale = Vector3.one * (1f + .11f * Mathf.Sin(t * Mathf.PI) * (1f - t * .5f));
                })
                .AddTo(rect.gameObject);
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
            Highlight(cards[column][board.FrontIndex(column)]);
            Rect tile = CardBounds(column, 0);
            Place(guideMarker, At(tile.center.x - .07f, tile.yMax + .007f, tile.center.x + .07f, tile.yMax + .064f));
        }

        private void ShowMatchHint()
        {
            if (board.Status != PackingStatus.Playing)
                return;

            ClearHighlight();
            int tile = board.FindMatchHint();
            if (tile < 0)
            {
                SetFeedback("No open match yet", Ink);
                return;
            }

            Highlight(tileCards[tile]);
            SetFeedback("", Ink);
        }

        private void Highlight(CardDisplay card)
        {
            if (card.HintOutline == null)
                card.HintOutline = CreateHintOutline(card);
            card.HintOutline.enabled = true;
            highlightedCard = card;
        }

        private static Outline CreateHintOutline(CardDisplay card)
        {
            var outline = card.Button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(255, 181, 45, 255);
            outline.effectDistance = new Vector2(10, 10);
            outline.enabled = false;
            return outline;
        }

        private void ClearHighlight()
        {
            if (highlightedCard != null)
            {
                highlightedCard.HintOutline.enabled = false;
                if (!highlightedCard.Anim.IsActive())
                    highlightedCard.Button.transform.localScale = Vector3.one;
            }
            highlightedCard = null;
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
            if (UiSprite("lock", Vector4.zero) != null)
            {
                IconImage(mark, "Padlock", "lock", At(-.15f, -.15f, 1.15f, 1.15f));
                return mark;
            }
            Panel(mark, "Shackle", At(.25f, .43f, .75f, .92f), Muted, true);
            Panel(mark, "Shackle cutout", At(.39f, .49f, .61f, .75f), LockCutout);
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

        private void CreateEffectSprites()
        {
            const int size = 64;
            var glow = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var sparkle = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var glowPixels = new Color32[size * size];
            var sparklePixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size * 2f - 1f;
                float v = (y + .5f) / size * 2f - 1f;
                float radius = Mathf.Sqrt(u * u + v * v);
                float soft = Mathf.Clamp01(1f - radius);
                glowPixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * soft * soft));

                // Four-point star (astroid) with a small bright core.
                float star = 1f - (Mathf.Sqrt(Mathf.Abs(u)) + Mathf.Sqrt(Mathf.Abs(v)));
                float alpha = Mathf.Clamp01(star * 4f) + Mathf.Clamp01(1f - radius * 3.2f) * .6f;
                sparklePixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(alpha)));
            }
            glow.SetPixels32(glowPixels);
            glow.Apply(false, true);
            sparkle.SetPixels32(sparklePixels);
            sparkle.Apply(false, true);
            generatedTextures.Add(glow);
            generatedTextures.Add(sparkle);
            glowSprite = Sprite.Create(glow, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
            sparkleSprite = Sprite.Create(sparkle, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
            generatedArt.Add(glowSprite);
            generatedArt.Add(sparkleSprite);
        }

        private static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private static float InOutCubic(float t)
            => t < .5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * .5f;

        private static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float InBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
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
            foreach (MotionHandle handle in effectMotions)
                handle.TryCancel();
            screenFade.TryCancel();
            settingsFade.TryCancel();
            foreach (Sprite sprite in generatedArt)
                Destroy(sprite);
            foreach (Texture2D texture in generatedTextures)
                Destroy(texture);
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

        private Button MakeArtButton(Transform parent, string name, Rect bounds, string resource, Rect crop, Color fallback,
            bool press = true)
        {
            Button button = MakeButton(parent, name, bounds, Color.clear, press);
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

        private Button MakeButton(Transform parent, string objectName, Rect bounds, Color color, bool press = true)
        {
            Image image = Panel(parent, objectName, bounds, color, true);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            if (press)
                image.gameObject.AddComponent<PressFeedback>();
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
