# Campus Crates

A small triple-match puzzle for phones, made in Unity 6. You are packing supplies for a campus festival: tap a tile, it goes to a tray with 7 slots, and three of the same kind turn into a crate.

I am a software developer and I want to move into product work in mobile games. I made this to practice the product side on something real: what the player should learn first, when to add a new rule, and how I would check that it works.

It is a prototype. There are 6 levels and a Windows build. Nobody outside has playtested it yet, so I have no player data to show.

## How it plays

You can only pick tiles that are not covered. Covered ones show a lock. A picked tile goes to the tray, and three of a kind clear out and become a crate. Pack everything and you win. If the tray is full with 7 tiles and no match, you lose the attempt.

From Level 2 there is a hint button. It points at the third tile when you already have two of that kind in the tray.

## Levels

I tried to add only one new thing per level.

| Level | New thing |
| --- | --- |
| 1 | Three open notebooks, first match with a guide |
| 2 | A covered tile. You move the pencil to reach the third notebook |
| 3 | A third supply type, three crates |
| 4 | Four types. This is the first level where the tray can fill up |
| 5 | Pyramid. One tile on top can cover four |
| 6 | Star. Some tiles are completely hidden under others |

## Outside the levels

- A campus map with six stops that shows which levels are done, open or locked
- A festival stand that grows with the first three levels
- Coins. The first clear of a level pays more than a replay
- A club room where coins buy seven decoration spots. Each has three styles, and switching style is free after you buy it

Everything is saved on the device.

## Why it looks like this

Before I decided anything I played Food Match 3D, Tile Match and Braindom and wrote down how they teach the game, what the core loop is, how they reward you and where ads show up. Tile Match is the closest one to this game: limited access to tiles, 7 slots, and the three tiles do not have to be next to each other. From Food Match I took the idea of teaching the main action first and the helpers later.

Level 1 holds your hand for exactly one match, with a highlight and an arrow. Level 2 asks for the same thing with one extra step and no guide.

The thing I most want to know is whether a new player understands which tiles can be picked without anyone telling them. So the number I would look at is this: of the people who start Level 2 for the first time, how many finish it on the first try, in under 120 seconds, with no help. I would also note the time to the first match, taps on locked tiles, hint use and retries.

Next I want to watch three people play, change one thing based on what I see, and then try again with new people.

## How I built it

Most of the C# was written with AI coding assistants. I did not want to hide that. My part was deciding what each step should do, writing down what "done" means, checking every build in the Unity Simulator and choosing what to change next. The art is also AI generated, from my prompts.

The rules are kept apart from the Unity screen code, which means they can be tested without opening the editor.

- `Assets/Scripts/Rules` has the board rules, level data, progress, coins and the club room
- `Assets/Scripts/Layout` has the portrait layout and tile stacking
- `Assets/Scripts` has the Unity view and a few UI helpers
- `Assets/Tests/EditMode` has 28 NUnit tests for rules, level solvability, rewards and layout

## Running it

Open the folder with Unity 6000.3.7f1, load `Assets/Scenes/Main.unity` and press Play. It is designed for portrait, so use the Device Simulator with a phone selected.

Tests are under Window > General > Test Runner > EditMode.

## What is missing

- I have only run it in the Simulator and as a Windows build, not on a real phone
- No analytics, ads or purchases
- Only six levels, and I have not checked the difficulty order with real players

## Credits

Animation uses [LitMotion](https://github.com/annulusgames/LitMotion). The font is [Fredoka](https://fonts.google.com/specimen/Fredoka) under the SIL Open Font License; the license text is in `Assets/Resources/CampusCrates/Theme/Fredoka-OFL.txt`.

Food Match 3D, Tile Match and Braindom belong to their owners. I only used them as references and this project has no connection to them.
