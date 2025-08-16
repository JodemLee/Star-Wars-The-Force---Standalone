using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    internal class CompAbilityEffect_ForceChoke : CompAbilityEffect
    {
        public float GetPowerForPawn()
        {
            return Mathf.FloorToInt((parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity) - 1) * 4);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!(target.Thing is Pawn targetPawn) || targetPawn.Dead || targetPawn.Destroyed)
                return;

            var neckParts = targetPawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                .Where(part => part.def.tags.Contains(BodyPartTagDefOf.BreathingPathway)).ToList();

            if (neckParts == null || neckParts.Count == 0)
                return;

            float damageAmount = GetPowerForPawn() * parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity);

            foreach (var neckPart in neckParts)
            {
                DamageInfo damageInfo = new DamageInfo(DamageDefOf.Blunt, damageAmount, 0, -1, null, neckPart);
                targetPawn.TakeDamage(damageInfo);

                if (!targetPawn.Dead && !targetPawn.Destroyed)
                {
                    var flyer = (TelekinesisPawnFlyer)PawnFlyer.MakeFlyer(
                        ForceDefOf.Force_ChokedPawn,
                        targetPawn,
                        targetPawn.Position,
                        null,
                        null
                    );
                    flyer.ability = parent;
                    GenSpawn.Spawn(flyer, targetPawn.Position, parent.pawn.Map);
                }
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return base.CanApplyOn(target, dest) && Valid(target);
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.Valid(target, showMessages))
                return false;

            if (target.Thing == parent.pawn)
            {
                if (showMessages)
                    Messages.Message("CannotTargetSelf".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }
    }
}