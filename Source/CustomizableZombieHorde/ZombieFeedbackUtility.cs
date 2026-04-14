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

            Messages.Message(pawn.LabelShortCap + " has contracted zombie sickness" + locationText + ". It worsens over time, stays treatable until 60%, becomes terminal after that, falls into a living coma at 80%, and if left untreated transforms into a colony lurker at 99%." + cureText, pawn, MessageTypeDefOf.NegativeHealthEvent);
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
                return "Butchering this corpse will yield " + countLabel + " zombie bile.";
            }

            int percent = (int)System.Math.Round(profile.BileChance * 100f);
            return "Butchering this corpse has a " + percent + "% chance to yield " + countLabel + " zombie bile.";
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
                lines.Add("Passive lurker. It will not attack colonists and other undead ignore it.");
                lines.Add("Right click with a colonist carrying rotten flesh or human meat to try taming it.");
            }
            else if (ZombieLurkerUtility.IsColonyLurker(pawn))
            {
                lines.Add("Tamed lurker. Other undead should ignore this colony member.");
            }


            if (ZombieInfectionUtility.HasZombieInfection(pawn))
            {
                if (ZombieBileUtility.NeedsBileTreatment(pawn))
                {
                    Hediff localizedInfection = ZombieInfectionUtility.GetZombieInfection(pawn);
                    string localizedPartText = localizedInfection?.Part != null ? " If the infected limb is removed before terminal, that also cures it." : string.Empty;
                    lines.Add("Zombie sickness: " + ZombieInfectionUtility.GetInfectionCompletionLabel(pawn) + " complete. It can be cured with a bile med kit before it reaches the terminal bloodstream stage at 60%." + localizedPartText);
                }
                else if (ZombieInfectionUtility.IsTerminal(pawn))
                {
                    lines.Add("Zombie sickness: terminal, " + ZombieInfectionUtility.GetInfectionCompletionLabel(pawn) + " complete. Terminal begins at 60%, coma begins at 80%, and if the pawn survives to 99% they transform into a colony lurker.");
                }
                else if (ZombieInfectionUtility.HasReanimatedState(pawn))
                {
                    lines.Add("Zombie sickness: fully turned, " + ZombieInfectionUtility.GetInfectionCompletionLabel(pawn) + " complete. This pawn can no longer be cured.");
                }
            }
            else if (ZombieInfectionUtility.HasReanimatedState(pawn))
            {
                lines.Add("Fully turned. This pawn can no longer be cured.");
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
            if (ZombieInfectionUtility.HasZombieInfection(innerPawn) || ZombieInfectionUtility.HasReanimatedState(innerPawn))
            {
                string infectionLine = ZombieInfectionUtility.HasZombieInfection(innerPawn)
                    ? "Zombie infection was present at death, but dead infected pawns stay dead in the current build."
                    : "Transformation completed before death, but dead infected pawns stay dead in the current build.";
                lines.Add(infectionLine);
            }

            if (ZombieRulesUtility.IsZombie(innerPawn))
            {
                lines.Add("This zombie is dead. Zombies now die like other pawns, but head shots are especially effective against them.");
            }

            if (ZombieRulesUtility.IsZombie(innerPawn))
            {
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
