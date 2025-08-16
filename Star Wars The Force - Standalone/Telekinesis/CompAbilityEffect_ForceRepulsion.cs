using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    internal class CompAbilityEffect_ForceRepulsion : CompAbilityEffect
    {
        private Force_ModSettings modSettings = new Force_ModSettings();
        public bool usePsycastStat = false;
        public int offsetMultiplier { get; set; }

        public CompAbilityEffect_ForceRepulsion()
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
                    TelekinesisUtility.LaunchPawn(
                        pawnTarget,
                        pushBackPosition,
                        parent,
                        ForceDefOf.Force_ThrownPawnRepulse,
                        parent.pawn.Map,
                        hitWall,
                        hitWall ? pushBackPosition : (IntVec3?) null
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
}
