# Zed Zed Zed

Zed Zed Zed is a configurable zombie horde mod for RimWorld.

## What this package is for

This package is prepared for GitHub so you can upload it to a repository, run one GitHub Actions workflow, and download an installable mod zip.

## What to upload to GitHub

Upload everything in this folder to your repository.

If GitHub on iPhone refuses to upload hidden files or folders, manually create this file in the repo and paste in the matching visible copy from `CREATE_ON_GITHUB_build.yml.txt`:

`.github/workflows/build.yml`

## How to build on GitHub

1. Upload the repo files.
2. Open the **Actions** tab.
3. Open **Build Zed Zed Zed**.
4. Press **Run workflow**.
5. Wait for the workflow to finish.
6. Download the `ZedZedZed-InstallZip` artifact.
7. Unzip it.
8. Put the `ZedZedZed` folder into your RimWorld `Mods` folder.
9. Make sure the Harmony mod is installed and enabled.
10. Enable **Zed Zed Zed** in RimWorld.

## GitHub build output

The workflow builds:

- `Assemblies/ZedZedZed.dll`
- an installable `ZedZedZed` mod folder
- `ZedZedZed_install.zip`

## Notes

- This workflow uses NuGet reference packages for RimWorld and Harmony, so it does not require a self-hosted runner.
- The repository keeps the source files. The workflow artifact gives you the installable mod package.
- The mod still depends on the Harmony mod at runtime.


Runtime hotfix: custom rotten flesh, rotten leather, and filth graphics now use single-file graphics so RimWorld 1.6 can load them without collection folders.
