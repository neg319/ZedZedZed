using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public class Recipe_InjectZombieBile : Recipe_Surgery
    {
        public override AcceptanceReport AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            AcceptanceReport baseReport = base.AvailableOnNow(thing, part);
            if (!baseReport.Accepted)
            {
                return baseReport;
            }

            Pawn patient = thing as Pawn;
            if (patient == null)
            {
                return false;
            }

            if (patient.Dead)
            {
                return "Patient is dead.";
            }

            if (patient.RaceProps?.Humanlike != true)
            {
                return "Requires a humanlike patient.";
            }

            if (ZombieLurkerUtility.IsLurker(patient))
            {
                return "Already a lurker.";
            }

            if (ZombieRulesUtility.IsZombie(patient))
            {
                return "Already undead.";
            }

            return true;
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }

            if (!ZombieBileUtility.TryInjectZombieBile(pawn, billDoer) && Prefs.DevMode)
            {
                Log.Warning("[Zed Zed Zed] Zombie bile injection failed for " + (pawn?.LabelShortCap ?? "unknown patient") + ".");
            }
        }
    }
}
