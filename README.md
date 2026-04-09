```text
 ______          _   ______          _   ______
|___  /         | | |___  /         | | |___  /
   / /  ___   __| |    / /  ___   __| |    / / 
  / /  / _ \ / _` |   / /  / _ \ / _` |   / /  
./ /__| (_) | (_| | ./ /__| (_) | (_| | ./ /__ 
\_____/\___/ \__,_| \_____/\___/ \__,_| \_____|

        R I M W O R L D   Z O M B I E   M O D
```

# Zed Zed Zed

Zed Zed Zed is a zombie horde mod for RimWorld focused on pressure, atmosphere, and messy colony defense. The undead do not always behave the same way. Some drift in from the edges, some huddle and wait to be stirred up, some shove toward the colony, and some claw their way out of the ground or gather around graves that have to be destroyed to stop the flow.

This repository is the full source package for the mod, along with a GitHub Actions workflow that builds a ready to install version of the mod.

## Main features

- configurable zombie variants, including biters, runts, boomers, sick, drowned, brutes, and grabbers
- edge trickles, huddled packs, colony pushes, ground bursts, moon events, blood moons, and grave events
- tainted zombie apparel, weak limbs, reanimation, and variant specific behavior
- debug controls for forcing major zombie event types during testing
- custom overlays and faction visuals tuned for a RimWorld style look
- GitHub build workflow that outputs an installable mod zip

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

This source package is currently at **iteration 33** of the active development cycle in this build set.

Iteration history:
- Iteration 1 to 4: initial compile fixes, XML cleanup, butchering fix, and the first sick spew overhaul
- Iteration 5 to 10: infection, terminal, lurker, and reanimation systems reworked into a staged death and resurrection flow
- Iteration 11 to 16: sick spew presentation polish, dead infection corpse tracking, and related build fixes
- Iteration 17 to 23: zombie flooring, texture scale work, acid and blood damage over time, filth variation support, and runt child enforcement
- Iteration 24 to 29: crawler creature added, boomer crawler spawning added, naming cleanup, and debug spawn support
- Iteration 30 to 32: settings cleanup, leftover system cleanup, pregnant boomer natural spawn pass, and README documentation upgrades
- Iteration 33: rotten leather furniture added, including a functional chair and bed with directional textures

## Major update changelog

Only major updates are listed here. Small single line tweaks are intentionally left out.

### 2026-04-08
- Added a rotten leather chair and rotten leather bed as fully buildable furniture that use rotten leather specifically instead of generic leather
- Added north, east, and south directional textures for both rotten leather furniture pieces
- Added a dedicated rotten leather stuff category so the new furniture can be restricted cleanly to rotten leather while leaving other leathery crafting behavior alone
- Added the crawler creature as a new zombie critter with directional textures, fast movement, low damage, debug spawning, and rare natural spawns with biter groups
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

