using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public class Recipe_InjectZombieBile : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            Pawn patient = thing as Pawn;
            if (patient == null)
            {
                return false;
            }

            if (patient.Dead)
            {
                return false;
            }

            if (patient.RaceProps?.Humanlike != true)
            {
                return false;
            }

            if (ZombieLurkerUtility.IsLurker(patient))
            {
                return false;
            }

            if (ZombieRulesUtility.IsZombie(patient))
            {
                return false;
            }

            return true;
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            int doctorMedical = billDoer?.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0;
            bool surgeryFailed = CheckSurgeryFail(billDoer, pawn, ingredients, part, bill);
            if (surgeryFailed && !ShouldConvertFailureToSuccess(doctorMedical))
            {
                return;
            }

            if (!ZombieBileUtility.TryInjectZombieBile(pawn, billDoer) && Prefs.DevMode)
            {
                Log.Warning("[Zed Zed Zed] Zombie bile injection failed for " + (pawn?.LabelShortCap ?? "unknown patient") + ".");
            }
        }

        private static bool ShouldConvertFailureToSuccess(int doctorMedical)
        {
            if (doctorMedical < 3)
            {
                return false;
            }

            float failOverrideChance;
            if (doctorMedical >= 20)
            {
                failOverrideChance = 0.995f;
            }
            else if (doctorMedical >= 15)
            {
                failOverrideChance = 0.98f;
            }
            else if (doctorMedical >= 10)
            {
                failOverrideChance = 0.93f;
            }
            else if (doctorMedical >= 6)
            {
                failOverrideChance = 0.80f;
            }
            else
            {
                failOverrideChance = 0.60f;
            }

            return Rand.Value < failOverrideChance;
        }
    }
}
