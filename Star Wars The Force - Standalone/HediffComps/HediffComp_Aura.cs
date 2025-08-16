using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.HediffComps
{
    internal class HediffComp_Aura : HediffComp
    {
        public HediffCompProperty_Aura Props => (HediffCompProperty_Aura)this.props;
        private int tickCounter = 0;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            tickCounter++;
            if (tickCounter >= Props.checkIntervalTicks)
            {
                tickCounter = 0;
                ApplyAuraEffect();
            }
        }

        private void ApplyAuraEffect()
        {
            if (this.Pawn == null || this.Pawn.Map == null || this.Pawn.Dead)
                return;

            foreach (Pawn target in GetAffectedPawns())
            {
                Hediff hediff = target.health?.hediffSet?.GetFirstHediffOfDef(Props.hediffToApply);
                if (hediff == null)
                {
                    hediff = target.health?.AddHediff(Props.hediffToApply);
                    hediff.Severity = 0.01f;
                    if (hediff == null) continue;
                }
                hediff.Severity += Props.severityIncrease;
            }
        }

        private IEnumerable<Pawn> GetAffectedPawns()
        {
            var pawns = GenRadial.RadialDistinctThingsAround(this.Pawn.Position,
                                                            this.Pawn.Map,
                                                            Props.radius,
                                                            true)
                                .OfType<Pawn>()
                                .Where(p => p != this.Pawn && !p.Dead && !p.Downed);

            if (Props.enemyPawnOnly)
            {
                pawns = pawns.Where(p => p.Faction != null && p.Faction.HostileTo(this.Pawn.Faction));
            }

            return pawns;
        }

        public override string CompTipStringExtra
        {
            get
            {
                return "Force.AuraEffectDescription".Translate(
                    Props.radius.ToString("F1"),
                    Props.hediffToApply.label,
                    Props.severityIncrease.ToStringPercent(),
                    Props.enemyPawnOnly ? "Force.EnemiesOnly".Translate() : "Force.AllNonAllied".Translate()
                );
            }
        }
    }

    public class HediffCompProperty_Aura : HediffCompProperties
    {
        public HediffDef hediffToApply;
        public float radius = 5f;
        public float severityIncrease = 0.1f;
        public int checkIntervalTicks = 60;
        public bool enemyPawnOnly = true;

        public HediffCompProperty_Aura()
        {
            this.compClass = typeof(HediffComp_Aura);
        }
    }
}