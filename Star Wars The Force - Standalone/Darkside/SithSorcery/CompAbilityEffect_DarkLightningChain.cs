using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    internal class CompAbilityEffect_DarkLightningChain : CompAbilityEffect
    {
        public new CompProperties_DarkLightningChain Props => (CompProperties_DarkLightningChain)props;

        private int currentChain;
        private IntVec3 currentCell;
        private int lastCastTick;
        private bool isCasting;
        private HashSet<Pawn> hitPawns;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            // Null check parent and pawn
            if (parent == null || parent.pawn == null || parent.pawn.Map == null)
            {
                Log.Error("[Sith Lightning] Parent, pawn, or map is null");
                return;
            }

            if (!target.IsValid || target.Cell.Fogged(parent.pawn.Map))
                return;

            currentCell = target.Cell;
            currentChain = 0;
            lastCastTick = GenTicks.TicksGame;
            isCasting = true;
            hitPawns = new HashSet<Pawn>();

            ApplyLightningEffect();
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!isCasting) return;

            if (parent == null || parent.pawn == null || parent.pawn.Map == null)
            {
                isCasting = false;
                return;
            }

            if (GenTicks.TicksGame >= lastCastTick + Props.delayTicks)
            {
                ApplyLightningEffect();

                if (currentChain >= GetMaxChains())
                {
                    isCasting = false;
                }
                else
                {
                    FindNextTarget();
                    currentChain++;
                    lastCastTick = GenTicks.TicksGame;
                }
            }
        }

        private void ApplyLightningEffect()
        {

            if (parent?.pawn?.Map == null || ForceDefOf.Force_Lightning == null)
            {
                Log.Error("[Sith Lightning] Critical references are null");
                isCasting = false;
                return;
            }

            Map map = parent.pawn.Map;

            if (!currentCell.InBounds(map))
            {
                isCasting = false;
                return;
            }

            var things = currentCell.GetThingList(map);
            if (things == null) return;

            foreach (Thing thing in things.ListFullCopy())
            {
                if (thing == null || thing.Destroyed) continue;
                if (thing.Faction == parent.pawn.Faction) continue;

                int damage = GetBaseDamage() + (Props.damageIncreasePerChain * currentChain);
                if (Props.damageStat != null)
                {
                    damage = (int)(damage * parent.pawn.GetStatValue(Props.damageStat));
                }

                thing.TakeDamage(new DamageInfo(ForceDefOf.Force_Lightning, damage, -1f,
                    parent.pawn.DrawPos.AngleToFlat(thing.DrawPos), parent.pawn));

                if (thing is Pawn pawn && pawn.Faction != parent.pawn.Faction && !pawn.Downed)
                {
                    hitPawns.Add(pawn);
                    pawn.stances?.stunner?.StunFor(Props.baseStunDuration, parent.pawn);
                }
            }

            SoundDef explosionSound = SoundDefOf.Thunder_OnMap ?? SoundDefOf.FlashstormAmbience;
            GenExplosion.DoExplosion(
                currentCell,
                map,
                GetRadiusForPawn(),
                ForceDefOf.Force_Lightning,
                parent.pawn,
                explosionSound: explosionSound,
                doSoundEffects: false);

            map.weatherManager?.eventHandler?.AddEvent(new SithLightningEvent(map, currentCell, UnityEngine.Color.red));
        }

        private int GetBaseDamage()
        {
            return Props?.baseDamage ?? 25;
        }

        private int GetMaxChains()
        {
            if (Props == null) return 3;

            int chains = Props.maxChains;
            if (Props.chainCountStat != null)
            {
                chains = (int)(chains * parent.pawn.GetStatValue(Props.chainCountStat));
            }
            return chains;
        }

        private void FindNextTarget()
        {
            if (parent?.pawn?.Map == null) return;

            Map map = parent.pawn.Map;
            var potentialTargets = GenRadial.RadialDistinctThingsAround(currentCell, map, Props.chainRadius, true)?
                .Where(t => t is Pawn p && p.Faction != parent.pawn.Faction && !p.Downed)?.ToList();

            if (potentialTargets == null || potentialTargets.Count == 0) return;

            var newTargets = potentialTargets.Where(t => !hitPawns.Contains((Pawn)t)).ToList();

            Thing nextTarget = newTargets.Count > 0
                ? newTargets.RandomElement()
                : potentialTargets.RandomElement();

            if (nextTarget != null)
            {
                currentCell = nextTarget.Position;
            }
        }

        private float GetRadiusForPawn()
        {
            return parent?.def?.EffectRadius ?? 1.5f;
        }
    }



    public class CompProperties_DarkLightningChain : CompProperties_AbilityEffect
    {
        public int baseDamage = 25;
        public int damageIncreasePerChain = 15;
        public int baseStunDuration = 60;
        public int maxChains = 3;
        public float chainRadius = 4.5f;
        public int delayTicks = 30;

        public StatDef damageStat;
        public StatDef chainCountStat;

        public CompProperties_DarkLightningChain()
        {
            compClass = typeof(CompAbilityEffect_DarkLightningChain);
        }
    }
}
