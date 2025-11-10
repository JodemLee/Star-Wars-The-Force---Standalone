using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    internal class CompAbilityEffect_ForceShockWave : CompAbilityEffect
    {
        private new CompProperties_AbilityForceShockWave Props => (CompProperties_AbilityForceShockWave)props;
        private Force_ModSettings modSettings = new Force_ModSettings();
        public bool usePsycastStat = false;
        public int offsetMultiplier { get; set; }

        public CompAbilityEffect_ForceShockWave()
        {
            modSettings = new Force_ModSettings();
        }

        public int GetOffsetMultiplier()
        {
            offsetMultiplier = Force_ModSettings.usePsycastStat
                ? (int)(offsetMultiplier * parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity))
                : (int)Force_ModSettings.offSetMultiplier;
            return offsetMultiplier;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            int offsetMultiplier = GetOffsetMultiplier();
            Map map = parent.pawn.Map;
            if (!target.IsValid || map == null)
            {
                Log.Warning($"[ForceRepulsion] Invalid target or null map - TargetValid: {target.IsValid}, MapNull: {map == null}");
                return;
            }

            var things = ThingsInRange();
            foreach (Thing thing in things)
            {
                IntVec3 pushBackPosition = TelekinesisUtility.CalculatePushPosition(
                parent.pawn.Position,
                thing.Position,
                offsetMultiplier,
                map,
                out bool hitWall
                );

               
                if (thing is Pawn pawnTarget && thing != parent.pawn)
                {
                    thing.TakeDamage(new DamageInfo(Props.damageDef ?? DamageDefOf.Blunt, Props.damageAmount, Props.armorPenetration * offsetMultiplier, -1, parent.pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown, thing, true, true, QualityCategory.Normal, true, false));

                    TelekinesisUtility.LaunchPawn(
                        pawnTarget,
                        pushBackPosition,
                        parent,
                        ForceDefOf.Force_ThrownPawnRepulse,
                        parent.pawn.Map,
                        hitWall,
                        hitWall ? pushBackPosition : (IntVec3?)null
                    );
                }
                else if (thing != null)
                {

                }
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            bool baseResult = base.CanApplyOn(target, dest);
            bool validResult = Valid(target);
            return baseResult && validResult;
        }

        private List<Thing> ThingsInRange()
        {
            try
            {
                IEnumerable<Thing> thingsInRange = GenRadial.RadialDistinctThingsAround(
                    parent.pawn.Position,
                    parent.pawn.Map,
                    parent.def.EffectRadius,
                    useCenter: true);
                var result = new List<Thing>(thingsInRange);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"[ForceRepulsion] Error in ThingsInRange: {ex}");
                return new List<Thing>();
            }
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = true)
        {
            bool baseValid = base.Valid(target, showMessages);
            if (!baseValid)
            {
                Log.Message($"[ForceRepulsion] Target invalid by base validation: {target.Cell}");
            }

            return baseValid;
        }

        public float GetPowerForPawn()
        {
            float sensitivity = parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            float power = Mathf.FloorToInt(sensitivity * 2);
            return power;
        }
    }

    public class CompProperties_AbilityForceShockWave : CompProperties_AbilityEffect
    {
        public float damageAmount = 15f;
        public DamageDef damageDef;
        public float armorPenetration = 0f;
        public SoundDef soundDefOnHit;
        public bool affectFriendlyPawns = false;
        public bool affectNeutralPawns = true;
        public bool affectHostilePawns = true;
        public bool affectItems = true;
        public bool affectBuildings = false;

        public CompProperties_AbilityForceShockWave()
        {
            this.compClass = typeof(CompAbilityEffect_ForceShockWave);
        }

        public bool ShouldAffectThing(Thing thing, Pawn caster)
        {
            if (thing == caster) return false;

            if (thing is Pawn pawn)
            {
                if (pawn.Faction == null) return affectNeutralPawns;
                if (pawn.Faction == caster.Faction) return affectFriendlyPawns;
                if (pawn.Faction.HostileTo(caster.Faction)) return affectHostilePawns;
                return affectNeutralPawns;
            }

            if (thing.def.category == ThingCategory.Item) return affectItems;
            if (thing.def.category == ThingCategory.Building) return affectBuildings;

            return false;
        }
    }
}
