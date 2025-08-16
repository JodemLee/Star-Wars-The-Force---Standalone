using RimWorld;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    public class HediffComp_MechuFlare : HediffComp
    {
        public HediffDef hediffToApply;
        public float severityMultiplier = 1f;

        public HediffCompProperties_MechuFlare Props => (HediffCompProperties_MechuFlare)props;

        public override void CompPostMake()
        {
            base.CompPostMake();

            ApplyImplantStrain();
        }
        private void ApplyImplantStrain()
        {
            var pawn = parent.pawn;
            var implantHediffs = pawn.health.hediffSet.hediffs
                .Where(hediff => IsImplantOrProsthetic(hediff))
                .ToList();
            severityMultiplier = Props.severityMultiplier;
            int implantCount = implantHediffs.Count;
            if (implantCount == 0) return;
            float severityFactor = implantCount * severityMultiplier;
            var parentHediff = parent as HediffWithComps;
            if (parentHediff != null)
            {
                parentHediff.Severity += severityFactor;
            }
        }
        private bool IsImplantOrProsthetic(Hediff hediff)
        {
            var implantDef = hediff.def;
            if (implantDef != null && implantDef.countsAsAddedPartOrImplant)
            {
                var implantThingDef = implantDef.spawnThingOnRemoved;
                if (implantThingDef != null && implantThingDef.techLevel > TechLevel.Industrial)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class HediffCompProperties_MechuFlare : HediffCompProperties
    {
        public float severityMultiplier = 1f;

        public HediffCompProperties_MechuFlare()
        {
            this.compClass = typeof(HediffComp_MechuFlare);
        }
    }
}
