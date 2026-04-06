using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class Projectile_SickSpit : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 cell = Position;
            Pawn sourcePawn = Launcher as Pawn;

            if (map != null && cell.IsValid && cell.InBounds(map))
            {
                FilthMaker.TryMakeFilth(cell, map, ZombieDefOf.CZH_Filth_SickZombieBlood);

                if (hitThing is Pawn hitPawn && !hitPawn.Dead && !ZombieUtility.ShouldZombiesIgnore(hitPawn))
                {
                    ZombieTraitUtility.TryApplyZombieSickness(hitPawn, 0.30f);
                }

                foreach (IntVec3 nearby in GenRadial.RadialCellsAround(cell, 1.1f, true))
                {
                    if (!nearby.InBounds(map))
                    {
                        continue;
                    }

                    if (Rand.Chance(0.55f))
                    {
                        FilthMaker.TryMakeFilth(nearby, map, ZombieDefOf.CZH_Filth_SickZombieBlood);
                    }

                    foreach (Thing thing in nearby.GetThingList(map))
                    {
                        if (thing is Pawn pawn && pawn != sourcePawn && !pawn.Dead && !ZombieUtility.ShouldZombiesIgnore(pawn))
                        {
                            ZombieTraitUtility.TryApplyZombieSickness(pawn, thing == hitThing ? 0.30f : 0.12f);
                        }
                    }
                }
            }

            base.Impact(hitThing, blockedByShield);
        }
    }
}
