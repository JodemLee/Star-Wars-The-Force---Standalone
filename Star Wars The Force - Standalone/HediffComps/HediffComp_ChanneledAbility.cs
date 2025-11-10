using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.HediffComps
{
    [StaticConstructorOnStartup]
    internal class HediffComp_ChanneledAbility : HediffComp
    {
        public virtual float FPCostPerTick => 1;
        public virtual float SeverityIncreasePerTick => 0.0001f;
        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
        public HediffCompProperties_ChanneledAbility Props => (HediffCompProperties_ChanneledAbility)props;
        public float CurrentFPCost
        {
            get
            {
                var statMultiplier = Pawn.GetStatValue(Props.fpCostMultiplier);
                if (Props.inverseStat)
                {
                    return Props.fpCostPerTick * (1f - statMultiplier);
                }
                return Props.fpCostPerTick * statMultiplier;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            DrainFP();
            IncreaseSeverity();
        }

        private void DrainFP()
        {
            var compForce = Pawn.GetComp<CompClass_ForceUser>();
            if (compForce == null) return;

            if (Find.TickManager.TicksGame % 10 == 0)
            {
                if (!compForce.TrySpendFP(CurrentFPCost))
                {
                    Pawn.health.RemoveHediff(parent);
                }
            }
        }

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

        private void IncreaseSeverity()
        {
            parent.Severity += Props.severityIncreasePerTick;
        }

        public override string CompTipStringExtra
        {
            get
            {
                string tip = base.CompTipStringExtra;
                if (Props.fpCostPerTick > 0)
                {
                    if (!tip.NullOrEmpty())
                        tip += "\n";
                    tip += "Force.ChanneledAbility.FPCostPerTick".Translate(CurrentFPCost.ToString("F2"));
                }
                return tip;
            }
        }
    }

    public class HediffCompProperties_ChanneledAbility : HediffCompProperties
    {
        public float fpCostPerTick = 1f;
        public StatDef fpCostMultiplier;
        public float severityIncreasePerTick = 0f;
        public bool inverseStat = false;

        public HediffCompProperties_ChanneledAbility()
        {
            compClass = typeof(HediffComp_ChanneledAbility);
        }
    }
}