using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Generic
{
    internal class CompAbilityEffect_ExplosionWithDirection : CompAbilityEffect
    {
        private readonly List<IntVec3> tmpCells = new List<IntVec3>();
        private Effecter maintainedEffecter;
        private int lastDamageTick = -1;
        private const int DamageInterval = 15;
        private int effecterEndTick = -1;
        private const int EffecterDuration = 60;

        public new CompProperties_ExplosionWithDirection Props => (CompProperties_ExplosionWithDirection)props;
        float coneAngle => Props.lineWidthEnd;
        private Pawn Pawn => parent.pawn;
        private bool isActive = false;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            currentTarget = target.Cell;
            isActive = true; 
            effecterEndTick = Find.TickManager.TicksGame + EffecterDuration;
            ApplyDamageToArea(target);
        }

        private void ApplyDamageToArea(LocalTargetInfo target)
        {
            List<IntVec3> affectedCells = AffectedCells(target);
            StunPawnsInArea(affectedCells);

            SoundDef explosionSound = Props.soundExplode ?? SoundDefOf.Tick_Tiny;
            GenExplosion.DoExplosion(
                center: target.Cell,
                map: Pawn.Map,
                radius: 0f,
                damType: Props.damageDef,
                instigator: Pawn,
                damAmount: Props.damAmount,
                armorPenetration: Props.armorPenetration,
                explosionSound: explosionSound, 
                weapon: null,
                projectile: null,
                intendedTarget: null,
                overrideCells: affectedCells
            );
        }

        private IntVec3 currentTarget;

        public override void CompTick()
        {
            base.CompTick();

            if (!isActive) return;

            bool effecterActive = effecterEndTick == -1 || Find.TickManager.TicksGame < effecterEndTick;

            if (effecterActive && Find.TickManager.TicksGame >= lastDamageTick + DamageInterval)
            {
                ApplyDamageToArea(new LocalTargetInfo(currentTarget));
                lastDamageTick = Find.TickManager.TicksGame;
            }
            else if (!effecterActive)
            {
                maintainedEffecter = null;
                isActive = false;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastDamageTick, "lastDamageTick", -1);
            Scribe_Values.Look(ref currentTarget, "currentTarget");
            Scribe_Values.Look(ref effecterEndTick, "effecterEndTick", -1);
        }

        private void StunPawnsInArea(List<IntVec3> affectedCells)
        {
            if (Pawn?.Map == null) return;

            foreach (IntVec3 cell in affectedCells)
            {
                foreach (Thing thing in cell.GetThingList(Pawn.Map))
                {
                    if (thing is Pawn targetPawn && targetPawn.health?.hediffSet?.hediffs.Any(h => h.def.countsAsAddedPartOrImplant) == true)
                    {
                        targetPawn.stances.stunner.StunFor((int)(Props.stunDuration * 60), Pawn);
                    }
                }
            }
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(AffectedCells(target));
        }

        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            tmpCells.Clear();
            Vector3 startVec = Pawn.Position.ToVector3Shifted().Yto0();
            Vector3 targetVec = target.Cell.ToVector3Shifted().Yto0();

            Vector3 direction = (targetVec - startVec).normalized;

            int numCells = GenRadial.NumCellsInRadius(Props.range);
            for (int i = 0; i < numCells; i++)
            {
                IntVec3 cell = Pawn.Position + GenRadial.RadialPattern[i];
                if (!cell.InBounds(Pawn.Map) || cell == Pawn.Position)
                    continue;

                Vector3 cellVec = cell.ToVector3Shifted().Yto0();
                Vector3 toCell = (cellVec - startVec).normalized;

                float distance = Vector3.Distance(cellVec, startVec);
                if (distance > Props.range)
                    continue;

                float currentConeAngle = coneAngle * (distance / Props.range);
                float angle = Vector3.Angle(direction, toCell);

                if (angle <= currentConeAngle * 0.5f)
                {
                    tmpCells.Add(cell);
                }
            }

            return tmpCells;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return target.Cell.IsValid &&
                   target.Cell.DistanceTo(Pawn.Position) <= Props.range &&
                   GenSight.LineOfSight(Pawn.Position, target.Cell, Pawn.Map);
        }

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            if (Props.effecterDef != null)
            {
                yield return new PreCastAction
                {
                    action = delegate (LocalTargetInfo a, LocalTargetInfo b)
                    {
                        maintainedEffecter = Props.effecterDef.Spawn(parent.pawn.Position, a.Cell, parent.pawn.Map);
                        parent.AddEffecterToMaintain(maintainedEffecter, Pawn.Position, a.Cell, EffecterDuration, Pawn.MapHeld);
                        currentTarget = a.Cell;
                        effecterEndTick = Find.TickManager.TicksGame + EffecterDuration;
                    },
                    ticksAwayFromCast = 17
                };
            }
        }
    }

    public class CompProperties_ExplosionWithDirection : CompProperties_AbilityEffect
    {
        public float range;
        public float lineWidthEnd;
        public int damAmount = -1;
        public DamageDef damageDef;
        public float armorPenetration = -1f;
        public SoundDef soundExplode;
        public float stunDuration = 3f;
        public EffecterDef effecterDef;
        public bool canHitFilledCells;

        public CompProperties_ExplosionWithDirection()
        {
            compClass = typeof(CompAbilityEffect_ExplosionWithDirection);
        }
    }
}