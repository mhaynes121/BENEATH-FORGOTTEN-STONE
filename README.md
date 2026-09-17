# BENEATH FORGOTTEN STONE

*"The deep remembers."*

A turn-based, permadeath roguelike built as a pure C# console application — no game engine, no external NuGet dependencies. Everything from field-of-view and procedural dungeon generation to lighting and sound propagation is hand-rolled on top of `System.Console`.

For centuries the old dwarven halls beneath the mountains stood silent. Now travelers vanish along the mountain roads, and the Crown pays gold for whatever an adventurer can recover from the ruins — if they come back at all.

## Features

- **Procedural dungeons** — every floor is generated fresh; no two runs look the same, and the deeper you go, the worse it gets.
- **4 races × 4 classes** — Human, Elf, Dwarf, and Halfling, each with distinct stat roll ranges and elemental resistance tendencies; Warrior, Thief, Priest, and Mage, each with its own growth curve, regen rate, and equipment restrictions (weapon types/sizes, armor weight, shield size, throwables).
- **Elemental resistance system** — fire, ice, shock, poison, and magic resistance stack from race, class, and gear, with some monsters attuned to specific terrain.
- **Hazardous terrain** — water, ice, fire, lava, mud, sand, grass, swamp, and ash tiles each modify movement or elemental damage taken in different ways.
- **Dynamic FOV and lighting** — dark rooms render as unexplored until lit; candles, torches, lanterns, and light-granting spells all project their own radius, and water douses a lit flame on contact.
- **Full combat suite** — melee, ranged weapons (bow/crossbow/sling), thrown weapons, spells, and class-specific physical skills, all turn-based.
- **Inventory & equipment depth** — weight-based encumbrance, containers, item quality and identification, stacking, and per-class equipment compatibility rules.
- **Companion pets** with their own AI and progression, plus wandering traders to buy from and sell to.
- **Persistent graveyard** — tracks your deepest deaths across runs (Top 10 Deepest Deaths, from the main menu).
- **Save/load**, and a `--selftest` CLI flag that runs a headless smoke test of core game logic without opening a console screen — useful for CI or a quick sanity check after changes.

## Getting Started

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/mhaynes121/BENEATH-FORGOTTEN-STONE.git
cd BENEATH-FORGOTTEN-STONE
dotnet run
```

To run the headless self-test instead of launching the game:

```bash
dotnet run -- --selftest
```

The game targets a 60x24 console window and resizes the terminal to fit on launch.

## Controls

Movement uses the numpad (with Home/Up/PageUp/Left/Right/End/Down/PageDown as a fallback if NumLock is off or your terminal doesn't send numpad codes). A few of the core actions:

| Key | Action |
|---|---|
| Numpad 1–9 (no 5) | Move in the corresponding direction |
| `G` / `D` | Pick up / drop an item |
| `I` | Open character sheet (stats, inventory, spells & skills) |
| `C` / `S` | Cast a spell (or charged item) / use a physical skill |
| `F` | Fire or throw whatever's readied |
| `>` / `<` | Descend / ascend stairs |
| `W` | Sleep to recover HP (risks an ambush) |
| `H` | Full in-game command reference |
| `Q` | Save and quit |

Press `H` at any time during a run for the complete, paginated command and floor-tile reference.

## Project Structure

```
Core/          Game loop, rendering, input, screens, turn scheduling, and other systems
Dungeon/       Procedural generation, tiles, field of view, lighting, room objects
Entities/      Player, monsters, items, races/classes, spells, skills, AI
Persistence/   Save/load and the graveyard of past runs
Diagnostics/   Headless self-test harness (--selftest)
```

## Status

Actively in development as a solo project. Current in-game version is tracked in `CreditsScreen.cs` and shown on the Credits screen.

## License

No license has been chosen yet. Until one is added, all rights are reserved by default — open an issue or reach out if you'd like to use or contribute to the code.

## AI Usage

This project is partially designed as a way for me to get more familiar with using AI tools such as the various ClaudeAI tools and ChatGPT.
I would say that around 90% of the actual code is AI generated and most of the details for items, monsters, spells, skills, etc are also AI
generated. Many are then modified to adjust various settings. 

The process I have followed for this project is as follows:

Initially I start in ChatGPT with a broad concept and I explain what I want it to do. An example might be the spell system. Here is an example of how I might build that out (after each step I would have it display what was added to the plan of action so we could catch errors and make changes early):

- I want a spell system that allows a character to cast spells. Spells should be able to be cast on the character themselves or in a specified direction. Let's start building out a plan of action.
- Let's expand upon that and add the concept of positive and negative effects. Positive effects will be considered as just 'normal' effects while negative effects will show as a 'curse'.
- We don't want the player's character to get every spell at lvl 1 so let's add a required level to each spell. Go ahead and populate with best guess on lvl acquired and I'll fine tune.
- Players should not get spells out of thin air. Mages should require reading a scroll to gain knowledge of a spell and priest should have to read a spellbook.
- Reading a scroll or spellbook should not be a guaranteed success. Successfully reading a scroll or spellbook should be based off of primary stat and luck to a minor degree.
- We need to add the concept of instant and over time spells. Instant spells should apply their effect in one turn while over time spells should have their effects spread out over the specified number of turns.
- Spells that are cast in a direction should show a moving character on the screen until they hit something or end their range. This 'animation' should run outside of the main game loop so that monsters can't 'dodge' spells by moving to a new tile. Mimic the display mechanic of thrown items / projectiles.
- There should be a new screen added that shows the user all of the spells they can get, what level they get them, and what they currently have. We will distinguish between what they know and don't know by using different font colors to highlight the learned ones.
- Ok, now give me the complete plan of action that I can provide to another AI agent so that it knows what feature we're trying to implement and how we want it implemented.

Once that is done I would take the project plan provided by ChatGPT and add it to the Claude project. Then I would have Claude review the plan of action and have it actually generate a Claude plan for the changes it's going to make to satisfy the requirements. Once I've had a chance to review the Claude plan I will make any required changes or add additional details if I realize I missed something. Then I would have it go ahead and implement those changes. 

After every change I have Claude setup to run through all of the existing and newly added test cases. If those succeed I will start a few games and begin testing out the functionality. 
