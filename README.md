# Campus Crates

A small triple-match puzzle prototype for mobile, built in Unity 6. You pack supplies for a campus festival: pick a tile, send it to a 7-slot tray, and three of a kind become a crate.

I built it as a product exercise. I am a software developer moving into product work in mobile games, and I wanted a real project where I had to make and defend product decisions: what the player learns first, when a new rule appears, and how I would know if it works.

**Status:** playable prototype, 6 levels, Windows build. It has not been tested with outside players yet, so there is no retention or playtest data in this repo.

## How it plays

- Only uncovered tiles can be picked. Covered tiles show a lock.
- A picked tile moves to the tray. Three of the same kind clear and become a crate.
- Pack every tile to win. If the tray holds 7 tiles with no match, the attempt ends.
- From Level 2 a hint button highlights the third tile when two matching ones are already in the tray.

## Level order

Each level adds one new thing, so the player never has to learn two rules at once.

| Level | What is new |
| --- | --- |
| 1 | Three open notebooks and a guided first match |
| 2 | A covered tile: remove the pencil in front to reach the third notebook |
| 3 | A third supply type and three crates |
| 4 | Four supply types; the tray can now fill up and end the attempt |
| 5 | Pyramid layout: one tile on top can cover four below |
| 6 | Star layout: some tiles are fully hidden under others |

## Progress and rewards

- **Campus map:** six stops on a festival route that show finished, open and locked levels.
- **Festival stand:** the first clear of Levels 1 to 3 each adds a delivery and upgrades the stand.
- **Coins:** a first clear pays more on later levels; replays pay a small fixed amount.
- **Club room:** coins buy seven decoration spots, each with three styles that can be swapped for free after purchase.

Progress, coins and the room are saved locally on the device.

## Product thinking behind it

**Reference games.** Before scoping, I played Food Match 3D, Tile Match and Braindom and took notes on their onboarding, core loop, rewards and ad placement. Tile Match is the closest mechanical reference (limited tile access, a 7-slot tray, triples that do not need to be adjacent). Food Match shaped how the basic action and helpers are introduced step by step.

**First-time experience.** Level 1 walks the player through a single match with a highlighted tile and a moving arrow. Level 2 asks them to apply the same rule alone, with one extra step. The guide turns off after the first crate.

**How I would measure it.** The main question is whether a new player understands tile access without help. The metric I defined: of the players who start Level 2 for the first time, how many finish it on the first attempt, within 120 seconds, with no help from the observer. Supporting notes: time to first match, taps on locked tiles, hint use and retries.

**Next step.** A small observed playtest (three people), one change based on what it shows, then a second round with new players.

## How it was built

I directed the project and made the product and design decisions. Most of the C# code was written with AI coding assistants. My part was to define what each step should do, write the acceptance criteria, check every build in the Unity Simulator, and decide what to change. The art was generated with AI image tools from my prompts.

The game rules are separated from the Unity view, so they can be tested without opening the editor.

- `Assets/Scripts/Rules`: board rules, level data, progress, coins and the club room
- `Assets/Scripts/Layout`: portrait layout and tile stacking
- `Assets/Scripts`: the Unity UI view and small UI helpers
- `Assets/Tests/EditMode`: 28 NUnit tests covering rules, level solvability, rewards and layout

## Run it

1. Install Unity **6000.3.7f1** (Unity Hub will offer it when you open the project).
2. Open this folder as a project and load `Assets/Scenes/Main.unity`.
3. Press Play. The Device Simulator with a phone preset gives the intended portrait view.
4. Tests: **Window > General > Test Runner > EditMode > Run All**.

## Known limits

- Tested in the Unity Simulator and as a Windows build; not yet on a physical phone.
- No analytics events, ads or in-app purchases.
- Six levels only; difficulty order has not been validated with players.

## Credits

- [LitMotion](https://github.com/annulusgames/LitMotion) for tile and UI animation
- [Fredoka](https://fonts.google.com/specimen/Fredoka) typeface, SIL Open Font License (license text in `Assets/Resources/CampusCrates/Theme/Fredoka-OFL.txt`)
- Food Match 3D, Tile Match and Braindom are products of their owners and are mentioned only as design references. This project is not affiliated with them.
