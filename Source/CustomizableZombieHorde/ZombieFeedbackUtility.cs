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
                "A passive lurker has wandered onto the map. It will not attack colonists. Right click it with rotten flesh or human meat to try taming it.",
                LetterDefOf.NeutralEvent,
                new TargetInfo(cell, map));
        }

        public static void SendLurkerTamedMessage(Pawn lurker, Pawn tamer)
        {
            string tamerName = tamer?.LabelShortCap ?? "A colonist";
            Messages.Message(tamerName + " has tamed a lurker. It joins the colony and other undead ignore it.", lurker, MessageTypeDefOf.PositiveEvent);
        }

        public static void SendLurkerTameFailedMessage(Pawn lurker)
        {
            Messages.Message("The lurker stays wild. Carry rotten flesh or human meat to try again.", lurker, MessageTypeDefOf.NeutralEvent);
        }

        public static void SendBileTreatmentMessage(Pawn patient, Pawn doctor)
        {
            string doctorName = doctor?.LabelShortCap ?? "A colonist";
            Messages.Message(doctorName + " uses a bile med kit and cures zombie sickness in " + patient.LabelShortCap + ".", patient, MessageTypeDefOf.PositiveEvent);
        }

        public static void SendBileInjectionMessage(Pawn patient, Pawn doctor)
        {
            string doctorName = doctor?.LabelShortCap ?? "A colonist";
            Messages.Message(doctorName + " injects zombie bile into " + patient.LabelShortCap + ". They survive and become a colony lurker.", patient, MessageTypeDefOf.NeutralEvent);
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
                ? " Remove the limb before the infection turns terminal to stop it."
                : string.Empty;

            Messages.Message(pawn.LabelShortCap + " has contracted zombie sickness" + locationText + ". At 99%, the pawn becomes a colony lurker." + cureText, pawn, MessageTypeDefOf.NegativeHealthEvent);
        }


        public static void SendZombieTurnMessage(Pawn pawn, bool becameLurker)
        {
            if (pawn == null)
            {
                return;
            }

            string text = becameLurker
                ? pawn.LabelShortCap + " finishes the transformation and becomes a lurker."
                : pawn.LabelShortCap + " finishes the transformation and joins the horde.";
            Messages.Message(text, pawn, MessageTypeDefOf.NegativeEvent);
        }

        public static void SendLivingTransformationMessage(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            Messages.Message(pawn.LabelShortCap + " completes the transformation and becomes a colony lurker.", pawn, MessageTypeDefOf.NegativeEvent);
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
            Messages.Message(grabber.LabelShortCap + " grabs " + prey.LabelShortCap + " and pins them in place.", prey, MessageTypeDefOf.NegativeEvent);
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
        }

        private static string GetButcherBileInspectLabel(ZombieButcherProfile profile)
        {
            if (profile == null || !profile.CanDropBile)
            {
                return null;
            }

            int min = profile.BileMinCount < 1 ? 1 : profile.BileMinCount;
            int max = profile.BileMaxCount < min ? min : profile.BileMaxCount;
            string countLabel = min == max ? min.ToString() : min + " to " + max;

            if (profile.BileChance >= 0.999f)
            {
                return "Butchering this corpse yields " + countLabel + " zombie bile.";
            }

            int percent = (int)System.Math.Round(profile.BileChance * 100f);
            return "This corpse has a " + percent + "% chance to yield " + countLabel + " zombie bile.";
        }

        public static string GetPawnInspectString(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            List<string> lines = new List<string>();
            string feignDeathLine = ZombieFeignDeathUtility.GetFeignDeathInspectString(pawn);
            if (!feignDeathLine.NullOrEmpty())
            {
                lines.Add(feignDeathLine);
            }

            if (ZombieLurkerUtility.IsPassiveLurker(pawn))
            {
                lines.Add("Passive lurker. It will not attack colonists.");
                lines.Add("Right click with rotten flesh or human meat to try taming it.");
            }
            else if (ZombieLurkerUtility.IsColonyLurker(pawn))
            {
                lines.Add("Tamed lurker. Other undead ignore this colony member.");
            }


            if (!ZombieUtility.IsZombie(pawn))
            {
                if (ZombieInfectionUtility.HasZombieInfection(pawn))
                {
                    Hediff infection = ZombieInfectionUtility.GetZombieInfection(pawn);
                    string completionLabel = ZombieInfectionUtility.GetInfectionCompletionLabel(pawn);
                    string stageLabel = ZombieInfectionUtility.GetInfectionStageLabel(infection);

                    if (ZombieBileUtility.NeedsBileTreatment(pawn))
                    {
                        string localizedPartText = infection?.Part != null ? " Remove the infected limb before terminal to cure it." : string.Empty;
                        lines.Add("Zombie sickness: " + stageLabel + ", " + completionLabel + " complete. A bile med kit can cure it before 60%." + localizedPartText);
                    }
                    else if (ZombieInfectionUtility.IsComatose(pawn))
                    {
                        lines.Add("Zombie sickness: " + stageLabel + ", " + completionLabel + " complete. The pawn is unconscious but alive. At 99%, they become a colony lurker.");
                    }
                    else if (ZombieInfectionUtility.IsInTransformationStage(pawn))
                    {
                        lines.Add("Zombie sickness: " + stageLabel + ", " + completionLabel + " complete. Final stage. At 99%, they become a colony lurker.");
                    }
                    else if (ZombieInfectionUtility.IsTerminal(pawn))
                    {
                        lines.Add("Zombie sickness: " + stageLabel + ", " + completionLabel + " complete. Terminal begins at 60%. Coma begins at 80%. Transformation begins at 90%.");
                    }
                    else if (ZombieInfectionUtility.HasReanimatedState(pawn))
                    {
                        lines.Add("Zombie sickness: fully turned, " + completionLabel + " complete. This pawn can no longer be cured.");
                    }
                }
                else if (ZombieInfectionUtility.HasReanimatedState(pawn))
                {
                    lines.Add("Fully turned. This pawn cannot be cured.");
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

            if (ZombieRulesUtility.IsZombie(innerPawn))
            {
                lines.Add("Brain destroyed. This zombie stays dead.");

                ZombieButcherProfile profile = ZombieVariantUtility.GetButcherProfile(ZombieVariantUtility.GetVariant(innerPawn));
                if (profile != null && profile.CanDropBile)
                {
                    string butcherLabel = GetButcherBileInspectLabel(profile);
                    if (!butcherLabel.NullOrEmpty())
                    {
                        lines.Add(butcherLabel);
                    }
                }
            }

            return lines.Count == 0 ? null : string.Join("\n", lines);
        }
    }
}
