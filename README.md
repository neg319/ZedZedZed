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

# Zed Zed Zed

Zed Zed Zed is a RimWorld zombie mod built around steady pressure, ugly cleanup, and nights that can get out of hand fast. The goal is not to spam one kind of raid over and over. The goal is to make the map feel infected. Zombies drift in, bunch up, push the base, claw out of the ground, spill out of graves, and sometimes move across the whole map in a herd.

This version is tuned around a simple combat rule. Zombies are supposed to be easy to kill. They die like normal pawns, any head or face hit destroys the brain immediately, and any solid hit has a chance to destroy the brain outright. Guns are loud and draw attention. Melee is quieter and usually pulls less of the map.

## What the mod is trying to do right now

- Keep normal map pressure steady with trickles, huddles, base pushes, ground bursts, graves, moon attacks, and herds.
- Make the undead feel dangerous because of numbers, angles, noise, and map pressure, not because each individual zombie is a bullet sponge.
- Let outbreak intensity act like the main pressure dial. It now scales up to 12x and directly affects the soft cap the mod tries to maintain on the map.
- Keep night scary. Zombies move 50% faster at night and the target population also rises by 50%.
- Make drowned zombies feel different. They want to stay in water when they are not actively hunting, and they do better in water, rain, and wet conditions.
- Let loud weapons wake up the area. Gunfire should attract more zombies than a pawn using quiet melee.
- Keep infection on the colony side. Living pawns can still suffer zombie infection, use bile treatment, or be turned into a lurker through the bile surgery route.

## Strains at a glance

**Biters**  
The baseline zombie. Most of the horde is built out of these.

**Bone Biters**  
Corpse eating skeletal biters that break off to feed when they find bodies.

**Runts**  
Small, nasty, clutter making zombies that are good at causing problems in tight spaces.

**Boomers**  
Bloated acid bombs. They punish bunching up and can leave a mess behind.

**Sick**  
Disease spreading zombies that add filth, bile pressure, and infection problems.

**Drowned**  
Water loving zombies that prefer to stay wet, move differently around water, and feel better in rain.

**Brutes**  
Big hitters. They are still meant to go down under focused fire, but they hit harder than the rest.

**Grabbers**  
Control focused zombies that pin pawns in place and force a break free moment.

**Lurkers**  
A quieter colony side zombie. They can show up through special systems and can be captured, recruited, or enslaved.

## Colony side features

- Zombie infection with visible stages
- Bile med kits for treatment
- Zombie bile surgery that turns a living pawn into a colony lurker
- Rotten flesh, rotten leather, zombie jerky, zombie bile, and gold teeth
- Rotten leather furniture and floors
- Grave events that keep producing zombies until the grave is destroyed
- A home map HUD for live zombie count and Danger
- Renaming for the zombie family and each strain
- Pause menu shortcut for settings
- Debug tools for forcing events and spawning test zombies

## Settings overview

**Overview** keeps the fast setup controls together.  
**Events** controls trickles, herd timing, graves, moon events, and outbreak pacing.  
**Variants** lets you turn strains on or off.  
**Names** keeps all naming fields in one place.  
**Colony** covers cleanup behavior, corpse handling, and infection timing.  
**Debug** is there for testing and screenshots.

The settings text has been cleaned up to match the current direction of the mod, especially around combat, herd behavior, drowned behavior, and outbreak intensity.

## Build notes

This repository is the full source package for the mod. It includes the GitHub Actions workflow used to build the project into a ready to install RimWorld mod zip.

## Curated changelog

### Iteration 139, April 13, 2026
- Rewrote the README, About info, and settings copy to match the current design of the mod.
- Trimmed old noise out of the changelog so the project notes are easier to read.
- Cleaned up player facing descriptions so the mod reads more like the game it currently is.

### Iteration 138
- Any successful damaging body hit can now have a chance to destroy a zombie's brain. Head and face hits destroy the brain immediately.
- Head and face hits still stay immediately lethal.

### Iteration 137
- Zombies were pushed much harder toward the easy to kill direction.
- Head and face hits were made instantly decisive.

### Iteration 136
- Zombies were switched back to normal pawn death flow.
- The old zombie only fake death and corpse rise handling was removed from normal combat.

### Iteration 135
- Smoothed zombie movement by reducing overly aggressive job stripping and retarget spam.
- Gunfire attraction was softened so hordes notice shooters without constantly stuttering.

### Iteration 131
- All zombies now move 50% faster at night.

### Iteration 130
- Herds were changed to cross the map properly and leave from the far side instead of peeling off the nearest edge.
- Herd shape was loosened so it looks more natural.
- Outbreak intensity was expanded to 12x and tied more clearly to the on map soft cap.

### Iteration 129
- Loud ranged weapons now attract nearby zombies much more than quiet melee.
- Drowned got consistent mood support for water, wet, and rain.
- Biters got a rare clothing roll.

### Iteration 128
- Drowned water return behavior was cleaned up so they walk back into water instead of jittering at shorelines.
- Wet conditions were made more rewarding for drowned.

### Iteration 126
- Zombie pathing was shifted to a more stable visitor style destination scoring system without giving zombies actual visitor leave map behavior.

### Iteration 117 and earlier
- Infection staging, naming cleanup, debug tools, colony cleanup rules, graves, bile items, furniture, and the broad strain roster were all built up across earlier iterations.
- Older micro updates and dead end notes have been trimmed out of this README so it stays readable.

## Current design summary

The undead should feel oppressive because they are always around, always making noise, always turning one bad angle into three more problems. They should not feel like immortal tanks. If a colonist has a gun, good positioning, and a little room to breathe, that pawn should absolutely be able to drop a bunch of zombies before the line collapses.
