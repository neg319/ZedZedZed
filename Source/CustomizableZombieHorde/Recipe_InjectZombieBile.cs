using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public class Recipe_InjectZombieBile : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part))
            {
                return false;
            }

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
