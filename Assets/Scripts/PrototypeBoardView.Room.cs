using System.Collections.Generic;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace CampusCrates
{
    // Meta layer: coins from cleared levels decorate a student club room, Tile Match style.
    public sealed partial class PrototypeBoardView
    {
        private const string CoinsKey = "CampusCrates.Coins";
        private const string RoomKey = "CampusCrates.ClubRoom";
        private const float RoomAspect = 900f / 1100f;

        // Normalized placement inside the room art (left, bottom, right, top), matching the generated layers.
        private static readonly Rect[] DecorBounds =
        {
            Rect.MinMaxRect(.168f, .059f, .832f, .274f), // rug
            Rect.MinMaxRect(.137f, .815f, .863f, .927f), // pennants
            Rect.MinMaxRect(.146f, .617f, .510f, .857f), // chalkboard
            Rect.MinMaxRect(.704f, .003f, 1.000f, .348f), // plant
            Rect.MinMaxRect(.798f, .148f, 1.000f, .646f), // lamp
            Rect.MinMaxRect(.023f, .148f, .303f, .617f), // bookshelf
            Rect.MinMaxRect(.406f, .139f, .803f, .461f)  // desk
        };
        private static readonly Vector2[] DecorBubble =
        {
            new Vector2(.36f, .15f), new Vector2(.50f, .88f), new Vector2(.33f, .74f), new Vector2(.86f, .16f),
            new Vector2(.90f, .50f), new Vector2(.16f, .40f), new Vector2(.60f, .32f)
        };
        // Back to front, so the desk stands on the rug and the plant sits in front of the lamp.
        private static readonly int[] DecorDrawOrder = { 1, 2, 0, 5, 6, 4, 3 };

        private CoinWallet wallet;
        private ClubRoom room;
        private float shownCoins;
        private readonly List<Text> coinTexts = new List<Text>();
        private RectTransform roomScreen;
        private RectTransform roomStage;
        private RectTransform roomEffects;
        private Image homeRoomDot;
        private readonly RoomSlot[] roomSlots = new RoomSlot[ClubRoom.Catalog.Length];
        private readonly List<MotionHandle> roomMotions = new List<MotionHandle>();
        private int selectedSlot = -1;
        private int previewStyle;
        private Text roomTitle;
        private Text roomHint;
        private Text roomProgressText;
        private RectTransform roomProgressFill;
        private RectTransform roomSheet;
        private Button roomAction;
        private Text roomActionText;
        private RawImage roomActionArt;
        private Image roomActionCoin;
        private readonly Button[] swatches = new Button[ClubRoom.Styles];
        private readonly Image[] swatchArt = new Image[ClubRoom.Styles];
        private readonly Image[] swatchChecks = new Image[ClubRoom.Styles];
        private RectTransform resultReward;
        private Text resultCoins;
        private int lastReward;

        private sealed class RoomSlot
        {
            public RectTransform Root;
            public Image Art;
            public Image Ghost;
            public RectTransform Bubble;
            public Text Price;
            public Sprite[] Styles;
            public MotionHandle Anim;
        }

        private void LoadMeta()
        {
            wallet = new CoinWallet(PlayerPrefs.GetInt(CoinsKey, CoinWallet.StartingGift));
            room = new ClubRoom(PlayerPrefs.GetString(RoomKey, ""));
            shownCoins = wallet.Balance;
        }

        private void SaveMeta()
        {
            PlayerPrefs.SetInt(CoinsKey, wallet.Balance);
            PlayerPrefs.SetString(RoomKey, room.Save());
            PlayerPrefs.Save();
        }

        private void AwardLevelCoins(bool firstClear)
        {
            lastReward = CoinWallet.RewardFor(level, firstClear);
            wallet.Earn(lastReward);
            SaveMeta();
        }

        private void UpdateCoins()
        {
            // Count only on screens that show the wallet, so the player sees the reward roll in.
            if (!homeScreen.gameObject.activeSelf && !roomScreen.gameObject.activeSelf)
                return;
            float target = wallet.Balance;
            if (Mathf.Approximately(shownCoins, target))
                return;
            float speed = Mathf.Max(60f, Mathf.Abs(target - shownCoins) * 3.5f);
            shownCoins = Mathf.MoveTowards(shownCoins, target, speed * Time.unscaledDeltaTime);
            SetCoinTexts();
        }

        private void SetCoinTexts()
        {
            string value = Mathf.RoundToInt(shownCoins).ToString();
            foreach (Text text in coinTexts)
                text.text = value;
        }

        // ---------- Home hooks ----------

        private void BuildHomeMeta()
        {
            coinTexts.Add(CoinPill(homeScreen, "Wallet", At(.03f, .935f, .40f, .992f), 36));

            Button map = MakePackingButton(homeScreen, "Campus Map", At(.06f, .076f, .495f, .153f), Positive);
            ButtonText(map, "Map", At(.11f, .18f, .89f, .87f), 38, false);
            map.onClick.AddListener(ShowMap);

            Button club = MakePackingButton(homeScreen, "Club Room", At(.505f, .076f, .94f, .153f), Positive);
            IconImage(club.transform, "Room icon", "room-button", At(.03f, .02f, .27f, .98f));
            ButtonText(club, "Club Room", At(.25f, .18f, .95f, .87f), 34, false);
            homeRoomDot = IconImage(club.transform, "Can decorate", "dot", At(.84f, .62f, 1.02f, 1.22f));
            club.onClick.AddListener(ShowRoom);
        }

        private void RefreshHomeMeta()
        {
            SetCoinTexts();
            if (homeRoomDot != null)
                homeRoomDot.gameObject.SetActive(!room.IsComplete && room.HasAffordable(wallet));
        }

        // ---------- Result hooks ----------

        private void BuildResultReward()
        {
            resultReward = Sticker(resultPanel, "Coin reward", At(.30f, .205f, .70f, .300f)).rectTransform;
            IconImage(resultReward, "Coin", "coin", At(-.04f, -.1f, .30f, 1.1f));
            resultCoins = Label(resultReward, "Amount", "+0", At(.30f, .12f, .92f, .88f), 38, CoinBrown);
            resultCoins.font = titleFont;
        }

        private void ShowResultReward(bool won)
        {
            resultReward.gameObject.SetActive(won);
            if (!won)
                return;

            int reward = lastReward;
            resultCoins.text = "+0";
            resultReward.localScale = Vector3.zero;
            RectTransform coin = (RectTransform)resultReward.GetChild(0);
            effectMotions.Add(LMotion.Create(0f, 1f, 1.1f)
                .WithDelay(.35f)
                .Bind(t =>
                {
                    resultReward.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, OutBack(Mathf.Min(1f, t * 2.6f)));
                    resultCoins.text = "+" + Mathf.RoundToInt(reward * OutCubic(Mathf.Clamp01((t - .2f) / .8f)));
                    coin.localScale = new Vector3(Mathf.Cos(t * Mathf.PI * 4f), 1f, 1f);
                }));
        }

        // ---------- Room screen ----------

        private void BuildRoom()
        {
            roomScreen = Box(viewport, "Club room", Fill);
            Panel(roomScreen, "Warm room wash", Fill, new Color32(16, 51, 46, 200));

            Button back = MakeArtButton(roomScreen, "Back", At(.020f, .911f, .154f, .987f), "trail-pin", Fill, Gold);
            Headline(back.transform, "Back arrow", "‹", At(.17f, .20f, .83f, .89f), 58, Ink, TextWhite, 3f).font = font;
            back.onClick.AddListener(ShowHome);
            Ribbon(roomScreen, "Room ribbon", "Club Room", At(.17f, .902f, .83f, .998f), 50);

            Image track = Sticker(roomScreen, "Room progress", At(.035f, .848f, .565f, .900f), Surface.PillTeal);
            roomProgressFill = Sticker(track.transform, "Fill", At(.02f, .1f, 1f, .9f), Surface.BarFill).rectTransform;
            roomProgressText = Headline(track.transform, "Room count", "", At(.05f, .08f, .95f, .92f), 28, TextWhite, StrokeTeal, 2.5f);
            coinTexts.Add(CoinPill(roomScreen, "Wallet", At(.60f, .845f, .97f, .900f), 34));

            Image frame = Sticker(roomScreen, "Room frame", At(.025f, .292f, .975f, .842f), Surface.Panel);
            var holder = Box(frame.transform, "Room holder", At(.035f, .03f, .965f, .965f));
            roomStage = Box(holder, "Room", Fill);
            var fitter = roomStage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = RoomAspect;
            var background = Resources.Load<Texture2D>("CampusCrates/Decor/room-bg");
            var backgroundArt = Box(roomStage, "Room background", Fill).gameObject.AddComponent<RawImage>();
            backgroundArt.texture = background;
            backgroundArt.raycastTarget = false;
            if (background == null)
                backgroundArt.color = Mint;

            foreach (int item in DecorDrawOrder)
                roomSlots[item] = BuildSlot(item);
            foreach (int item in DecorDrawOrder)
                BuildBubble(item);
            roomEffects = Box(roomStage, "Room effects", Fill);

            BuildRoomSheet();
            roomScreen.gameObject.SetActive(false);
        }

        private RoomSlot BuildSlot(int item)
        {
            DecorItem decor = ClubRoom.Catalog[item];
            var slot = new RoomSlot { Root = Box(roomStage, decor.Name, DecorBounds[item]), Styles = new Sprite[ClubRoom.Styles] };
            for (int style = 0; style < ClubRoom.Styles; style++)
                slot.Styles[style] = DecorSprite($"{decor.Id}-{style}");

            slot.Ghost = Panel(slot.Root, "Empty spot", Fill, new Color(1f, 1f, 1f, .42f));
            slot.Ghost.sprite = DecorSprite(decor.Id + "-ghost");
            if (slot.Ghost.sprite == null)
                slot.Ghost.color = Color.clear;

            slot.Art = Panel(slot.Root, "Decor", Fill, Color.white);
            slot.Art.sprite = slot.Styles[0];
            slot.Art.raycastTarget = true;
            // Only the painted pixels react, so overlapping furniture stays individually tappable.
            if (slot.Styles[0] != null && slot.Styles[0].texture.isReadable)
                slot.Art.alphaHitTestMinimumThreshold = .3f;
            var pick = slot.Art.gameObject.AddComponent<Button>();
            pick.transition = Selectable.Transition.None;
            pick.onClick.AddListener(() => SelectSlot(item));
            return slot;
        }

        private void BuildBubble(int item)
        {
            RoomSlot slot = roomSlots[item];
            slot.Bubble = Box(roomStage, ClubRoom.Catalog[item].Name + " spot", Centered(DecorBubble[item], .2f, .2f * RoomAspect));
            Button plus = MakeButton(slot.Bubble, "Add decor", At(.18f, .30f, .82f, 1f), Color.clear);
            Image plusArt = IconImage(plus.transform, "Plus", "plus", Fill);
            plus.targetGraphic = plusArt;
            plus.onClick.AddListener(() => SelectSlot(item));

            Image tag = Sticker(slot.Bubble, "Price", At(-.06f, 0f, 1.06f, .34f), Surface.PillGold);
            IconImage(tag.transform, "Coin", "coin", At(.02f, -.05f, .36f, 1.05f));
            slot.Price = Label(tag.transform, "Price", ClubRoom.Catalog[item].Price.ToString(),
                At(.32f, .12f, .94f, .88f), 30, TextWhite);
            slot.Price.font = titleFont;
            Stroke(slot.Price, StrokeBrown, 2.5f, 2f);
        }

        private void BuildRoomSheet()
        {
            roomSheet = Sticker(roomScreen, "Decor sheet", At(.03f, .018f, .97f, .282f), Surface.Panel).rectTransform;
            roomTitle = Label(roomSheet, "Title", "", At(.06f, .74f, .94f, .95f), 40, Positive);
            roomTitle.font = titleFont;
            roomHint = Label(roomSheet, "Hint", "", At(.07f, .40f, .93f, .74f), 28, Ink, detail: true);

            for (int style = 0; style < ClubRoom.Styles; style++)
            {
                int choice = style;
                float left = .09f + style * .285f;
                Button swatch = MakeButton(roomSheet, $"Style {style + 1}", At(left, .37f, left + .25f, .76f), Color.clear);
                Image face = Sticker(swatch.transform, "Face", Fill, Surface.Panel);
                swatch.targetGraphic = face;
                swatchArt[style] = Panel(swatch.transform, "Preview", At(.12f, .1f, .88f, .9f), Color.white);
                swatchArt[style].preserveAspect = true;
                swatchChecks[style] = IconImage(swatch.transform, "Chosen", "check", At(.66f, .62f, 1.08f, 1.12f));
                swatch.onClick.AddListener(() => ChooseStyle(choice));
                swatches[style] = swatch;
            }

            roomAction = MakePackingButton(roomSheet, "Decor action", At(.17f, .06f, .83f, .33f), Gold);
            roomActionArt = roomAction.targetGraphic as RawImage;
            roomActionText = ButtonText(roomAction, "", At(.08f, .14f, .78f, .90f), 40, true);
            roomActionCoin = IconImage(roomAction.transform, "Coin", "coin", At(.74f, .12f, .92f, .88f));
            roomAction.onClick.AddListener(RoomActionPressed);
        }

        private Sprite DecorSprite(string name)
        {
            var texture = Resources.Load<Texture2D>("CampusCrates/Decor/" + name);
            if (texture == null)
                return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
            generatedArt.Add(sprite);
            return sprite;
        }

        private void ShowRoom()
        {
            selectedSlot = -1;
            RefreshRoom();
            ShowScreen(roomScreen);
            // Freshly bought rooms land in a gentle cascade every time the player visits.
            int order = 0;
            foreach (int item in DecorDrawOrder)
            {
                RoomSlot slot = roomSlots[item];
                if (!room.IsOwned(item))
                    continue;
                PopSlot(slot, .06f + order++ * .05f, .4f);
            }
        }

        private void SelectSlot(int item)
        {
            if (selectedSlot == item)
            {
                selectedSlot = -1;
                RefreshRoom();
                return;
            }
            selectedSlot = item;
            previewStyle = room.IsOwned(item) ? room.StyleOf(item) : 0;
            RefreshRoom();
            PopSlot(roomSlots[item], 0f, .32f);
        }

        private void ChooseStyle(int style)
        {
            if (selectedSlot < 0)
                return;
            previewStyle = style;
            if (room.IsOwned(selectedSlot) && room.SetStyle(selectedSlot, style))
                SaveMeta();
            RefreshRoom();
            PopSlot(roomSlots[selectedSlot], 0f, .3f);
            RoomBurst(selectedSlot, .45f);
        }

        private void RoomActionPressed()
        {
            if (selectedSlot < 0)
            {
                StartFromHome();
                return;
            }
            if (room.IsOwned(selectedSlot))
            {
                selectedSlot = -1;
                RefreshRoom();
                return;
            }

            int item = selectedSlot;
            if (!room.TryBuy(item, previewStyle, wallet))
                return;
            SaveMeta();
            selectedSlot = -1;
            RefreshRoom();
            DropInSlot(roomSlots[item]);
            RoomBurst(item, 1f);
            if (room.IsComplete)
                SpawnConfettiIn(roomScreen, roomMotions);
        }

        private void RefreshRoom()
        {
            for (int item = 0; item < roomSlots.Length; item++)
            {
                RoomSlot slot = roomSlots[item];
                bool owned = room.IsOwned(item);
                bool previewing = !owned && selectedSlot == item;
                int style = owned ? room.StyleOf(item) : previewStyle;
                slot.Art.gameObject.SetActive(owned || previewing);
                slot.Art.sprite = slot.Styles[Mathf.Clamp(style, 0, ClubRoom.Styles - 1)];
                slot.Art.color = previewing ? new Color(1f, 1f, 1f, .78f) : Color.white;
                slot.Art.raycastTarget = owned;
                slot.Ghost.gameObject.SetActive(!owned && !previewing);
                slot.Bubble.gameObject.SetActive(!owned && !previewing);
                bool affordable = wallet.CanAfford(ClubRoom.Catalog[item].Price);
                slot.Price.color = affordable ? (Color)TextWhite : new Color32(255, 226, 205, 255);
                slot.Bubble.GetComponentInChildren<Button>().targetGraphic.color = affordable ? Color.white : new Color(.82f, .78f, .74f);
            }

            float fraction = room.OwnedCount / (float)ClubRoom.Catalog.Length;
            roomProgressFill.anchorMax = new Vector2(Mathf.Max(.2f, .02f + .96f * fraction), .9f);
            roomProgressFill.gameObject.SetActive(room.OwnedCount > 0);
            roomProgressText.text = room.IsComplete ? "Room complete!" : $"Room {room.OwnedCount}/{ClubRoom.Catalog.Length}";
            SetCoinTexts();
            RefreshSheet();
        }

        private void RefreshSheet()
        {
            bool picking = selectedSlot >= 0;
            roomHint.gameObject.SetActive(!picking);
            foreach (Button swatch in swatches)
                swatch.gameObject.SetActive(picking);
            roomActionCoin.gameObject.SetActive(false);
            SetActionEnabled(true);

            if (!picking)
            {
                roomTitle.text = room.IsComplete ? "Club room complete!" : "Decorate your club room";
                roomHint.text = room.IsComplete
                    ? "Tap any piece to change its style."
                    : "Tap a + to add school decor.\nPack crates to earn more coins.";
                roomActionText.text = $"Play Level {campusProgress.UnlockedLevel}";
                Place(roomActionText.rectTransform, At(.08f, .14f, .92f, .90f));
                return;
            }

            DecorItem decor = ClubRoom.Catalog[selectedSlot];
            bool owned = room.IsOwned(selectedSlot);
            roomTitle.text = decor.Name;
            for (int style = 0; style < ClubRoom.Styles; style++)
            {
                swatchArt[style].sprite = roomSlots[selectedSlot].Styles[style];
                swatchArt[style].color = swatchArt[style].sprite != null ? Color.white : Color.clear;
                swatchChecks[style].gameObject.SetActive(style == previewStyle);
            }

            if (owned)
            {
                roomActionText.text = "Done";
                Place(roomActionText.rectTransform, At(.08f, .14f, .92f, .90f));
                return;
            }

            int missing = decor.Price - wallet.Balance;
            if (missing > 0)
            {
                roomActionText.text = $"Need {missing} more";
                Place(roomActionText.rectTransform, At(.08f, .14f, .92f, .90f));
                SetActionEnabled(false);
                return;
            }

            roomActionText.text = $"Buy  {decor.Price}";
            Place(roomActionText.rectTransform, At(.06f, .14f, .76f, .90f));
            roomActionCoin.gameObject.SetActive(true);
        }

        private void SetActionEnabled(bool enabled)
        {
            roomAction.interactable = enabled;
            if (roomActionArt != null)
                roomActionArt.color = enabled ? Color.white : new Color(.7f, .7f, .7f, 1f);
        }

        private void AnimateRoom(float time)
        {
            for (int item = 0; item < roomSlots.Length; item++)
            {
                RoomSlot slot = roomSlots[item];
                if (slot.Bubble.gameObject.activeSelf)
                    slot.Bubble.anchoredPosition = Vector2.up * (6f * Mathf.Sin(time * 3f + item * 1.3f));
                if (item == selectedSlot && !room.IsOwned(item) && !slot.Anim.IsActive())
                    slot.Art.color = new Color(1f, 1f, 1f, .62f + .25f * (.5f + .5f * Mathf.Sin(time * 5f)));
            }
        }

        private void PopSlot(RoomSlot slot, float delay, float duration)
        {
            RectTransform rect = slot.Root;
            slot.Anim.TryCancel();
            slot.Anim = LMotion.Create(0f, 1f, duration)
                .WithDelay(delay)
                .WithOnComplete(() => rect.localScale = Vector3.one)
                .Bind(t => rect.localScale = new Vector3(1f + .08f * Mathf.Sin(t * Mathf.PI), 1f + .12f * Mathf.Sin(t * Mathf.PI), 1f))
                .AddTo(rect.gameObject);
        }

        private void DropInSlot(RoomSlot slot)
        {
            RectTransform rect = slot.Root;
            slot.Anim.TryCancel();
            rect.localScale = Vector3.zero;
            slot.Anim = LMotion.Create(0f, 1f, .55f)
                .WithOnComplete(() =>
                {
                    rect.localScale = Vector3.one;
                    rect.anchoredPosition = Vector2.zero;
                })
                .Bind(t =>
                {
                    float grow = OutBack(Mathf.Min(1f, t * 1.4f));
                    rect.localScale = new Vector3(grow, grow * (1f + .1f * Mathf.Sin(t * Mathf.PI * 2f) * (1f - t)), 1f);
                    rect.anchoredPosition = Vector2.up * (90f * (1f - OutCubic(t)));
                })
                .AddTo(rect.gameObject);
        }

        private void RoomBurst(int item, float size)
        {
            Vector2 at = roomEffects.InverseTransformPoint(roomSlots[item].Root.TransformPoint(roomSlots[item].Root.rect.center));
            BurstIn(roomEffects, roomMotions, at, Gold, size);
        }
    }
}
