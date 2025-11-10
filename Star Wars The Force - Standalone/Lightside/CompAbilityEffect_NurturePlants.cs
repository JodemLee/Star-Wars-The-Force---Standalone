using RimWorld;
using System.Collections.Generic;
using System.Numerics;
using TheForce_Standalone.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Lightside
{
    internal class CompAbilityEffect_NurturePlants : CompAbilityEffect
    {
        public new CompProperties_AbilityWithStat Props => (CompProperties_AbilityWithStat)props;

        private float GetScaledRadius()
        {
            var pawn = parent.pawn;
            if (Props.scalingStat != null)
            {
                float statValue = pawn.GetStatValue(Props.scalingStat);
                return 3f * (statValue / 100f);
            }

            // Default radius if no scaling stat is defined
            return 3f;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = parent.pawn;
            if (!Find.Selector.IsSelected(pawn))
            {
                ExecuteNurture(target, 0.25f);
            }
            else
            {
                ShowNurturePowerMenu(target);
            }
        }

        private void ShowNurturePowerMenu(LocalTargetInfo target)
        {
            var menuOptions = Utility_FPCostMenu.CreateStandardPercentages(ExecuteNurture);
            Utility_FPCostMenu.ShowForcePercentageMenu(parent.pawn, target, menuOptions, "Force.Nurture_NotEnoughFP");
        }

        private void ExecuteNurture(LocalTargetInfo target, float fpPercentage)
        {
            var forceUser = parent.pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null) return;

            float fpCost = (fpPercentage >= 0.99f) ? forceUser.currentFP : forceUser.MaxFP * fpPercentage;

            if (!forceUser.TrySpendFP(fpCost)) return;

            float radius = GetScaledRadius();
            bool anyPlantsNurtured = false;
            int plantsNurtured = 0;

            // Get all cells in scaled radius
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Cell, radius, true))
            {
                if (!cell.InBounds(parent.pawn.Map)) continue;

                // Check for plants in this cell
                Plant plant = cell.GetPlant(parent.pawn.Map);
                if (plant != null)
                {
                    // Simple, guaranteed growth formula
                    float baseGrowthAmount = 0.15f * fpPercentage;
                    float growthRateFactor = 10f / plant.def.plant.growDays;
                    float totalGrowth = baseGrowthAmount * growthRateFactor;
                    totalGrowth = Mathf.Max(totalGrowth, 0.05f * fpPercentage);
                    plant.Growth = Mathf.Min(plant.Growth + totalGrowth, 1f);

                    FleckMaker.AttachedOverlay(plant, FleckDefOf.HealingCross, new UnityEngine.Vector3(0, 0, 0), 1f);

                    // Only show text for the main target to avoid spam
                    if (cell == target.Cell)
                    {
                        MoteMaker.ThrowText(plant.Position.ToVector3Shifted(), plant.Map,
                            "Force.NurtureSuccess".Translate(parent.pawn.LabelShort, (totalGrowth * 100f).ToString("F0"), plantsNurtured + 1));
                    }

                    plant.DirtyMapMesh(plant.Map);
                    plantsNurtured++;
                    anyPlantsNurtured = true;

                    // Strong visual for significant growth
                    if (totalGrowth > 0.1f)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            FleckMaker.ThrowLightningGlow(plant.Position.ToVector3Shifted(), plant.Map, 0.5f);
                        }
                    }
                }
            }

            if (!anyPlantsNurtured)
            {
                forceUser.currentFP += fpCost;
                Messages.Message("Force.NurtureInvalidTarget".Translate(), MessageTypeDefOf.RejectInput);
            }
            else if (plantsNurtured > 1)
            {
                // Show summary message for multiple plants
                Messages.Message("Force.NurtureMultipleSuccess".Translate(parent.pawn.LabelShort, plantsNurtured, radius.ToString("F1")), MessageTypeDefOf.PositiveEvent);
            }
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (target.IsValid && target.Cell.InBounds(parent.pawn.Map))
            {
                float radius = GetScaledRadius();
                GenDraw.DrawRadiusRing(target.Cell, radius, Color.green);
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Cell, radius, true))
                {
                    if (cell.InBounds(parent.pawn.Map))
                    {
                        Plant plant = cell.GetPlant(parent.pawn.Map);
                        if (plant != null)
                        {
                            // Draw a green highlight on affected plants
                            GenDraw.DrawCircleOutline(cell.ToVector3Shifted(), 0.5f, SimpleColor.Green);
                        }
                    }
                }
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!target.IsValid || !target.Cell.InBounds(parent.pawn.Map))
                return false;

            float radius = GetScaledRadius();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Cell, radius, true))
            {
                if (cell.InBounds(parent.pawn.Map) && cell.GetPlant(parent.pawn.Map) != null)
                    return true;
            }

            if (throwMessages)
            {
                Messages.Message("Force.NurtureNoPlantsInArea".Translate(radius.ToString("F1")), MessageTypeDefOf.RejectInput);
            }
            return false;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (Props.scalingStat != null)
            {
                float radius = GetScaledRadius();
                float statValue = parent.pawn.GetStatValue(Props.scalingStat);

                return "Force.NurtureRadiusLabel".Translate(radius.ToString("F1"), Props.scalingStat.LabelCap, statValue.ToString("F1"));
            }

            return base.ExtraLabelMouseAttachment(target);
        }
    }
}