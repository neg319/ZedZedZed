# Audit Checklist

This file maps the requested feature list to the current source package.

## Core zombie behavior
- Custom zombie family prefix in settings: implemented through `zombiePrefix` and `ZombieDefUtility.ApplyDynamicLabels()`.
- Names like `Walker Biter`, `Walker Boomer`, and `Walker Crawler`: implemented through dynamic PawnKind labels.
- Standard-pawn visual base with dirty overlays and sickly skin colors: implemented through `ZombieVisualUtility` and the overlay atlases in `Textures/PawnOverlays/`.
- Zombies spawn from map edges and move toward the colony: implemented through `ZombieSpawnHelper.SpawnWave()` and `LordJob_AssaultColony`.
- Constant slow edge trickle: implemented in `ZombieGameComponent.HandleTrickleSpawns()`.
- Smaller ground-burst events inside the base: implemented in `ZombieSpawnHelper.SpawnGroundBurst()`.
- Full moon hordes roughly every 30 days with 7-day and 1-day warnings: implemented in `ZombieGameComponent.HandleMoonCycle()`.
- Rare blood moon horde: implemented with `bloodMoonChance`.

## Combat and durability
- No weapon use or weapon pickup retention: implemented through `StripWeaponsAndWeaponInventory()` plus an `AddEquipment` Harmony patch.
- Bite and scratch style combat verbs: implemented on `CZH_ZombieRot`.
- Weaker limbs and more visible decay injuries: implemented through `CZH_ZombieLimbDecay`, `CZH_ZombieOpenWound`, and the application helpers in `ZombieUtility`.
- Reduced health and speed overall: implemented through PawnKind health scales plus `CZH_ZombieRot`.
- Rare fast zombies: implemented through `CZH_ZombieRunner` and `fastZombieChance`.
- Reanimation until the head is damaged or destroyed: implemented in `ZombieGameComponent.HandleZombieReanimation()` and `ZombieUtility.HasHeadDamageOrDestruction()`.
- Tainted zombie apparel: implemented through `MarkZombieApparelTainted()`.

## Variant-specific behavior
- Enable and disable each variant in settings: implemented through the six `allow...` settings.
- Standard zombies common, special variants rare: implemented through `ZombieKindSelector` weighting.
- Boomers explode with acid, not fire: implemented in `ZombieSpecialUtility.DoAcidBurst()`.
- Sick zombies burst with almost black blood and can spread sickness: implemented through `DoSicknessBurst()` and `HandleSickBloodContact()`.
- Drowned zombies spawn from water, stay water-biased, and become weaker on land / stronger in water: implemented through water spawn selection, `HandleDrownedBehavior()`, and the drowned hediff pair.
- Tank zombies are much stronger, visually larger, and very rare: implemented through low spawn weight, body/overlay scaling, and the tank hediff.

## Resources and blood
- All zombies produce dark red blood: implemented through `CZH_Filth_ZombieBlood`.
- Sick zombies use almost black blood: implemented through `CZH_Filth_SickZombieBlood`.
- Zombies butcher into rotten flesh and rotten leather: implemented through butcher patches and `ZombieSpecialUtility.BuildZombieButcherProducts()`.
- Rotten flesh can be processed into leather: implemented through `CZH_RenderRottenFleshToLeather`.

## Time-of-day pressure
- 50 percent fewer daytime spawns and heavier nighttime pressure: implemented in `ZombieSpawnHelper.ApplyTimeOfDayMultiplier()`.

## UI and traits
- Top-corner zombie counter: implemented through the `GlobalControlsOnGUI` patch.
- Difficulty setting adds 25 percent more zombies per level: implemented through `DifficultyMultiplier`.
- Trait: immune to sick-zombie sickness: implemented through `CZH_Trait_ZombieSickImmune`.
- Trait: always aim for the head: implemented through the `PreApplyDamage` Harmony patch.
- Trait: zombies not hostile to that pawn: implemented through the `GenHostility.HostileTo` Harmony patch and drowned-prey filtering.

## Visuals
- Higher-quality unique variant textures: implemented through per-variant overlay atlases.
- Side and back detail for directional movement: included in the current overlay atlas pass.

## Remaining limitation
This package is still a source package. It has not been compiled against a local RimWorld installation in this environment, so a real local build and in-game test are still required.
