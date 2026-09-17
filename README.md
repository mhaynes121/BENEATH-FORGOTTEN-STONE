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
