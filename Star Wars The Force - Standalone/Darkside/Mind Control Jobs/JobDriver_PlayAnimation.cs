using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TheForce_Standalone.Darkside.Mind_Control_Jobs
{
    public class JobDriver_PlayAnimation : JobDriver_Wait
    {
        private AnimationDef animationDef;
        private JobDefExtension_Animation extension;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref animationDef, "animationDef");
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();

            // Get the animation from the job def's mod extension
            extension = job.def.GetModExtension<JobDefExtension_Animation>();
            if (extension != null)
            {
                animationDef = extension.animationDef;

                // Apply forced facing from extension if specified
                if (extension.forcedFacing != Rot4.Invalid && job.overrideFacing == Rot4.Invalid)
                {
                    job.overrideFacing = extension.forcedFacing;
                }
            }

            if (animationDef != null)
            {
                pawn.Drawer.renderer.SetAnimation(animationDef);
            }

            // Add cleanup action for when the job ends
            AddFinishAction(condition =>
            {
                // Stop the animation when the job ends
                if (animationDef != null && pawn.Drawer.renderer.CurAnimation == animationDef)
                {
                    pawn.Drawer.renderer.SetAnimation(null);
                }
            });
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Create the base wait toil
            Toil toil = (job.forceSleep ? Toils_LayDown.LayDown(TargetIndex.A, hasBed: false, lookForOtherJobs: false) : ToilMaker.MakeToil("MakeNewToils"));
            toil.initAction = (Action)Delegate.Combine(toil.initAction, (Action)delegate
            {
                base.Map.pawnDestinationReservationManager.Reserve(pawn, job, pawn.Position);
                pawn.pather?.StopDead();
                CheckForAutoAttack();
            });
            toil.tickIntervalAction = (Action<int>)Delegate.Combine(toil.tickIntervalAction, (Action<int>)delegate (int delta)
            {
                if (job.expiryInterval == -1 && job.def == JobDefOf.Wait_Combat && !pawn.Drafted)
                {
                    Log.Error(pawn?.ToString() + " in eternal WaitCombat without being drafted.");
                    ReadyForNextToil();
                }
                else
                {
                    if (job.forceSleep)
                    {
                        asleep = true;
                    }
                    if (GenTicks.IsTickIntervalDelta(pawn.thingIDNumber, 4, delta))
                    {
                        CheckForAutoAttack();
                    }
                }
            });

            // Add animation handling to the toil
            toil.AddPreInitAction(() =>
            {
                if (animationDef != null && pawn.Drawer.renderer.CurAnimation != animationDef)
                {
                    pawn.Drawer.renderer.SetAnimation(animationDef);
                }
            });

            // Set the appropriate complete mode based on looping
            if (extension != null && !extension.loopAnimation && animationDef != null)
            {
                // For non-looping animations, set duration to match animation
                toil.defaultCompleteMode = ToilCompleteMode.Delay;
                toil.defaultDuration = animationDef.durationTicks;
            }
            else
            {
                // For looping animations, never complete automatically
                toil.defaultCompleteMode = ToilCompleteMode.Never;
            }

            // Handle facing and other base decorations
            if (job.overrideFacing != Rot4.Invalid)
            {
                toil.handlingFacing = true;
                toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
                {
                    pawn.rotationTracker.FaceTarget(pawn.Position + job.overrideFacing.FacingCell);
                });
            }
            else if (pawn.mindState != null && pawn.mindState.duty != null && pawn.mindState.duty.focus != null && job.def != JobDefOf.Wait_Combat)
            {
                LocalTargetInfo focusLocal = pawn.mindState.duty.focus;
                toil.handlingFacing = true;
                toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
                {
                    pawn.rotationTracker.FaceTarget(focusLocal);
                });
            }

            yield return toil;
        }

        public override void DecorateWaitToil(Toil wait)
        {
            base.DecorateWaitToil(wait);
        }

        private void CheckForAutoAttack()
        {
            if (!base.pawn.kindDef.canMeleeAttack || base.pawn.Downed || base.pawn.stances.FullBodyBusy || base.pawn.IsCarryingPawn() || (!base.pawn.IsPlayerControlled && base.pawn.IsPsychologicallyInvisible()) || base.pawn.IsShambler)
            {
                return;
            }
            collideWithPawns = false;
            bool flag = !base.pawn.WorkTagIsDisabled(WorkTags.Violent);
            bool flag2 = base.pawn.RaceProps.ToolUser && base.pawn.Faction == Faction.OfPlayer && !base.pawn.WorkTagIsDisabled(WorkTags.Firefighting);
            if (!(flag || flag2))
            {
                return;
            }
            Fire fire = null;
            for (int i = 0; i < 9; i++)
            {
                IntVec3 c = base.pawn.Position + GenAdj.AdjacentCellsAndInside[i];
                if (!c.InBounds(base.pawn.Map))
                {
                    continue;
                }
                List<Thing> thingList = c.GetThingList(base.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (flag && base.pawn.kindDef.canMeleeAttack && thingList[j] is Pawn pawn && !pawn.ThreatDisabled(base.pawn) && base.pawn.HostileTo(pawn))
                    {
                        CompActivity comp = pawn.GetComp<CompActivity>();
                        if ((comp == null || comp.IsActive) && !base.pawn.ThreatDisabledBecauseNonAggressiveRoamer(pawn) && GenHostility.IsActiveThreatTo(pawn, base.pawn.Faction, ignoreHives: false))
                        {
                            base.pawn.meleeVerbs.TryMeleeAttack(pawn);
                            collideWithPawns = true;
                            return;
                        }
                    }
                    if (flag2 && thingList[j] is Fire fire2 && (fire == null || fire2.fireSize < fire.fireSize || i == 8) && (fire2.parent == null || fire2.parent != base.pawn))
                    {
                        fire = fire2;
                    }
                }
            }
            if (fire != null && (!base.pawn.InMentalState || base.pawn.MentalState.def.allowBeatfire))
            {
                base.pawn.natives.TryBeatFire(fire);
            }
            else
            {
                if (!flag || !job.canUseRangedWeapon || job.def != JobDefOf.Wait_Combat || (base.pawn.drafter != null && !base.pawn.drafter.FireAtWill))
                {
                    return;
                }
                Verb currentEffectiveVerb = base.pawn.CurrentEffectiveVerb;
                if (currentEffectiveVerb != null && !currentEffectiveVerb.verbProps.IsMeleeAttack)
                {
                    TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToAll | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
                    if (currentEffectiveVerb.IsIncendiary_Ranged())
                    {
                        targetScanFlags |= TargetScanFlags.NeedNonBurning;
                    }
                    Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(base.pawn, targetScanFlags);
                    if (thing != null)
                    {
                        base.pawn.TryStartAttack(thing);
                        collideWithPawns = true;
                    }
                }
            }
        }
    }

    public class JobDefExtension_Animation : DefModExtension
    {
        public AnimationDef animationDef;
        public bool loopAnimation = true;
        public Rot4 forcedFacing = Rot4.Invalid;
        public bool faceTarget = false;
    }
}