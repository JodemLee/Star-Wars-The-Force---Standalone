using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TheForce_Standalone
{

    public class StatWorker_ForceLevel : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            // Get the base value first
            float value = base.GetValueUnfinalized(req, applyPostProcess);

            // Try to get force level from CompClass_ForceUser
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                var forceComp = pawn.GetComp<CompClass_ForceUser>();
                if (forceComp != null && forceComp.IsValidForceUser)
                {
                    // Use the force level from the component
                    return forceComp.forceLevel;
                }
            }

            return value;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();

            // Add base explanation
            stringBuilder.AppendLine("StatsReport_BaseValue".Translate() + ": " + stat.ValueToString(stat.defaultBaseValue, numberSense));

            // Add force level specific explanation
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                var forceComp = pawn.GetComp<CompClass_ForceUser>();
                if (forceComp != null && forceComp.IsValidForceUser)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("Force Level: " + forceComp.forceLevel.ToString());

                    // Add midichlorian count if available
                    if (forceComp.MidichlorianCount >= 0)
                    {
                        stringBuilder.AppendLine("Midichlorians: " + forceComp.MidichlorianCount.ToStringPercent());
                    }

                    // Add alignment info if available
                    if (forceComp.Alignment != null)
                    {
                        stringBuilder.AppendLine("Alignment: " + forceComp.Alignment.AlignmentBalance.ToString());
                    }
                }
                else
                {
                    stringBuilder.AppendLine("Not a Force User");
                }
            }

            return stringBuilder.ToString();
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            // Only show for pawns that are valid force users
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                var forceComp = pawn.GetComp<CompClass_ForceUser>();
                return forceComp != null && forceComp.IsValidForceUser;
            }
            return false;
        }
    }
}

