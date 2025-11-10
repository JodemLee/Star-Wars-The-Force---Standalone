using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone.Generic;
using TheForce_Standalone.HediffComps;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside
{
    internal class CompAbilityEffect_SummonPhantom : CompAbilityEffect_WithParentDuration
    {
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
                Log.Error($"[Force Phantom] Error in ThingsInRange: {ex}");
                return new List<Thing>();
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            // Get things in range and create a safe copy
            var things = ThingsInRange();

            // Process valid targets first
            var targetsToProcess = new List<Pawn>();

            foreach (Thing thing in things)
            {
                if (thing is Pawn targetPawn && IsValidTarget(targetPawn))
                {
                    targetsToProcess.Add(targetPawn);
                }
            }

            // Now process each target
            foreach (Pawn targetPawn in targetsToProcess)
            {
                CreatePhantomForPawn(targetPawn, target);
            }
        }

        private bool IsValidTarget(Pawn pawn)
        {
            return pawn.ageTracker != null &&
                   pawn.ageTracker.Adult &&
                   pawn.workSettings != null;
        }

        private void CreatePhantomForPawn(Pawn originalPawn, LocalTargetInfo target)
        {
            try
            {
                Pawn copy = PawnCloningUtility.Duplicate(originalPawn);
                if (copy == null || parent.pawn?.Map == null) return;

                // Apply hediff FIRST (before spawning)
                ApplyHediff(copy);

                // Spawn the copy
                GenSpawn.Spawn(copy, target.Cell, parent.pawn.Map);

                // Restore missing parts AFTER spawning
                RestoreMissingParts(copy);

                // Apply other modifications
                CopyApparelSafely(copy, originalPawn);
                SetTimetableAndWork(copy);
                CopyForceUserAttributes(copy);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in CreatePhantomForPawn for pawn {originalPawn?.LabelShort ?? "null"}: {ex}");
            }
        }

        private void ApplyHediff(Pawn copy)
        {
            if (copy.health == null || ForceDefOf.Force_Phantom == null) return;

            var hediff = HediffMaker.MakeHediff(ForceDefOf.Force_Phantom, copy);
            hediff.Severity = GetDurationSeconds(parent.pawn);

            var hediffComp_Link = hediff.TryGetComp<HediffComp_LinkWithEffect>();
            if (hediffComp_Link != null)
            {
                hediffComp_Link.other = parent.pawn;
                hediffComp_Link.drawConnection = false;
            }

            copy.health.AddHediff(hediff);
        }

        private void RestoreMissingParts(Pawn copy)
        {
            if (copy.health?.hediffSet == null) return;

            try
            {
                // Get all missing parts and convert to list FIRST
                var missingParts = copy.health.hediffSet.GetMissingPartsCommonAncestors().ToList();

                // Restore parts from the list
                foreach (var missingPart in missingParts)
                {
                    try
                    {
                        if (missingPart?.Part != null)
                        {
                            copy.health.RestorePart(missingPart.Part);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error restoring part {missingPart?.Part?.def?.defName ?? "unknown"}: {ex}");
                    }
                }

                // Heal all injuries after restoring parts
                HealAllInjuries(copy);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in RestoreMissingParts: {ex}");
            }
        }

        private void HealAllInjuries(Pawn copy)
        {
            if (copy.health?.hediffSet == null) return;

            try
            {
                // Get all injuries and convert to list FIRST
                var injuries = copy.health.hediffSet.hediffs
                    .Where(h => h is Hediff_Injury && h.Visible && h.def.everCurableByItem)
                    .ToList();

                // Heal injuries from the list
                foreach (var injury in injuries)
                {
                    try
                    {
                        copy.health.RemoveHediff(injury);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error healing injury: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in HealAllInjuries: {ex}");
            }
        }

        private void CopyApparelSafely(Pawn copy, Pawn originalPawn)
        {
            if (originalPawn.apparel == null || copy.apparel == null) return;

            try
            {
                // Remove any existing apparel from the copy first
                var copyApparelList = copy.apparel.WornApparel.ToList();
                foreach (var apparel in copyApparelList)
                {
                    copy.apparel.Remove(apparel);
                }

                // Create a list of apparel to copy from original
                var apparelToCopy = originalPawn.apparel.WornApparel.ToList();
                var newApparelList = new List<Apparel>();

                // First create all the new apparel items
                foreach (Apparel originalApparel in apparelToCopy)
                {
                    try
                    {
                        if (originalApparel != null && originalApparel.def != null)
                        {
                            Apparel newApparel = (Apparel)ThingMaker.MakeThing(originalApparel.def, originalApparel.Stuff);
                            if (newApparel != null)
                            {
                                newApparel.HitPoints = Mathf.Clamp(originalApparel.HitPoints, 1, newApparel.MaxHitPoints);

                                Color darkColor = new Color(
                                    Mathf.Clamp01(originalApparel.DrawColor.r * 0.5f),
                                    Mathf.Clamp01(originalApparel.DrawColor.g * 0.2f),
                                    Mathf.Clamp01(originalApparel.DrawColor.b * 0.3f),
                                    Mathf.Clamp01(originalApparel.DrawColor.a * 0.8f)
                                );

                                newApparel.SetColor(darkColor);
                                newApparelList.Add(newApparel);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error creating apparel copy for {originalApparel?.Label ?? "null"}: {ex}");
                    }
                }

                // Then wear all the new apparel
                foreach (var newApparel in newApparelList)
                {
                    try
                    {
                        if (newApparel != null && copy.apparel.CanWearWithoutDroppingAnything(newApparel.def))
                        {
                            copy.apparel.Wear(newApparel);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error wearing apparel {newApparel?.Label ?? "null"}: {ex}");
                    }
                }

                // Lock all apparel
                copy.apparel.LockAll();
            }
            catch (Exception ex)
            {
                Log.Error($"Error in CopyApparelSafely: {ex}");
            }
        }

        private void SetTimetableAndWork(Pawn copy)
        {
            // Set timetable to work
            if (copy.timetable != null)
            {
                for (int h = 0; h < 24; h++)
                {
                    copy.timetable.SetAssignment(h, TimeAssignmentDefOf.Work);
                }
            }

            // Copy work settings from caster
            if (copy.workSettings != null && parent.pawn.workSettings != null)
            {
                List<WorkTypeDef> workTypeDefs = new List<WorkTypeDef>
                {
                    WorkTypeDefOf.Childcare,
                    WorkTypeDefOf.Handling,
                    WorkTypeDefOf.Doctor,
                    WorkTypeDefOf.Construction,
                    WorkTypeDefOf.Growing,
                    WorkTypeDefOf.Mining,
                    WorkTypeDefOf.Cleaning,
                    WorkTypeDefOf.Crafting,
                    WorkTypeDefOf.DarkStudy,
                    WorkTypeDefOf.Firefighter,
                    WorkTypeDefOf.Hauling,
                    WorkTypeDefOf.Hunting,
                    WorkTypeDefOf.PlantCutting,
                    WorkTypeDefOf.Research,
                    WorkTypeDefOf.Smithing,
                    WorkTypeDefOf.Warden
                };

                foreach (WorkTypeDef work in workTypeDefs)
                {
                    if (work != null && copy.workSettings.WorkIsActive(work))
                    {
                        int casterPriority = parent.pawn.workSettings.GetPriority(work);
                        if (casterPriority >= 0)
                        {
                            copy.workSettings.SetPriority(work, casterPriority);
                        }
                    }
                }
            }
        }

        private void CopyForceUserAttributes(Pawn copy)
        {
            CompClass_ForceUser casterForceUser = parent.pawn.GetComp<CompClass_ForceUser>();
            CompClass_ForceUser copyForceUser = copy.GetComp<CompClass_ForceUser>();

            if (copyForceUser != null && casterForceUser != null)
            {
                try
                {
                    copyForceUser.Alignment.LightSideAttunement = casterForceUser.Alignment.LightSideAttunement;
                    copyForceUser.Alignment.DarkSideAttunement = casterForceUser.Alignment.DarkSideAttunement;
                    copyForceUser.forceLevel = casterForceUser.forceLevel;
                    copyForceUser.unlockedAbiliities = new HashSet<string>(casterForceUser.unlockedAbiliities);

                    copyForceUser.Abilities.abilityPresets = new Dictionary<string, AbilityPreset>();
                    foreach (var preset in casterForceUser.Abilities.abilityPresets)
                    {
                        copyForceUser.Abilities.abilityPresets[preset.Key] = new AbilityPreset
                        {
                            presetName = preset.Value.presetName,
                            activeAbilities = new HashSet<string>(preset.Value.activeAbilities)
                        };
                    }

                    copyForceUser.Abilities.currentPreset = casterForceUser.Abilities.currentPreset;
                    copyForceUser.RecalculateMaxFP();
                    copyForceUser.RecoverFP(copyForceUser.MaxFP);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error copying Force user attributes: {ex}");
                }
            }
        }

        public override bool Valid(GlobalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            if (!target.HasThing || target.Thing is not Pawn targetPawn)
            {
                if (throwMessages)
                    Messages.Message("AbilityMustTargetPawn".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            if (targetPawn.ageTracker == null || !targetPawn.ageTracker.Adult)
            {
                if (throwMessages)
                    Messages.Message("AbilityMustTargetAdult".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                if (!Props.canTargetBaby && !AbilityUtility.ValidateMustNotBeBaby(pawn, throwMessages, parent))
                {
                    return false;
                }
                if (!Props.canTargetBosses && pawn.kindDef.isBoss)
                {
                    return false;
                }
            }

            return true;
        }

        public override bool GizmoDisabled(out string reason)
        {
            base.GizmoDisabled(out reason);

            if (!parent.pawn.ageTracker.Adult)
            {
                reason = "Force_CannotBeChild".Translate();
                return true;
            }

            if (parent.pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Phantom) != null)
            {
                reason = "Force_CasterCannotBePhantom".Translate();
                return true;
            }
            return false;
        }
    }
}