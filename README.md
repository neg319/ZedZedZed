# Zed Zed Zed

Zed Zed Zed is a zombie horde mod for RimWorld focused on pressure, atmosphere, and messy colony defense. The undead do not always behave the same way. Some drift in from the edges, some huddle and wait to be stirred up, some shove toward the colony, and some claw their way out of the ground or gather around graves that have to be destroyed to stop the flow.

This repository is the full source package for the mod, along with a GitHub Actions workflow that builds a ready to install version of the mod.

## Main features

- configurable zombie variants, including biters, crawlers, boomers, sick, drowned, heavies, and grabbers
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
test
