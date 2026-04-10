using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieBileUtility
    {
        public static int GetButcheredBileCount(Pawn pawn)
        {
            if (pawn == null || !ZombieRulesUtility.IsZombie(pawn))
            {
                return 0;
            }

            ZombieVariant variant = ZombieVariantUtility.GetVariant(pawn);
            ZombieButcherProfile profile = ZombieVariantUtility.GetButcherProfile(variant);
            if (profile == null || !profile.CanDropBile)
            {
                return 0;
            }

            int seed = Gen.HashCombineInt(pawn.thingIDNumber, 8179 + (int)variant * 53);
            Rand.PushState(seed);
            try
            {
                if (!Rand.Chance(profile.BileChance))
                {
                    return 0;
                }

                return Rand.RangeInclusive(profile.BileMinCount, profile.BileMaxCount);
            }
            finally
            {
                Rand.PopState();
            }
        }

        public static bool NeedsBileTreatment(Pawn pawn)
        {
            return ZombieInfectionUtility.CanCureZombieInfection(pawn);
        }

        public static Thing FindCarriedBileTreatmentKit(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            Thing carried = pawn.carryTracker?.CarriedThing;
            if (IsBileTreatmentKit(carried))
            {
                return carried;
            }

            if (pawn.inventory?.innerContainer != null)
            {
                for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                {
                    Thing thing = pawn.inventory.innerContainer[i];
                    if (IsBileTreatmentKit(thing))
                    {
                        return thing;
                    }
                }
            }

            return null;
        }

        public static bool IsBileTreatmentKit(Thing thing)
        {
            return thing?.def == ZombieDefOf.CZH_BileTreatmentKit && thing.stackCount > 0;
        }

        public static bool ConsumeOneBileTreatmentKit(Pawn pawn, Thing preferredKit = null)
        {
            Thing kit = IsBileTreatmentKit(preferredKit) ? preferredKit : FindCarriedBileTreatmentKit(pawn);
            if (kit == null)
            {
                return false;
            }

            if (kit.stackCount > 1)
            {
                kit.stackCount -= 1;
            }
            else
            {
                try
                {
                    kit.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }


        public static bool TryInjectZombieBile(Pawn patient, Pawn doctor = null)
        {
            if (patient == null || patient.Dead || patient.RaceProps?.Humanlike != true)
            {
                return false;
            }

            if (ZombieRulesUtility.IsZombie(patient) || ZombieLurkerUtility.IsLurker(patient))
            {
                return false;
            }

            Map map = patient.MapHeld ?? doctor?.MapHeld;
            PawnKindDef lurkerKind = ZombieKindSelector.GetKindForVariant(ZombieVariant.Lurker, map);
            if (lurkerKind == null)
            {
                return false;
            }

            ZombieInfectionUtility.RemoveZombieInfection(patient, Current.Game?.GetComponent<ZombieGameComponent>());
            ZombiePawnFactory.ConvertExistingPawnToZombie(patient, lurkerKind, Faction.OfPlayer, preserveName: true, preserveSkills: true, preserveRelations: true, initialSpawn: false);
            ZombieLurkerUtility.EnsureColonyLurkerState(patient, emergencyStabilize: true, stopCurrentJobs: true);
            ZombieUtility.MarkPawnGraphicsDirty(patient);

            if (doctor?.skills != null)
            {
                doctor.skills.Learn(SkillDefOf.Medicine, 300f);
            }

            ZombieFeedbackUtility.SendBileInjectionMessage(patient, doctor);
            return true;
        }

        public static bool CureZombieSickness(Pawn patient, Pawn doctor = null)
        {
            if (patient?.health?.hediffSet == null)
            {
                return false;
            }

            Hediff sickness = patient.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieSickness);
            if (sickness == null || !ZombieInfectionUtility.CanCureZombieInfection(patient))
            {
                return false;
            }

            ZombieInfectionUtility.RemoveZombieInfection(patient, Current.Game?.GetComponent<ZombieGameComponent>());

            if (doctor?.skills != null)
            {
                doctor.skills.Learn(SkillDefOf.Medicine, 220f);
            }

            ZombieFeedbackUtility.SendBileTreatmentMessage(patient, doctor);
            return true;
        }
    }
}
