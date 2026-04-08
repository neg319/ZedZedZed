using System.Text;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class Hediff_ZombieInfection : HediffWithComps
    {
        public override string LabelInBrackets
        {
            get
            {
                string stage = CurStage?.label;
                string progress = ZombieInfectionUtility.GetInfectionCompletion(this).ToStringPercent();

                if (stage.NullOrEmpty())
                {
                    return progress;
                }

                return stage + ", " + progress;
            }
        }

        public override string SeverityLabel => ZombieInfectionUtility.GetInfectionCompletion(this).ToStringPercent();

        public override string TipStringExtra
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(base.TipStringExtra);
                if (stringBuilder.Length > 0 && stringBuilder[stringBuilder.Length - 1] != '\n')
                {
                    stringBuilder.AppendLine();
                }

                stringBuilder.Append("Turn progress: ");
                stringBuilder.Append(ZombieInfectionUtility.GetInfectionCompletion(this).ToStringPercent());
                return stringBuilder.ToString();
            }
        }
    }
}
