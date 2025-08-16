using RimWorld;
using System.Text;
using Verse;

namespace TheForce_Standalone
{
    public class StatWorker_ForcePowerMax : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            return base.GetValueUnfinalized(req, applyPostProcess);
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            return base.GetStatDrawEntryLabel(stat, value, numberSense, optionalReq, finalized);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("StatsReport_BaseValue".Translate() + ": " + stat.ValueToString(stat.defaultBaseValue, numberSense));

            if (req.HasThing && req.Thing is Pawn pawn)
            {
                var forceUser = pawn.GetComp<CompClass_ForceUser>();
                if (forceUser != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("Force.Stats.ForcePowerCalculation".Translate());

                    float baseFP = 20f + (forceUser.forceLevel * 5f);
                    sb.AppendLine("Force.Stats.BaseFP".Translate(forceUser.forceLevel, baseFP));

                    float sensitivity = stat.defaultBaseValue;
                    sb.AppendLine("Force.Stats.BaseSensitivity".Translate(sensitivity.ToStringPercent()));

                    GetOffsetsAndFactorsExplanation(req, sb, sensitivity);

                    float maxFP = baseFP * GetValue(req, true);
                    sb.AppendLine("Force.Stats.FinalMaxFP".Translate(
                        baseFP,
                        GetValue(req, true).ToString("F1"),
                        maxFP.ToString("F1")
                    ));
                }
            }
            else
            {
                // Default explanation for non-pawns
                GetOffsetsAndFactorsExplanation(req, sb, stat.defaultBaseValue);
            }

            return sb.ToString();
        }
    }
}