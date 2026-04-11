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

        public static void SendBileInjectionMessage(Pawn patient, Pawn doctor)
        {
            string doctorName = doctor?.LabelShortCap ?? "A colonist";
            Messages.Message(doctorName + " injects zombie bile into " + patient.LabelShortCap + ". They survive the procedure and become a colony lurker.", patient, MessageTypeDefOf.NeutralEvent);
        }

        public static void SendZombieSicknessMessage(Pawn pawn, BodyPartRecord part = null)
        {
            if (pawn == null)
            {
                return;
            }

            string locationText = part != null
                ? " in the " + (part.Label ?? part.def?.label ?? "limb")
                : string.Empty;
            string cureText = part != null
                ? " If that limb is amputated before the infection turns terminal, the infection can be removed with it."
                : string.Empty;

            Messages.Message(pawn.LabelShortCap + " has contracted zombie sickness" + locationText + ". It worsens over time, becomes terminal at 90%, kills the pawn, then continues through the corpse until it reaches 100% and rises unless the skull is destroyed." + cureText, pawn, MessageTypeDefOf.NegativeHealthEvent);
        }


        public static void SendZombieTurnMessage(Pawn pawn, bool becameLurker)
        {
            if (pawn == null)
            {
                return;
            }

            string text = becameLurker
                ? pawn.LabelShortCap + " dies from zombie infection and rises again as a lurker."
                : pawn.LabelShortCap + " dies from zombie infection and rises again as part of the horde.";
            Messages.Message(text, pawn, MessageTypeDefOf.NegativeEvent);
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


            if (ZombieInfectionUtility.HasReanimatedState(pawn))
            {
                lines.Add("Reanimated: " + ZombieInfectionUtility.GetInfectionCompletionLabel(pawn) + " complete. This pawn has fully turned, can no longer be cured, and if killed will be handled by the global reanimation sweep every few in game seconds once the corpse is ready, unless the head or skull is ruined.");
            }
            else if (ZombieInfectionUtility.HasZombieInfection(pawn))
            {
                if (ZombieBileUtility.NeedsBileTreatment(pawn))
                {
                    Hediff localizedInfection = ZombieInfectionUtility.GetZombieInfection(pawn);
                    string localizedPartText = localizedInfection?.Part != null ? " If the infected limb is removed before terminal, that also cures it." : string.Empty;
                    lines.Add("Zombie sickness: " + ZombieInfectionUtility.GetInfectionCompletionLabel(pawn) + " complete. It worsens over time and can be cured with a bile med kit before it reaches terminal at 90%." + localizedPartText);
                }
                else if (ZombieInfectionUtility.IsTerminal(pawn))
                {
                    lines.Add("Zombie sickness: terminal, " + ZombieInfectionUtility.GetInfectionCompletionLabel(pawn) + " complete. Terminal runs from 90% to 99%, can no longer be cured, and if the pawn dies at any earlier stage the infection keeps climbing from that exact percentage after death until it reaches Reanimated at 100%.");
                }
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

            if (ZombieInfectionUtility.HasReanimatedState(innerPawn) || ZombieInfectionUtility.HasZombieInfection(innerPawn))
            {
                string infectionLine;
                if (ZombieInfectionUtility.HasReanimatedState(innerPawn))
                {
                    infectionLine = "Reanimated: " + ZombieInfectionUtility.GetInfectionCompletionLabel(innerPawn) + " complete. This corpse will try to rise again after its reanimation delay, and a global sweep checks for ready corpses every few in game seconds. ";
                }
                else
                {
                    infectionLine = ZombieInfectionUtility.IsTerminal(innerPawn)
                        ? "Zombie infection is terminal at " + ZombieInfectionUtility.GetInfectionCompletionLabel(innerPawn) + " complete and can no longer be cured. After death the corpse keeps progressing upward from its current percentage until it reaches Reanimated at 100%. "
                        : "Zombie infection is " + ZombieInfectionUtility.GetInfectionCompletionLabel(innerPawn) + " complete and its corpse will keep climbing from this exact percentage after death until it reaches Reanimated at 100%. ";
                }

                ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
                if (component != null && component.TryGetCorpseReanimationTick(corpse, out int wakeTick) && Find.TickManager != null)
                {
                    int ticksLeft = wakeTick - Find.TickManager.TicksGame;
                    if (ticksLeft > 0)
                    {
                        infectionLine += "It should be ready in about " + ticksLeft.ToStringTicksToPeriod() + ".";
                    }
                    else
                    {
                        infectionLine += "It should be ready to rise now.";
                    }
                }
                else
                {
                    infectionLine += "It will keep being checked by the global reanimation sweep unless the corpse is destroyed or prevented from rising.";
                }

                lines.Add(infectionLine);
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
