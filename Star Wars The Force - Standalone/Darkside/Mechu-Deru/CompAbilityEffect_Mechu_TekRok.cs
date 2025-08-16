using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    internal class CompAbilityEffect_Mechu_TekRok : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Thing targetThing = target.Thing;
            if (targetThing == null || parent.pawn == null)
            {
                return;
            }

            if (targetThing is Pawn targetPawn)
            {
                Find.WindowStack.Add(new Dialog_ChooseImplant(targetPawn, parent.pawn, ReplaceImplant));
            }
            else if (targetThing.def.isTechHediff)
            {
                var hediffComp = parent.pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_MechuLinkImplant)?.TryGetComp<HediffComp_MechuDeru>();
                if (hediffComp != null)
                {
                    var matchingSurgeries = DefDatabase<RecipeDef>.AllDefs
                        .Where(recipe => recipe.ingredients.Any(ingredient => ingredient.IsFixedIngredient && ingredient.FixedIngredient == targetThing.def));

                    foreach (var surgery in matchingSurgeries)
                    {
                        if (surgery.addsHediff != null)
                        {
                            hediffComp.StudyImplant(surgery.addsHediff);
                        }
                    }

                    targetThing.Destroy(DestroyMode.Vanish);
                    Messages.Message("Force.MechuTek_ImplantStudied".Translate(targetThing.LabelCap),
                                  parent.pawn,
                                  MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            base.Valid(target, throwMessages);

            Thing targetThing = target.Thing;
            if (targetThing == null)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.MechuTek_InvalidTargetType".Translate(),
                                  parent.pawn,
                                  MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            if (parent.pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_MechuLinkImplant) == null)
            {
                if (throwMessages)
                {
                    Messages.Message("Force.MechuTek_RequiresImplant".Translate(),
                                  parent.pawn,
                                  MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            if (targetThing is Pawn)
            {
                return true;
            }

            if (targetThing.def.isTechHediff)
            {
                var hediffComp = parent.pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_MechuLinkImplant)?.TryGetComp<HediffComp_MechuDeru>();
                if (hediffComp != null)
                {
                    var matchingSurgeries = DefDatabase<RecipeDef>.AllDefs
                        .Where(recipe => recipe.ingredients.Any(ingredient => ingredient.IsFixedIngredient && ingredient.FixedIngredient == targetThing.def));

                    foreach (var surgery in matchingSurgeries)
                    {
                        if (surgery.addsHediff != null && hediffComp.HasStudied(surgery.addsHediff))
                        {
                            if (throwMessages)
                            {
                                Messages.Message("Force.MechuTek_AlreadyStudied".Translate(targetThing.LabelCap),
                                              parent.pawn,
                                              MessageTypeDefOf.RejectInput);
                            }
                            return false;
                        }
                    }
                }
                return true;
            }

            if (throwMessages)
            {
                Messages.Message("Force.MechuTek_InvalidTargetType".Translate(),
                              parent.pawn,
                              MessageTypeDefOf.RejectInput);
            }
            return false;
        }

        private void ReplaceImplant(Pawn pawn, Hediff_Implant oldImplant, HediffDef newImplantDef)
        {
            pawn.health.RemoveHediff(oldImplant);
            pawn.health.AddHediff(newImplantDef, oldImplant.Part);
            Messages.Message("Force.MechuTek_ImplantReplaced".Translate(oldImplant.LabelCap, newImplantDef.LabelCap, pawn.LabelCap),
                          parent.pawn,
                          MessageTypeDefOf.PositiveEvent);
        }
    }

    public class Dialog_ChooseImplant : Window
    {
        private Pawn targetPawn;
        private Pawn casterPawn;
        private Action<Pawn, Hediff_Implant, HediffDef> onImplantSelected;

        private Vector2 scrollPosition;
        private bool highlight = true;

        public Dialog_ChooseImplant(Pawn targetPawn, Pawn casterPawn, Action<Pawn, Hediff_Implant, HediffDef> onImplantSelected)
        {
            this.targetPawn = targetPawn;
            this.casterPawn = casterPawn;
            this.onImplantSelected = onImplantSelected;
            this.forcePause = true;
            this.closeOnAccept = false;
            this.closeOnCancel = true;
            this.doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(600f, 500f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 30f), "Force.MechuTek_SelectImplant".Translate(targetPawn.LabelCap));
            Text.Font = GameFont.Small;

            Rect leftRect = new Rect(0, 40f, inRect.width * 0.3f, inRect.height - 40f);
            Rect rightRect = new Rect(leftRect.width, 40f, inRect.width - leftRect.width, inRect.height - 40f);

            DrawPawnPortrait(leftRect, targetPawn);
            DrawHediffList(rightRect, targetPawn);
        }

        private void DrawPawnPortrait(Rect rect, Pawn pawn)
        {
            Rect portraitRect = new Rect(rect.center.x - 40f, rect.center.y - 60f, 80f, 120f);
            GUI.DrawTexture(portraitRect, PortraitsCache.Get(pawn, portraitRect.size, Rot4.South));
        }

        private void DrawHediffList(Rect rect, Pawn pawn)
        {
            Rect scrollRect = new Rect(rect.x, rect.y, rect.width, rect.height - 16f);
            var hediffGroups = GetVisibleHediffGroups(pawn);
            float viewRectHeight = hediffGroups.Sum(group => group.Count() * 25f);
            Rect viewRect = new Rect(0, 0, scrollRect.width - 16f, viewRectHeight);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
            float curY = 0;

            foreach (var group in hediffGroups)
            {
                foreach (var hediff in group)
                {
                    Rect rowRect = new Rect(0, curY, viewRect.width, 25f);
                    DrawHediffRow(rowRect, hediff);
                    curY += 25f;
                }
            }

            Widgets.EndScrollView();
        }

        private IEnumerable<IGrouping<BodyPartRecord, Hediff_Implant>> GetVisibleHediffGroups(Pawn pawn)
        {
            var implants = pawn.health.hediffSet.hediffs.OfType<Hediff_Implant>();
            return implants
                .GroupBy(hediff => hediff.Part)
                .OrderByDescending(group => GetListPriority(group.Key));
        }

        private float GetListPriority(BodyPartRecord part)
        {
            if (part == null)
                return 9999999f;
            return (float)((int)part.height * 10000) + part.coverageAbsWithChildren;
        }

        private void DrawHediffRow(Rect rect, Hediff_Implant hediff)
        {
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            float infoButtonSize = 20f;
            float padding = 4f;
            float colWidth = (rect.width - infoButtonSize - padding * 3) / 3;

            if (Widgets.InfoCardButton(new Rect(rect.x + padding, rect.y + (rect.height - infoButtonSize) / 2, infoButtonSize, infoButtonSize), hediff))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(hediff));
            }

            Widgets.Label(new Rect(rect.x + padding + infoButtonSize + padding, rect.y, colWidth, rect.height), hediff.LabelCap);
            Widgets.Label(new Rect(rect.x + padding + infoButtonSize + padding + colWidth, rect.y, colWidth, rect.height), hediff.Part?.LabelCap ?? "Force.General_WholeBody".Translate());

            if (Widgets.ButtonText(new Rect(rect.x + padding + infoButtonSize + padding + colWidth * 2, rect.y, colWidth, rect.height), "Force.General_Select".Translate()))
            {
                Find.WindowStack.Add(new Dialog_ChooseReplacement(targetPawn, hediff, casterPawn, onImplantSelected));
                Close();
            }

            TooltipHandler.TipRegion(rect, () => GetHediffTooltip(hediff), hediff.GetHashCode());
        }

        private string GetHediffTooltip(Hediff_Implant hediff)
        {
            StringBuilder tooltip = new StringBuilder();
            tooltip.AppendLine(hediff.LabelCap);
            tooltip.AppendLine(hediff.def.description);
            if (hediff.Part != null)
            {
                tooltip.AppendLine();
                tooltip.AppendLine("Force.MechuTek_BodyPart".Translate(hediff.Part.LabelCap));
            }
            return tooltip.ToString();
        }
    }

    public class Dialog_ChooseReplacement : Window
    {
        private Pawn targetPawn;
        private Hediff_Implant selectedImplant;
        private Pawn casterPawn;
        private Action<Pawn, Hediff_Implant, HediffDef> onReplacementChosen;
        private Vector2 scrollPosition;

        public Dialog_ChooseReplacement(Pawn targetPawn, Hediff_Implant selectedImplant, Pawn casterPawn, Action<Pawn, Hediff_Implant, HediffDef> onReplacementChosen)
        {
            this.targetPawn = targetPawn;
            this.selectedImplant = selectedImplant;
            this.casterPawn = casterPawn;
            this.onReplacementChosen = onReplacementChosen;
            this.forcePause = true;
            this.closeOnAccept = false;
            this.closeOnCancel = true;
            this.doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(600f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 30f), "Force.MechuTek_SelectReplacement".Translate(selectedImplant.LabelCap));
            Text.Font = GameFont.Small;

            Rect scrollRect = new Rect(0, 40f, inRect.width - 16f, inRect.height - 50f);
            Rect viewRect = new Rect(0, 0, scrollRect.width - 16f, GetReplacementOptions().Count() * 30f);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
            float curY = 0;

            foreach (var implantDef in GetReplacementOptions())
            {
                Rect rowRect = new Rect(0, curY, viewRect.width, 30f);
                float infoButtonSize = 24f;
                float padding = 5f;
                float colWidth = (rowRect.width - infoButtonSize - padding * 2) / 3;

                if (Widgets.InfoCardButton(new Rect(rowRect.x + padding, rowRect.y + (rowRect.height - infoButtonSize) / 2, infoButtonSize, infoButtonSize), implantDef))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(implantDef));
                }

                Widgets.Label(new Rect(rowRect.x + padding + infoButtonSize + padding, rowRect.y, colWidth, rowRect.height), implantDef.LabelCap);
                Widgets.Label(new Rect(rowRect.x + padding + infoButtonSize + padding + colWidth, rowRect.y, colWidth, rowRect.height), selectedImplant.Part?.LabelCap ?? "Force.General_WholeBody".Translate());

                if (Widgets.ButtonText(new Rect(rowRect.x + padding + infoButtonSize + padding + colWidth * 2, rowRect.y, colWidth, rowRect.height), "Force.General_Select".Translate()))
                {
                    onReplacementChosen(targetPawn, selectedImplant, implantDef);
                    Close();
                }

                curY += 30f;
            }

            Widgets.EndScrollView();
        }

        private IEnumerable<HediffDef> GetReplacementOptions()
        {
            var bodyPartDef = selectedImplant.Part.def;
            var applicableHediffs = DefDatabase<RecipeDef>.AllDefs
                .Where(recipe => recipe.appliedOnFixedBodyParts != null &&
                                 recipe.appliedOnFixedBodyParts.Contains(bodyPartDef))
                .Select(recipe => recipe.addsHediff)
                .OfType<HediffDef>()
                .Where(implantDef => PawnHasStudiedImplant(casterPawn, implantDef));

            return applicableHediffs.Distinct();
        }

        private bool PawnHasStudiedImplant(Pawn pawn, HediffDef implantDef)
        {
            var studiedComp = pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_MechuLinkImplant)?.TryGetComp<HediffComp_MechuDeru>();
            return studiedComp != null && studiedComp.HasStudied(implantDef);
        }
    }
}