using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieFeedbackUtility
    {
        private static readonly Dictionary<int, int> LastGrabberWarningTickByTarget = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> LastReanimationWarningTickByPawn = new Dictionary<int, int>();

        public static void SendLurkerArrivalLetter(Map map, IntVec3 cell)
        {
            if (map == null)
            {
                return;
            }

            Find.LetterStack.ReceiveLetter(
                "A passive lurker has wandered in",
                "A passive lurker has wandered onto the map. It does not join the horde, it does not attack colonists, and other undead should ignore it. Right click it with a colonist carrying rotten flesh or human meat to try taming it.",
                LetterDefOf.NeutralEvent,
                new TargetInfo(cell, map));
        }

        public static void SendLurkerTamedMessage(Pawn lurker, Pawn tamer)
        {
            string tamerName = tamer?.LabelShortCap ?? "A colonist";
            Messages.Message(tamerName + " has tamed a lurker. It joins the colony and remains ignored by other undead.", lurker, MessageTypeDefOf.PositiveEvent);
        }

        public static void SendLurkerTameFailedMessage(Pawn lurker)
        {
            Messages.Message("The lurker shies away and remains wild. Carry rotten flesh or human meat to try again.", lurker, MessageTypeDefOf.NeutralEvent);
        }

        public static void SendBileTreatmentMessage(Pawn patient, Pawn doctor)
        {
            string doctorName = doctor?.LabelShortCap ?? "A colonist";
            Messages.Message(doctorName + " administers a bile med kit and cures zombie sickness in " + patient.LabelShortCap + ".", patient, MessageTypeDefOf.PositiveEvent);
        }

        public static void SendZombieSicknessMessage(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            Messages.Message(pawn.LabelShortCap + " has contracted zombie sickness. It reduces movement and consciousness until treated. Administer a bile med kit to cure it.", pawn, MessageTypeDefOf.NegativeHealthEvent);
        }

        public static void TrySendGrabberPullWarning(Pawn prey, Pawn grabber)
        {
            if (prey == null || grabber == null || !prey.IsColonistPlayerControlled)
            {
                return;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            if (LastGrabberWarningTickByTarget.TryGetValue(prey.thingIDNumber, out int lastTick) && ticksGame - lastTick < 900)
            {
                return;
            }

            LastGrabberWarningTickByTarget[prey.thingIDNumber] = ticksGame;
            Messages.Message(grabber.LabelShortCap + " grabs " + prey.LabelShortCap + " and pins them in place. They start struggling to break free.", prey, MessageTypeDefOf.NegativeEvent);
        }

        public static void TrySendGrabberEscapeMessage(Pawn prey, Pawn grabber)
        {
            if (prey == null || !prey.IsColonistPlayerControlled)
            {
                return;
            }

            Messages.Message(prey.LabelShortCap + " breaks free from " + (grabber?.LabelShortCap ?? "the grabber") + ".", prey, MessageTypeDefOf.NeutralEvent);
        }

        public static void TrySendReanimationWarning(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || !pawn.Map.IsPlayerHome)
            {
                return;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            if (LastReanimationWarningTickByPawn.TryGetValue(pawn.thingIDNumber, out int lastTick) && ticksGame - lastTick < 2500)
            {
                return;
            }

            LastReanimationWarningTickByPawn[pawn.thingIDNumber] = ticksGame;
            Messages.Message(pawn.LabelShortCap + " rises again. Destroy the head if you want it to stay down.", pawn, MessageTypeDefOf.NegativeEvent);
        }

        public static string GetPawnInspectString(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            List<string> lines = new List<string>();
            if (ZombieLurkerUtility.IsPassiveLurker(pawn))
            {
                lines.Add("Passive lurker. It will not attack colonists and other undead ignore it.");
                lines.Add("Right click with a colonist carrying rotten flesh or human meat to try taming it.");
            }
            else if (ZombieLurkerUtility.IsColonyLurker(pawn))
            {
                lines.Add("Tamed lurker. Other undead should ignore this colony member.");
            }

            if (ZombieBileUtility.NeedsBileTreatment(pawn))
            {
                lines.Add("Zombie sickness: this can be cured with a bile med kit.");
            }

            return lines.Count == 0 ? null : string.Join("\n", lines);
        }

        public static string GetCorpseInspectString(Corpse corpse)
        {
            Pawn innerPawn = corpse?.InnerPawn;
            if (innerPawn == null)
            {
                return null;
            }

            List<string> lines = new List<string>();
            if (ZombieRulesUtility.CanReanimate(innerPawn))
            {
                lines.Add("This corpse may rise again unless the head is ruined.");
            }

            if (ZombieRulesUtility.IsZombie(innerPawn))
            {
                ZombieButcherProfile profile = ZombieVariantUtility.GetButcherProfile(ZombieVariantUtility.GetVariant(innerPawn));
                if (profile != null && profile.CanDropBile)
                {
                    lines.Add("Butchering this special corpse may yield zombie bile.");
                }
            }

            return lines.Count == 0 ? null : string.Join("\n", lines);
        }
    }
}
