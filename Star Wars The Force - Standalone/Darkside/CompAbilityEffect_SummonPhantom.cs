using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
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


            var things = ThingsInRange();
            foreach (Thing thing in things)
            {

                if (thing is Pawn targetPawn )
                {

                    if (targetPawn.ageTracker == null || !targetPawn.ageTracker.Adult)
                        return;

                    if (targetPawn.workSettings == null)
                        return;

                    try
                    {
                        Pawn copy = PawnCloningUtility.Duplicate(targetPawn);
                        if (copy == null) return;
                        if (parent.pawn?.Map == null) return;

                        GenSpawn.Spawn(copy, target.Cell, parent.pawn.Map);

                        if (copy.health != null && ForceDefOf.Force_Phantom != null)
                        {
                            var hediff = HediffMaker.MakeHediff(ForceDefOf.Force_Phantom, copy);
                            hediff.Severity = (GetDurationSeconds(parent.pawn));
                            if (hediff != null)
                            {
                                HediffComp_LinkWithEffect hediffComp_Link = hediff.TryGetComp<HediffComp_LinkWithEffect>();
                                if (hediffComp_Link != null)
                                {
                                    hediffComp_Link.other = parent.pawn;
                                    hediffComp_Link.drawConnection = target == parent.pawn;
                                }
                            }
                            copy.health.AddHediff(hediff);
                           

                            
                        }

                        if (targetPawn.apparel != null && copy.apparel != null)
                        {
                            foreach (Apparel apparel in targetPawn.apparel.WornApparel)
                            {
                                if (apparel == null) continue;

                                Apparel newApparel = (Apparel)ThingMaker.MakeThing(apparel.def, apparel.Stuff);
                                if (newApparel == null) continue;

                                newApparel.HitPoints = apparel.HitPoints;

                                Color darkColor = new Color(
                                    Mathf.Clamp01(apparel.DrawColor.r * 0.5f),
                                    Mathf.Clamp01(apparel.DrawColor.g * 0.2f),
                                    Mathf.Clamp01(apparel.DrawColor.b * 0.3f),
                                    Mathf.Clamp01(apparel.DrawColor.a * 0.8f)
                                );

                                newApparel.SetColor(darkColor);
                                copy.apparel.Wear(newApparel);
                                copy.apparel.LockAll();
                            }
                        }

                        if (copy.timetable != null)
                        {
                            for (int h = 0; h < 24; h++)
                            {
                                copy.timetable.SetAssignment(h, TimeAssignmentDefOf.Work);
                            }
                        }

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
                                    if (casterPriority > 0)
                                    {
                                        copy.workSettings.SetPriority(work, casterPriority);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error in CompAbilityEffect_ForcePhantom.Apply: {ex}");
                    }
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

            if (parent.pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Phantom) != null)
            {
                reason = "Force_CasterCannotBePhantom".Translate();
                return true;
            }
            return false;
        }

    }
}

