using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.HediffComps
{
    [StaticConstructorOnStartup]
    internal class HediffComp_Cancel : HediffComp
    {

        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
        public HediffCompProperties_CancelAbility Props => (HediffCompProperties_CancelAbility)props;

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Pawn.IsColonistPlayerControlled)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Force_CancelBuff".Translate(parent.def.LabelCap),
                    defaultDesc = "Force_CancelBuffDesc".Translate(parent.def.LabelCap),
                    icon = CancelIcon,
                    action = () =>
                    {
                        Pawn.health.RemoveHediff(parent);
                    }
                };
            }
        }
    }

    public class HediffCompProperties_CancelAbility : HediffCompProperties
    {
        public HediffCompProperties_CancelAbility()
        {
            compClass = typeof(HediffComp_Cancel);
        }
    }
}
