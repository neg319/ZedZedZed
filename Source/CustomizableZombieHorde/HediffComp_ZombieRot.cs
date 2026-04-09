using Verse;

namespace CustomizableZombieHorde
{
    public sealed class HediffCompProperties_ZombieRot : HediffCompProperties
    {
        public HediffCompProperties_ZombieRot()
        {
            compClass = typeof(HediffComp_ZombieRot);
        }
    }

    public sealed class HediffComp_ZombieRot : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn == null || !Pawn.Spawned || Pawn.IsHashIntervalTick(180) == false)
            {
                return;
            }

            ZombieUtility.SetZombieDisplayName(Pawn);
            ZombieUtility.StripAllUsableItems(Pawn);
            ZombieUtility.MarkZombieApparelTainted(Pawn, degradeApparel: false);
        }
    }
}
