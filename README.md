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

Zed Zed Zed is a RimWorld zombie mod built around pressure, atmosphere, and the feeling that the colony is never quite finished cleaning up the last mess before the next one starts. This is not just a single raid that shows up, dies, and goes away. The dead can drift in from the edge of the map, bunch up in quiet packs, push straight at your base, erupt inside the colony, or keep crawling out of graves until you smash the source.

The whole mod is built to be tuned to your taste. You can rename the zombie family, rename each strain, decide which strains are allowed, control how often events fire, scale danger to colony size, change infection timing, open the settings directly from the pause menu, and use debug tools when you want to test something fast.

This repository is the full source package for the mod, along with a GitHub Actions workflow that builds a ready to install version of the mod.

## What this mod adds

### Dynamic outbreak pressure

- **Edge trickles** send in small wandering groups between bigger attacks so the map never feels fully safe.
- **Huddled packs** gather in place until something wakes them up.
- **Base pushes** lean harder into direct pressure on the colony.
- **Ground bursts** can erupt inside your walls and turn a calm moment into a scramble.
- **Grave events** create an active grave that keeps producing more undead until you destroy it.
- **Full moons and blood moons** can spike the danger and push the map into a much worse state.
- **Colony scaled pressure** lets the mod size danger around your current colonist count instead of relying only on flat caps.
- **The map HUD** shows a live count for your chosen zombie family name and a Danger percentage for your current home map.

### Zombie strains

- **Biters** are the standard horde body. They make up most packs and raids.
- **Runts** are smaller, weaker, and unnerving. They use child sized pawns and are good at clogging tight spaces.
- **Boomers** burst into acid and punish grouped colonists.
- **Sick zombies** spread filthy blood, sickness, and bile pressure.
- **Drowned zombies** are strongest near water, try to return to water when they lose prey, and roam more freely in rain.
- **Brutes** are larger, tougher, and better at soaking damage.
- **Grabbers** pin colonists in place and force a struggle to break free.
- **Lurkers** are a special passive strain that can arrive, be captured, recruited, tamed, and kept by the colony.
- **Runner strains** can appear as rare faster zombies within the wider outbreak.
- **Custom names** let you rename the zombie family and each individual strain so the mod reads the way you want it to in game.

### Infection, death, and reanimation

- **Zombie infection** builds over time instead of ending the moment somebody is scratched.
- **Visible infection stages** make it easier to tell how bad things are getting.
- **Bile med kits** can cure zombie infection before it reaches the terminal point.
- **Terminal and reanimated states** keep the outbreak dangerous even after a pawn dies.
- **Post death progression** means infection can keep climbing on a corpse after death instead of stopping there.
- **Hourly resurrection checks** give reanimated corpses repeated chances to rise again if the head and skull are still intact.
- **Zombie blood exposure** and **zombie acid corrosion** add lingering damage and contamination pressure on top of direct attacks.
- **Puked On** makes a pawn stink so badly that nearby zombies are drawn toward them.

### Colony side content

- **Zombie graves** come in multiple strain themed versions.
- **Zombie butchering and salvage** produce rotten flesh, rotten leather, zombie bile, zombie jerky, and gold teeth.
- **Bile treatment crafting** gives you a colony side answer to infection if you stay ahead of it.
- **Rotten leather furniture** currently includes a rotten leather bed and chair.
- **Rotten leather floors** include rotten, stitched, patchwork, and mottled variants.
- **Zombie blood, sick blood, and acid filth** all have their own themed visuals and gameplay hooks.
- **Custom overlays, graves, faction visuals, traits, and hediffs** help the mod feel like a full themed outbreak instead of a loose pile of incidents.

### Settings and testing tools

- **Themed settings menu** with Overview, Events, Variants, Advanced, and Debug tabs.
- **Pause menu shortcut** so you can open Zed Zed Zed settings directly from the in game menu.
- **Naming controls** for the family name and every strain name.
- **Event controls** for trickles, pushes, huddles, bursts, graves, full moons, and blood moons.
- **Population controls** for day, night, moon events, and colony scaling.
- **Infection timing controls** so you can make the outbreak harsher or more forgiving.
- **Debug buttons** for forced waves, moon events, grave events, and strain specific test spawns.

## Feature ledger

This is the running feature list for the mod. Dates are listed when they can be confirmed from the current local changelog and iteration history. If the exact day cannot be confirmed from the files in this package, the feature is marked as **date not confirmed** instead of pretending otherwise.

### Date not confirmed, but present before 2026-04-07

- The base zombie horde incident was already in place.
- The settings window already had Overview, Events, Variants, Advanced, and Debug tabs.
- Family naming and per strain naming were already supported.
- Difficulty tuning for group size, runner chance, resurrection timing, and infection timing was already supported.
- Event tuning for edge trickles, colony pushes, huddled packs, ground bursts, grave events, full moons, and blood moons was already in place.
- Colony scaled danger logic for day, night, full moon, and blood moon pressure was already in place.
- The core strain roster already included Biter, Boomer, Sick, Drowned, Brute, Grabber, and Lurker.
- Variant behavior such as acid bursts, sickness pressure, water based drowned behavior, brute toughness, and grabber attacks was already present.
- Infection, corpse progression, reanimation, and recurring resurrection checks were already part of the mod.
- Grave spawners that keep producing undead until destroyed were already part of the event pool.
- Zombie resource processing, bile treatment jobs, filth systems, butchering support, and tainted zombie apparel handling were already present.
- Lurker arrival, capture, recruit, and tame support were already present.
- Custom faction visuals, overlays, graves, hediffs, recipes, and themed UI art were already part of the package.
- The GitHub Actions build workflow that produces an installable zip was already part of the repository.

### 2026-04-07

- Visible infection progress feedback was added.
- Lurker conversion handling was improved.
- Sick zombie spew was reworked to feel closer to Fire Spew while staying inside the mod's own attack system.
- Early compile issues and XML problems that blocked clean loading were fixed.

### 2026-04-08

- Rotten leather chair was added as buildable furniture.
- Rotten leather bed was added as buildable furniture.
- Rotten leather furniture got north, east, and south directional textures.
- A dedicated rotten leather stuff category was added.
- Rotten leather floors were expanded with rotten, stitched, patchwork, and mottled variants.
- Rotten leather floors were updated so they can be placed more broadly and built over more cleanly.
- Runts were added as a new zombie strain with debug spawning and rare natural spawns alongside biters.
- Runts were forced to use true child sized pawns instead of only looking smaller through visuals.
- Pregnant boomers were added, first for debugging and then as a natural spawn feature.
- Dead infection progression was reworked so corpses can keep advancing after death.
- Reanimation rules were reworked around Terminal and Reanimated states with hourly corpse checks.
- Zombie acid corrosion and zombie blood exposure were added as damage over time systems.
- The Puked On debuff and its lure behavior were added.
- Filth texture variation support was added for zombie blood, sick zombie blood, and zombie acid.
- Old leftover systems and disabled remnants were cleaned out.
- The natural pregnant boomer chance was raised to 10 percent.
- Rotten leather bed art was repainted in a cleaner RimWorld style pass.
- Rotten leather bed footprint was corrected to the standard wooden single bed size.
- Rotten leather bed textures were rebuilt again to fit that footprint properly.

### 2026-04-09

- A pause menu shortcut was added so players can open Zed Zed Zed settings directly from the game menu.
- The settings menu got more vertical breathing room to reduce clipping and overlap.
- Settings descriptions and tab help text were rewritten to sound more natural while staying brief.
- The colony counter was rebuilt into a smaller themed HUD card.
- The counter was updated to respect the custom family name instead of always saying zombies.
- The counter was changed to show danger as a percentage instead of showing current over cap.
- The counter was simplified into a cleaner two line format, such as `Zombies: 14` and `Danger: 117%`.
- The README and About text were rewritten in a more natural workshop style voice and expanded to explain the mod's features more clearly.
- The map counter HUD card was given a little more height so the lower line does not clip at the bottom.

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
- This project is meant to be practical to maintain, easy to package, and easy to keep building from GitHub.

## Iteration log

This source package is currently at **iteration 45** of the active development cycle in this build set.

Iteration history:
- Iteration 1 to 4: early compile fixes, XML cleanup, butchering repair, and the first sick spew overhaul.
- Iteration 5 to 10: infection, terminal, lurker, and reanimation systems were reshaped into a staged death and resurrection flow.
- Iteration 11 to 16: infection follow through, corpse handling, sick spew polish, and related stability work.
- Iteration 17 to 23: zombie flooring, texture scale work, acid and blood damage over time, filth variation support, and runt child enforcement.
- Iteration 24 to 29: runts were added, boomer runt spawning was added, naming cleanup was handled, and debug spawn support expanded.
- Iteration 30 to 32: settings cleanup, leftover cleanup, pregnant boomer natural spawn work, and README documentation upgrades.
- Iteration 33: rotten leather furniture was added, including a functional chair and bed with directional textures.
- Iteration 34: rotten leather bed art was rebuilt with cleaner vanilla inspired north, east, and south textures.
- Iteration 35: rotten leather bed defs were first aligned around the vanilla single bed layout, but the footprint note in the README was later corrected after testing.
- Iteration 36: rotten leather bed north, east, and south textures were rebuilt again to properly fit the standard single bed footprint.
- Iteration 37: an in game pause menu shortcut was added so Zed Zed Zed settings can be opened directly from the pause menu.
- Iteration 38: the settings UI got more breathing room and the settings text was rewritten to read more naturally.
- Iteration 39: the map counter was rebuilt into a smaller themed HUD card that uses the custom family name and shows danger as a percentage.
- Iteration 40: the counter was simplified into a compact two line readout with a clearer Danger label.
- Iteration 41: the README gained a dated feature ledger so the mod's main systems can be tracked more clearly over time.
- Iteration 42: the pause menu settings shortcut build error was fixed by correcting the Quit to OS shutdown call.
- Iteration 43: the README and About text were rewritten to sound more natural, better explain the mod, and follow a cleaner workshop style structure.
- Iteration 44: the map counter HUD card got a little more vertical room so the lower line no longer clips at the bottom.
- Iteration 45: the rotten leather bed was corrected to use the exact wooden bed footprint and behavior, while keeping its custom rotten leather textures and material.

## Changelog

This changelog treats a **major update** as something that adds substantial new features, new items, or meaningful gameplay changes. Cleanup, build fixes, wording passes, layout fixes, art alignment work, and small UI improvements are listed as **minor updates**.

### Major updates

#### 2026-04-08 - outbreak expansion
- Added the runt strain as a real new zombie type with debug spawning and rare natural spawns.
- Added pregnant boomers as a gameplay feature instead of leaving them as a debug only idea.
- Added zombie acid corrosion, zombie blood exposure, and the Puked On lure debuff.
- Reworked post death infection and reanimation so corpses can keep progressing after death and rise through a clearer staged system.
- Added rotten leather furniture and expanded rotten leather floors with multiple variants.

### Minor updates

#### 2026-04-09 - counter layout cleanup
- Gave the map counter HUD card a little more height so the lower Danger line no longer clips at the bottom.

#### 2026-04-09 - rotten leather bed footprint fix
- Corrected the rotten leather bed so it now uses the same single bed footprint and behavior as the standard wooden bed while keeping the rotten leather material and custom textures.

#### 2026-04-09 - documentation voice pass
- Rewrote the About description to sound more natural and clearly explain what the mod does.
- Rewrote the README so it reads more like a workshop page and explains the mod's features in plain language.
- Kept the dated feature ledger and updated it with this documentation pass.

#### 2026-04-09 - HUD and settings quality of life pass
- Added a pause menu shortcut that opens Zed Zed Zed settings directly from the game menu.
- Gave the settings menu more vertical breathing room to reduce clipping and overlap.
- Rewrote the settings descriptions and tab help text to read more naturally.
- Rebuilt the map counter into a smaller themed HUD card.
- Changed the counter to respect the custom family name and show Danger as a percentage.

#### 2026-04-09 - build repair
- Fixed the GitHub build failure caused by calling `Root.Shutdown()` through an instance reference.
- Corrected the pause menu shortcut code so the `Quit to OS` button uses the proper static shutdown call.

#### 2026-04-08 - rotten leather art and footprint cleanup
- Repainted the rotten leather bed north, east, and south sprites in a cleaner RimWorld style pass.
- Set the rotten leather bed to use the normal wooden single bed footprint and matched the draw size to it.
- Reworked the bed sprites again so they fit that footprint in game.

#### 2026-04-07 - stability and readability pass
- Fixed early compile and XML issues that were blocking the mod from loading or building cleanly.
- Improved visible infection feedback.
- Improved lurker conversion handling.
- Reworked sick zombie spew presentation inside the custom zombie attack system.
