```text
▒███████▒▓█████ ▓█████▄    ▒███████▒▓█████ ▓█████▄    ▒███████▒▓█████ ▓█████▄ 
▒ ▒ ▒ ▄▀░▓█   ▀ ▒██▀ ██▌   ▒ ▒ ▒ ▄▀░▓█   ▀ ▒██▀ ██▌   ▒ ▒ ▒ ▄▀░▓█   ▀ ▒██▀ ██▌
░ ▒ ▄▀▒░ ▒███   ░██   █▌   ░ ▒ ▄▀▒░ ▒███   ░██   █▌   ░ ▒ ▄▀▒░ ▒███   ░██   █▌
  ▄▀▒   ░▒▓█  ▄ ░▓█▄   ▌     ▄▀▒   ░▒▓█  ▄ ░▓█▄   ▌     ▄▀▒   ░▒▓█  ▄ ░▓█▄   ▌
▒███████▒░▒████▒░▒████▓    ▒███████▒░▒████▒░▒████▓    ▒███████▒░▒████▒░▒████▓ 
░▒▒ ▓░▒░▒░░ ▒░ ░ ▒▒▓  ▒    ░▒▒ ▓░▒░▒░░ ▒░ ░ ▒▒▓  ▒    ░▒▒ ▓░▒░▒░░ ▒░ ░ ▒▒▓  ▒ 
░░▒ ▒ ░ ▒ ░ ░  ░ ░ ▒  ▒    ░░▒ ▒ ░ ▒ ░ ░  ░ ░ ▒  ▒    ░░▒ ▒ ░ ▒ ░ ░  ░ ░ ▒  ▒ 
░ ░ ░ ░ ░   ░    ░ ░  ░    ░ ░ ░ ░ ░   ░    ░ ░  ░    ░ ░ ░ ░ ░   ░    ░ ░  ░ 
  ░ ░       ░  ░   ░         ░ ░       ░  ░   ░         ░ ░       ░  ░   ░    
░                ░         ░                ░         ░                ░      
```

# Zed Zed Zed

Zed Zed Zed is a zombie horde mod for RimWorld focused on pressure, atmosphere, and messy colony defense. The undead do not always behave the same way. Some drift in from the edges, some huddle and wait to be stirred up, some shove toward the colony, and some claw their way out of the ground or gather around graves that have to be destroyed to stop the flow.

This repository is the full source package for the mod, along with a GitHub Actions workflow that builds a ready to install version of the mod.

## Main features

- a customizable zombie horde system with named variants, population pressure, and multiple event types
- infection, corpse progression, reanimation, and variant specific behavior that keeps colonies under constant stress
- themed settings, testing controls, custom art, and a GitHub build workflow for full source based mod development

## Feature ledger

This is the running feature list for the mod. Dates are listed when they can be confirmed from the current local changelog and iteration history. If the exact day cannot be confirmed from the files in this package, the feature is marked as **date not confirmed** instead of pretending otherwise.

### Date not confirmed, but present before 2026-04-07

- Core zombie horde incident that sends hostile undead onto the map
- Settings window with Overview, Events, Variants, Advanced, and Debug tabs
- Player naming controls for the zombie family name and the individual variant names
- Difficulty controls for group size, fast runner chance, resurrection timing, and infection turn timing
- Event tuning for edge trickles, colony pushes, huddled groups, ground bursts, grave events, full moons, and blood moons
- Colonist scaled population logic for day, night, full moon, and blood moon danger levels
- Variant roster including Biter, Boomer, Sick, Drowned, Brute, Grabber, and Lurker
- Variant specific behavior such as acid bursts, sickness pressure, water based drowned behavior, brute toughness, and grabber tongue attacks
- Zombie infection, terminal state, corpse tracking, reanimation, and repeated resurrection checks
- Zombie grave spawners that can keep producing undead until the grave is dealt with
- Zombie resources and processing, including rotten flesh, rotten leather, zombie bile, gold teeth, and zombie jerky
- Bile treatment kit crafting and treatment jobs
- Zombie blood, acid, filth, and related disease or damage systems
- Zombie butchering support and tainted zombie apparel handling
- Lurker systems including a lurker arrival incident plus capture, recruit, and tame interactions
- Custom zombie faction visuals, overlays, graves, hediffs, recipes, and themed UI art support
- Debug controls for forcing major zombie event types during testing
- GitHub Actions build workflow that produces an installable mod zip

### 2026-04-07

- Visible infection progress feedback was added
- Lurker conversion handling was improved
- Sick zombie spew was reworked to feel more like Fire Spew while staying in the custom zombie attack system
- Early compile and XML issues that blocked clean loading and building were fixed

### 2026-04-08

- Rotten leather chair was added as buildable furniture
- Rotten leather bed was added as buildable furniture
- Rotten leather furniture got north, east, and south directional textures
- A dedicated rotten leather stuff category was added
- Rotten leather floor set was expanded with mottled, patchwork, rotten, and stitched variants
- Rotten leather floors were updated so they can be placed broadly and built over more cleanly
- Runt was added as a new zombie creature with debug spawning and rare natural spawns alongside biters
- Runt zombies were forced to use real child sized pawns instead of only faking the scale visually
- Pregnant boomers were added, first for debugging and then as a natural spawn feature
- Dead infection progression was reworked so corpses can keep advancing after death
- Reanimation rules were reworked around Terminal and Reanimated states with hourly corpse checks
- Zombie acid and zombie blood damage over time systems were added
- The Puked On debuff and lure behavior tied to it were added
- Filth texture variation support was added for zombie blood, sick zombie blood, and zombie acid
- Old leftover systems and disabled remnants were cleaned out
- Natural pregnant boomer chance was raised to 10 percent
- Rotten leather bed art was repainted in a cleaner RimWorld style pass
- Rotten leather bed footprint was locked to a standard 2 by 1 single bed size
- Rotten leather bed textures were rebuilt again to fit that footprint properly

### 2026-04-09

- A pause menu shortcut was added so players can open Zed Zed Zed settings directly from the game menu
- The settings menu got extra vertical breathing room to reduce clipping and overlap
- Settings descriptions and tab help text were rewritten to sound more natural while staying brief
- The colony counter was rebuilt into a smaller themed HUD card
- The counter was updated to respect the custom family name instead of always saying zombies
- The counter was changed to show danger as a percentage instead of showing current over cap
- The counter was simplified into a cleaner two line format, such as `Zombies: 14` and `Danger: 117%`

## Building the mod on GitHub

1. Upload the full contents of this folder to your GitHub repository.
2. Open the **Actions** tab.
3. Open **Build Zed Zed Zed**.
4. Click **Run workflow**.
5. Wait for the build to finish.
6. Download the `ZedZedZed-InstallZip` artifact.
7. Unzip it.
8. Put the `ZedZedZed` folder into your RimWorld `Mods` folder.
9. Make sure Harmony is installed and enabled.
10. Enable **Zed Zed Zed** in RimWorld.

## Hidden workflow file note

If GitHub on iPhone refuses to upload hidden folders, manually create this file in the repository:

`.github/workflows/build.yml`

Then copy the contents from:

`CREATE_ON_GITHUB_build.yml.txt`

## Build output

The GitHub workflow produces:

- `Assemblies/ZedZedZed.dll`
- a ready to install `ZedZedZed` mod folder
- `ZedZedZed_install.zip`

## Project notes

- The workflow uses NuGet reference packages for RimWorld and Harmony, so it does not need a self hosted runner.
- The source is kept in the repository. The workflow artifact is the installable build.
- Harmony is still required at runtime.
- This project has been cleaned up as a full, vibe coded source package meant to be practical to maintain and expand.

## Iteration log

This source package is currently at **iteration 41** of the active development cycle in this build set.

Iteration history:
- Iteration 1 to 4: initial compile fixes, XML cleanup, butchering fix, and the first sick spew overhaul
- Iteration 5 to 10: infection, terminal, lurker, and reanimation systems reworked into a staged death and resurrection flow
- Iteration 11 to 16: sick spew presentation polish, dead infection corpse tracking, and related build fixes
- Iteration 17 to 23: zombie flooring, texture scale work, acid and blood damage over time, filth variation support, and runt child enforcement
- Iteration 24 to 29: runt creature added, boomer runt spawning added, naming cleanup, and debug spawn support
- Iteration 30 to 32: settings cleanup, leftover system cleanup, pregnant boomer natural spawn pass, and README documentation upgrades
- Iteration 33: rotten leather furniture added, including a functional chair and bed with directional textures
- Iteration 34: rotten leather bed art rebuilt with cleaner vanilla inspired north, east, and south textures
- Iteration 35: rotten leather bed def locked to a standard 2 by 1 RimWorld bed footprint and matching draw size
- Iteration 36: rotten leather bed north, east, and south textures rebuilt again to truly fit the new 2 by 1 bed footprint
- Iteration 37: added an in game pause menu shortcut that opens Zed Zed Zed settings directly from the game menu
- Iteration 38: added extra vertical breathing room in the settings UI and rewrote the settings text to read more naturally
- Iteration 39: rebuilt the map counter into a smaller themed HUD card that uses the custom family name and shows spawn cap as a percentage
- Iteration 40: simplified the counter into a compact two line readout with a clearer Danger percentage label
- Iteration 41: expanded the README with a dated feature ledger so the mod's major systems can be tracked more clearly over time

## Major update changelog

### 2026-04-09 - README feature ledger
- Added a dated feature ledger to the README so the mod's systems can be tracked in one place
- Used exact dates where the current changelog and iteration history support them
- Marked older features as date not confirmed when the exact day could not be proven from the files in this package

### 2026-04-09 - Counter label cleanup
- Changed the counter to a cleaner two line format that reads like `Zombies: 14` and `Danger: 117%`
- Kept the live map count as a real number while leaving the cap represented only through the percentage
- Made the first line respect the custom family name setting so renamed zombie families still show correctly

### 2026-04-09 - Themed colony counter clarity pass
- Reworked the on map counter into a smaller HUD card styled to match the Zed Zed Zed settings menu
- Swapped the hard to read current over cap display for a clear current count plus spawn cap percentage readout
- Made the counter use the player chosen family name instead of always saying zombies

### 2026-04-09 - Settings menu readability and wording pass
- Added more vertical breathing room inside settings cards, section bands, warning cards, preset notes, and debug action rows to reduce clipping and overlap risk
- Made the stepper cards size themselves to the description text instead of relying on a fixed height
- Rewrote the settings descriptions and tab help text to sound more natural while staying brief and clear

### 2026-04-08 - Rotten leather bed texture footprint alignment
- Reworked the rotten leather bed north, east, and south sprites to actually fit the locked 2 by 1 single bed footprint
- Narrowed the north and south views into a proper long single bed silhouette and widened the east view to read more like a standard RimWorld bed from the side
- Kept the rotten leather patchwork look while improving footprint readability in world placement

### 2026-04-08 - Rotten leather bed footprint correction
- Set the rotten leather bed to an explicit **2 by 1** footprint so it behaves like a normal RimWorld single bed
- Matched the bed graphic draw size to **2.0 by 1.0** so the sprite and placement are aligned more cleanly

### 2026-04-08 - Rotten leather bed texture art pass
- Repainted the rotten leather bed north, east, and south sprites with cleaner RimWorld style silhouettes
- Improved frame readability, mattress volume, pillow placement, and stitched hide details
- Kept the bed grimy and post apocalyptic while reducing the flat icon look


Only major updates are listed here. Small single line tweaks are intentionally left out.

### 2026-04-08
- Added a rotten leather chair and rotten leather bed as fully buildable furniture that use rotten leather specifically instead of generic leather
- Added north, east, and south directional textures for both rotten leather furniture pieces
- Added a dedicated rotten leather stuff category so the new furniture can be restricted cleanly to rotten leather while leaving other leathery crafting behavior alone
- Added the runt creature as a new zombie critter with directional textures, fast movement, low damage, debug spawning, and rare natural spawns with biter groups
- Added pregnant boomer support, then promoted it from a debug only test hook into a natural gameplay feature
- Reworked dead infection progression so corpses can keep progressing after death and enter the reanimation cycle correctly
- Reworked reanimation rules around Terminal and Reanimated states, hourly corpse checks, and recurring resurrection behavior
- Added zombie acid and zombie blood damage over time behavior, the Puked On debuff, and zombie lure behavior tied to that status
- Added filth texture variation support for zombie blood, sick zombie blood, and zombie acid
- Updated zombie leather floors so they can be placed broadly and so other construction can be built over them
- Forced runt zombies to be true child pawns instead of only looking smaller through visuals
- Cleaned out replaced or unfinished leftovers, including the old sick spit projectile path and disabled patch remnants
- Raised the natural pregnant boomer spawn chance to **10%**

### 2026-04-07
- Fixed early compile and XML issues that were blocking the mod from loading or building cleanly
- Fixed the zombie butchering path so zombie corpses can be processed correctly at the butcher table
- Reworked sick zombie spew to better mimic the feel and presentation of Fire Spew while staying on the custom zombie attack system
- Added visible infection progress feedback and improved lurker conversion handling

