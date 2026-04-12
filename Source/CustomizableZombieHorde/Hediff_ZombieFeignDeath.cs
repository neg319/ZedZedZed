using System.Text;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class Hediff_ZombieFeignDeath : HediffWithComps
    {
        public override string LabelInBrackets
        {
            get
            {
                return ZombieFeignDeathUtility.GetReanimationProgress(this).ToStringPercent();
            }
        }

        public override string SeverityLabel => ZombieFeignDeathUtility.GetReanimationProgress(this).ToStringPercent();

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

                stringBuilder.Append("Reanimating: ");
                stringBuilder.Append(ZombieFeignDeathUtility.GetReanimationProgress(this).ToStringPercent());
                return stringBuilder.ToString();
            }
        }
    }
}
