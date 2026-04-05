using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    [DefOf]
    public static class ZombieDefOf
    {
        public static HediffDef CZH_ZombieRot;
        public static HediffDef CZH_ZombieLimbDecay;
        public static HediffDef CZH_ZombieOpenWound;
        public static HediffDef CZH_ZombieRunner;
        public static HediffDef CZH_ZombieBiter;
        public static HediffDef CZH_ZombieSkeletonBiter;
        public static HediffDef CZH_ZombieCrawler;
        public static HediffDef CZH_ZombieTank;
        public static HediffDef CZH_ZombieDrownedLand;
        public static HediffDef CZH_ZombieDrownedWater;
        public static HediffDef CZH_ZombieSickness;

        public static ThingDef CZH_RottenFlesh;
        public static ThingDef CZH_RottenLeather;
        public static ThingDef CZH_ZombieBile;
        public static ThingDef CZH_BileTreatmentKit;
        public static ThingDef CZH_Filth_ZombieBlood;
        public static ThingDef CZH_Filth_SickZombieBlood;
        public static ThingDef CZH_Filth_ZombieAcid;
        public static ThingDef CZH_SickSpitProjectile;

        public static ThingDef CZH_Grave_Biter;
        public static ThingDef CZH_Grave_Crawler;
        public static ThingDef CZH_Grave_Boomer;
        public static ThingDef CZH_Grave_Sick;
        public static ThingDef CZH_Grave_Drowned;
        public static ThingDef CZH_Grave_Tank;
        public static ThingDef CZH_Grave_Grabber;

        public static DamageDef CZH_ZombieAcidBurn;

        public static TraitDef CZH_Trait_ZombieSickImmune;
        public static TraitDef CZH_Trait_HeadHunter;
        public static TraitDef CZH_Trait_DeadScent;
        public static TraitDef CZH_Trait_ZombiePassive;
        public static TraitDef CZH_Trait_HardToKill;
        public static TraitDef CZH_Trait_SteadyHands;
        public static TraitDef CZH_Trait_QuickEscape;

        public static IncidentDef CZH_LurkerArrives;
        public static JobDef CZH_TameLurker;
        public static JobDef CZH_RecruitLurker;
        public static JobDef CZH_AdministerBileTreatment;

        static ZombieDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ZombieDefOf));
        }
    }
}
