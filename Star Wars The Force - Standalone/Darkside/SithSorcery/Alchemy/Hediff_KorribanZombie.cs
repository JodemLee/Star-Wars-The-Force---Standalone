using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace TheForce_Standalone.Darkside.SithSorcery.Alchemy
{
    internal class Hediff_KorribanZombie : HediffWithComps
    {
        private static readonly FloatRange ResurrectionSecondsRange = new FloatRange(5f, 10f);
        private static readonly FloatRange StunSecondsRange = new FloatRange(1f, 3f);
        private static readonly FloatRange AlertSecondsRange = new FloatRange(0.5f, 3f);
        private static readonly IntRange SelfRaiseHoursRange = new IntRange(3, 4);
        private const float ParticleSpawnMTBSeconds = 1f;
        private static readonly IntRange CheckForTargetTicksInterval = new IntRange(900, 1800);
        private const float ExtinguishFireMTB = 45f;
        private const float BioferriteOnDeathChance = 0.04f;
        private const int BioferriteAmountOnDeath = 10;

        public float headRotation;
        private float resurrectTimer;
        private float selfRaiseTimer;
        private float alertTimer;
        private int nextTargetCheckTick = -99999;
        private Thing alertedTarget;
        private Effecter riseEffecter;
        private Sustainer riseSustainer;
        private float corpseDamagePct = 1f;

        // New fields for Korriban zombie behavior
        private Thing linkedMaster;
        private bool isBerserk;

        public Thing LinkedMaster
        {
            get => linkedMaster;
            set
            {
                linkedMaster = value;
                CheckMasterStatus();
            }
        }

        public bool IsBerserk => isBerserk;
        public bool IsRising => pawn.health.hediffSet.HasHediff(HediffDefOf.Rising);

        public override void PostMake()
        {
            base.PostMake();
            headRotation = Rand.RangeSeeded(-20f, 20f, pawn.thingIDNumber);
            if (!pawn.Dead)
            {
                pawn.timesRaisedAsShambler++;
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            if (!ModLister.CheckAnomaly("KorribanZombie"))
            {
                pawn.health.RemoveHediff(this);
            }
            else
            {
                base.PostAdd(dinfo);
            }
        }

        public override void Notify_Spawned()
        {
            base.Notify_Spawned();
            pawn.Map.mapPawns.RegisterShambler(pawn);
            CheckMasterStatus();
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            // Check if master is destroyed/removed
            if (!isBerserk && (linkedMaster == null || linkedMaster.Destroyed || !linkedMaster.Spawned))
            {
                GoBerserk();
            }

            if (IsRising)
            {
                if ((float)Find.TickManager.TicksGame > resurrectTimer)
                {
                    FinishRising();
                }
                if (!pawn.Spawned)
                {
                    return;
                }
                if ((float)Find.TickManager.TicksGame > resurrectTimer - 15f)
                {
                    riseSustainer?.End();
                }
                else if (IsRising)
                {
                    if (riseSustainer == null || riseSustainer.Ended)
                    {
                        SoundInfo info = SoundInfo.InMap(pawn, MaintenanceType.PerTick);
                        riseSustainer = SoundDefOf.Pawn_Shambler_Rise.TrySpawnSustainer(info);
                    }
                    if (riseEffecter == null)
                    {
                        riseEffecter = EffecterDefOf.ShamblerRaise.Spawn(pawn, pawn.Map);
                    }
                    if (pawn.Drawer.renderer.CurAnimation != AnimationDefOf.ShamblerRise)
                    {
                        pawn.Drawer.renderer.SetAnimation(AnimationDefOf.ShamblerRise);
                    }
                    riseSustainer.Maintain();
                    riseEffecter.EffectTick(pawn, TargetInfo.Invalid);
                }
                return;
            }

            if (Rand.MTBEventOccurs(1f, 60f, 1f))
            {
                FleckMaker.ThrowShamblerParticles(pawn);
            }

            if (pawn.IsBurning() && !pawn.Downed && Rand.MTBEventOccurs(45f, 60f, 1f))
            {
                ((Fire)pawn.GetAttachment(ThingDefOf.Fire))?.Destroy();
                pawn.records.Increment(RecordDefOf.FiresExtinguished);
            }

            if (pawn.Spawned && !pawn.mutant.IsPassive && !pawn.Drafted)
            {
                if (Find.TickManager.TicksGame > nextTargetCheckTick)
                {
                    nextTargetCheckTick = Find.TickManager.TicksGame + CheckForTargetTicksInterval.RandomInRange;
                    Thing thing = FindTarget();
                    if (thing != null)
                    {
                        Notify_DelayedAlert(thing);
                        MutantUtility.ActivateNearbyShamblers(pawn, thing);
                    }
                }
                if (alertedTarget != null && (float)Find.TickManager.TicksGame > alertTimer)
                {
                    pawn.mindState.enemyTarget = alertedTarget;
                    Notify_EngagedTarget();
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    alertedTarget = null;
                    if (DebugViewSettings.drawShamblerAlertMote)
                    {
                        MoteMaker.MakeColonistActionOverlay(pawn, ThingDefOf.Mote_ShamblerAlert);
                    }
                    SoundDefOf.Pawn_Shambler_Alert.PlayOneShot(pawn);
                }
            }

            if (ShouldSelfRaise())
            {
                StartRising();
            }
        }

        private Thing FindTarget()
        {
            if (isBerserk)
            {
                return MutantUtility.FindShamblerTarget(pawn);
            }

            // If has master, only attack master's enemies
            if (linkedMaster != null && linkedMaster.Spawned && !linkedMaster.Destroyed)
            {
                // Attack master's enemies
                if (linkedMaster is Pawn masterPawn && masterPawn.mindState.enemyTarget != null)
                {
                    return masterPawn.mindState.enemyTarget;
                }

                // Attack anyone hostile to master
                foreach (Pawn potentialTarget in pawn.Map.mapPawns.AllPawnsSpawned)
                {
                    if (potentialTarget.HostileTo(linkedMaster) && pawn.CanSee(potentialTarget))
                    {
                        return potentialTarget;
                    }
                }
            }

            return null;
        }

        private void CheckMasterStatus()
        {
            if (!isBerserk && (linkedMaster == null || linkedMaster.Destroyed || !linkedMaster.Spawned))
            {
                GoBerserk();
            }
        }

        private void GoBerserk()
        {
            if (isBerserk) return;

            isBerserk = true;
            pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, forced: true);
            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Berserk!", Color.red);
            SoundDefOf.Pawn_Shambler_Alert.PlayOneShot(pawn);
            pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            pawn.mindState.enemyTarget = null;
            var faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Entities);
            pawn.SetFaction(faction);
            linkedMaster = null;
        }

        private bool ShouldSelfRaise()
        {
            if (pawn.DevelopmentalStage == DevelopmentalStage.Baby)
            {
                return false;
            }
            if (pawn.Downed && pawn.CarriedBy == null)
            {
                return (float)Find.TickManager.TicksGame > selfRaiseTimer;
            }
            return false;
        }

        public void StartRising(int lifespanTicks = -1)
        {
            if (!pawn.Dead && !pawn.Downed)
            {
                Log.Error("Tried to raise non dead/downed pawn as Korriban zombie");
                pawn.mutant.Turn(clearLord: true);
                return;
            }

            MutantUtility.RestoreBodyParts(pawn);
            pawn.Notify_DisabledWorkTypesChanged();

            if (!pawn.Dead || ResurrectionUtility.TryResurrect(pawn, new ResurrectionParams
            {
                noLord = true,
                restoreMissingParts = false,
                removeDiedThoughts = false
            }))
            {
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
                resurrectTimer = Find.TickManager.TicksGame + ResurrectionSecondsRange.RandomInRange.SecondsToTicks();
                pawn.health.AddHediff(HediffDefOf.Rising);
            }
        }

        private void CancelRising()
        {
            riseSustainer?.End();
            resurrectTimer = -99999f;
            if (pawn.health.hediffSet.TryGetHediff(HediffDefOf.Rising, out var hediff))
            {
                pawn.health.RemoveHediff(hediff);
            }
            if (!pawn.Dead)
            {
                pawn.Kill(null, null);
            }
        }

        private void FinishRising(bool stun = true)
        {
            if (pawn.health.hediffSet.TryGetHediff(HediffDefOf.Rising, out var hediff))
            {
                pawn.health.RemoveHediff(hediff);
            }

            if (pawn.ParentHolder is Pawn_CarryTracker pawn_CarryTracker)
            {
                pawn_CarryTracker.TryDropCarriedThing(pawn_CarryTracker.pawn.Position, ThingPlaceMode.Near, out var _);
                pawn_CarryTracker.pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }

            if (!pawn.mutant.HasTurned)
            {
                pawn.mutant.Turn();
            }

            pawn.timesRaisedAsShambler++;
            MutantUtility.RestoreUntilNotDowned(pawn);

            if (pawn.Spawned && stun)
            {
                pawn.Rotation = Rot4.South;
                pawn.stances.stunner.StunFor(StunSecondsRange.RandomInRange.SecondsToTicks(), pawn, addBattleLog: false, showMote: false);
            }

            pawn.Drawer.renderer.SetAnimation(null);
            StartSelfRaiseTimer();
            CheckMasterStatus(); // Check master status after rising
        }

        private void StartSelfRaiseTimer()
        {
            selfRaiseTimer = Find.TickManager.TicksGame + 2500 * SelfRaiseHoursRange.RandomInRange;
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (dinfo.Instigator != null && pawn.HostileTo(dinfo.Instigator))
            {
                if (pawn.Spawned && dinfo.Instigator != null && dinfo.Instigator is IAttackTarget && dinfo.Instigator.Spawned && pawn.HostileTo(dinfo.Instigator) && pawn.CanSee(dinfo.Instigator))
                {
                    pawn.mindState.enemyTarget = dinfo.Instigator;
                    Notify_EngagedTarget();
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                    pawn.GetLord()?.Notify_PawnAcquiredTarget(pawn, dinfo.Instigator);
                }
                MutantUtility.ActivateNearbyShamblers(pawn, dinfo.Instigator);
            }
        }

        internal void Notify_EngagedTarget()
        {
            pawn.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;
        }


        public void Notify_DelayedAlert(Thing target)
        {
            if (!pawn.mutant.IsPassive && !pawn.Drafted && pawn.mindState.enemyTarget == null)
            {
                alertTimer = Find.TickManager.TicksGame + AlertSecondsRange.RandomInRange.SecondsToTicks();
                alertedTarget = target;
                pawn.GetLord()?.Notify_PawnAcquiredTarget(pawn, target);
            }
        }

        public override void Notify_Downed()
        {
            StartSelfRaiseTimer();
        }

        public override void Notify_PawnKilled()
        {
            corpseDamagePct = pawn.health.summaryHealth.SummaryHealthPercent;
            base.Notify_PawnKilled();
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            if (IsRising)
            {
                CancelRising();
            }

            if (pawn.timesRaisedAsShambler == 1 && Rand.Chance(0.04f) && pawn.SpawnedOrAnyParentSpawned)
            {
                Thing thing = ThingMaker.MakeThing(ThingDefOf.Bioferrite);
                thing.stackCount = 10;
                GenPlace.TryPlaceThing(thing, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near, out var lastResultingThing);
                lastResultingThing?.SetForbidden(value: true);
            }

            if (pawn.Corpse != null)
            {
                pawn.Corpse.HitPoints = Mathf.Max(Mathf.RoundToInt((float)pawn.Corpse.MaxHitPoints * corpseDamagePct), 10);
            }

            pawn.health.AddHediff(HediffDefOf.ShamblerCorpse);
            base.Notify_PawnDied(dinfo, culprit);
        }

        public override void PreRemoved()
        {
            pawn.MapHeld?.mapPawns.DeregisterShambler(pawn);
            base.PreRemoved();
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            if (pawn.Dead)
            {
                return;
            }
            if (IsRising)
            {
                CancelRising();
            }
            if (pawn.IsMutant)
            {
                if (pawn.mutant.HasTurned)
                {
                    pawn.mutant.Revert();
                }
                else
                {
                    pawn.mutant = null;
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (DebugSettings.ShowDevGizmos)
            {
                if (pawn.Downed && !IsRising)
                {
                    Command_Action command_Action = new Command_Action();
                    command_Action.defaultLabel = "Self Raise";
                    command_Action.action = delegate
                    {
                        StartRising();
                    };
                    yield return command_Action;
                }

                if (linkedMaster != null)
                {
                    Command_Action command_Action2 = new Command_Action();
                    command_Action2.defaultLabel = "Clear Master";
                    command_Action2.action = delegate
                    {
                        LinkedMaster = null;
                    };
                    yield return command_Action2;
                }
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (ShouldSelfRaise())
            {
                return "ShamblerRegenerating".Translate();
            }
            if (IsRising)
            {
                return "ShamblerRising".Translate();
            }
            if (pawn.CurJobDef == JobDefOf.Wait_Wander || pawn.CurJobDef == JobDefOf.Wait_MaintainPosture)
            {
                return "ShamblerStanding".Translate();
            }
            if (pawn.CurJobDef == JobDefOf.GotoWander || pawn.CurJobDef == JobDefOf.Goto)
            {
                return "ShamblerShuffling".Translate();
            }

            if (linkedMaster != null)
            {
                if (stringBuilder.Length > 0) stringBuilder.Append("\n");
                stringBuilder.Append("Linked to: " + linkedMaster.Label);
            }

            if (isBerserk)
            {
                if (stringBuilder.Length > 0) stringBuilder.Append("\n");
                stringBuilder.Append("BERSERK!");
            }

            return "";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref alertTimer, "alertTimer", 0f);
            Scribe_Values.Look(ref nextTargetCheckTick, "nextTargetCheckTick", 0);
            Scribe_References.Look(ref alertedTarget, "alertedTarget");
            Scribe_Values.Look(ref resurrectTimer, "resurrectTimer", 0f);
            Scribe_Values.Look(ref selfRaiseTimer, "selfRaiseTimer", 0f);
            Scribe_Values.Look(ref headRotation, "headRotation", 0f);

            // New data for Korriban zombie
            Scribe_References.Look(ref linkedMaster, "linkedMaster");
            Scribe_Values.Look(ref isBerserk, "isBerserk", false);
        }
    }
}
