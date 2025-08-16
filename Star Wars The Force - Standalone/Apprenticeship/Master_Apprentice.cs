using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;


namespace TheForce_Standalone.Apprenticeship
{
    public class CompAbilityEffect_MasterApprentice : CompAbilityEffect
    {
        private Hediff_Master masterHediff;

        public override void Initialize(AbilityCompProperties props)
        {
            base.Initialize(props);
            if (masterHediff == null)
            {
                masterHediff = parent.pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master ?? CreateMasterHediff();
                masterHediff.ChangeApprenticeCapacitySetting(Force_ModSettings.apprenticeCapacity);
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Pawn == null || parent.pawn == null || !parent.pawn.HasComp<CompClass_ForceUser>())
            {
                if (throwMessages) Messages.Message("Force.InvalidTarget".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            var targetPawn = target.Pawn;
            if (IsInvalidApprentice(targetPawn, throwMessages))
            {
                return false;
            }

            return base.Valid(target, throwMessages);
        }

        private bool IsInvalidApprentice(Pawn targetPawn, bool showMessages)
        {
            var apprenticeHediff = targetPawn.health?.hediffSet?.GetFirstHediffOfDef(ForceDefOf.Force_Apprentice) as Hediff_Apprentice;
            if (apprenticeHediff != null && apprenticeHediff.master != parent.pawn)
            {
                if (showMessages) Messages.Message("Force.TargetIsNotYourApprentice".Translate(), MessageTypeDefOf.RejectInput);
                return true;
            }

            var masterHediff = parent.pawn.health?.hediffSet?.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
            if (masterHediff == null || (masterHediff.apprentices.Count >= masterHediff.apprenticeCapacity && !masterHediff.apprentices.Contains(targetPawn)))
            {
                if (showMessages) Messages.Message("Force.ApprenticeLimitReached".Translate(), MessageTypeDefOf.RejectInput);
                return true;
            }

            if (targetPawn.GetComp<CompClass_ForceUser>().forceLevel >= parent.pawn.GetComp<CompClass_ForceUser>().forceLevel)
            {
                if (showMessages) Messages.Message("Force.TargetforceLevelTooHigh".Translate(targetPawn.Label), MessageTypeDefOf.CautionInput);
                return true;
            }
            if (!targetPawn.HasComp<CompClass_ForceUser>())
            {
                if (showMessages) Messages.Message("Force.NotForceUser".Translate(targetPawn.Label), MessageTypeDefOf.CautionInput);
                return true;
            }
            return false;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            masterHediff.ChangeApprenticeCapacitySetting(Force_ModSettings.apprenticeCapacity);

            if (target.Pawn == null)
            {
                Log.Warning("Invalid cast target; not a Pawn.");
                return;
            }

            if (target.Pawn.health?.hediffSet == null)
            {
                Log.Warning("Target pawn health is invalid.");
                return;
            }

            Hediff_Apprentice apprenticeHediff = target.Pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Apprentice) as Hediff_Apprentice;
            if (apprenticeHediff != null && apprenticeHediff.master == parent.pawn)
            {
                if (parent.pawn.health.hediffSet.HasHediff(ForceDefOf.Force_TeachingCooldown))
                {
                    Messages.Message("Force.MasterIsInCooldown".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }
                ShowTeachAbilityDialog(target.Pawn);
            }
            else
            {
                AssignApprenticeHediff(target.Pawn);
                if (Force_ModSettings.rankUpApprentice) ChangeBackstoryBasedOnAlignment(target.Pawn);
                if (!parent.pawn.relations.DirectRelationExists(ForceDefOf.Force_ApprenticeRelation, target.Pawn))
                {
                    parent.pawn.relations.AddDirectRelation(ForceDefOf.Force_ApprenticeRelation, target.Pawn);
                }
                if (!target.Pawn.relations.DirectRelationExists(ForceDefOf.Force_MasterRelation, parent.pawn))
                {
                    target.Pawn.relations.AddDirectRelation(ForceDefOf.Force_MasterRelation, parent.pawn);
                }
            }
        }

        private void ChangeBackstoryBasedOnAlignment(Pawn targetPawn)
        {
            HashSet<string> sithSpawnCategories = new HashSet<string> { "SithChildhood" };
            HashSet<string> jediSpawnCategories = new HashSet<string> { "JediChildhood" };
            float darksideAlignment = masterHediff.GetDarksideAlignment();
            float lightsideAlignment = masterHediff.GetLightsideAlignment();

            if (darksideAlignment > lightsideAlignment)
            {
                if (targetPawn.story.Childhood.spawnCategories.Any(category => sithSpawnCategories.Contains(category)))
                {
                    Messages.Message("Force.AlreadyHaveCorrectBackstory".Translate(targetPawn.Label, targetPawn.story.Childhood.title), MessageTypeDefOf.RejectInput);
                    return;
                }
                targetPawn.story.Childhood = ForceDefOf.Force_SithApprenticeChosen;
            }
            else if (lightsideAlignment > darksideAlignment)
            {
                if (targetPawn.story.Childhood.spawnCategories.Any(category => jediSpawnCategories.Contains(category)))
                {
                    Messages.Message("Force.AlreadyHaveCorrectBackstory".Translate(targetPawn.Label, targetPawn.story.Childhood.title), MessageTypeDefOf.RejectInput);
                    return;
                }
                targetPawn.story.Childhood = ForceDefOf.Force_JediPadawanChosen;
            }
            else
            {
                bool chooseSith = UnityEngine.Random.value < 0.5f;
                if (chooseSith)
                {
                    if (targetPawn.story.Childhood.spawnCategories.Any(category => sithSpawnCategories.Contains(category)))
                    {
                        Messages.Message("Force.AlreadyHaveCorrectBackstory".Translate(targetPawn.Label, targetPawn.story.Childhood.title), MessageTypeDefOf.RejectInput);
                        return;
                    }
                    targetPawn.story.Childhood = ForceDefOf.Force_SithApprenticeChosen;
                }
                else
                {
                    if (targetPawn.story.Childhood.spawnCategories.Any(category => jediSpawnCategories.Contains(category)))
                    {
                        Messages.Message("Force.AlreadyHaveCorrectBackstory".Translate(targetPawn.Label, targetPawn.story.Childhood.title), MessageTypeDefOf.RejectInput);
                        return;
                    }
                    targetPawn.story.Childhood = ForceDefOf.Force_JediPadawanChosen;
                }
            }
        }

        private void AssignApprenticeHediff(Pawn targetPawn)
        {
            var apprenticeHediff = HediffMaker.MakeHediff(ForceDefOf.Force_Apprentice, targetPawn, targetPawn.health.hediffSet.GetBrain()) as Hediff_Apprentice;
            if (apprenticeHediff != null)
            {
                apprenticeHediff.master = parent.pawn;
                masterHediff.apprentices.Add(targetPawn);
                targetPawn.health.AddHediff(apprenticeHediff);

                targetPawn.Notify_DisabledWorkTypesChanged();
                PawnComponentsUtility.AddAndRemoveDynamicComponents(targetPawn);
            }
        }

        private void ShowTeachAbilityDialog(Pawn apprenticePawn)
        {
            var masterComp = parent.pawn.abilities;
            if (masterComp == null)
            {
                Messages.Message("Force.MasterHasNoAbilities".Translate(parent.pawn.Name.ToStringShort), MessageTypeDefOf.RejectInput);
                return;
            }
            var apprenticeComp = apprenticePawn.abilities;
            if (apprenticeComp == null)
            {
                Messages.Message("Force.ApprenticeCannotLearnAbility".Translate(apprenticePawn.Name.ToStringShort), MessageTypeDefOf.RejectInput);
                return;
            }
            List<Ability> teachableAbilities = masterComp.abilities
            .Where(ability =>
                ability.def.comps != null &&
                ability.def.comps.Any(c => c.compClass == typeof(CompAbilityEffect_ForcePower)) &&
                !apprenticeComp.AllAbilitiesForReading.Any(a => a.def == ability.def)
            )
            .ToList();
            Find.WindowStack.Add(new Dialog_TeachAbility(parent.pawn, apprenticePawn, teachableAbilities));
        }

        private Hediff_Master CreateMasterHediff()
        {
            var hediff = HediffMaker.MakeHediff(ForceDefOf.Force_Master, parent.pawn) as Hediff_Master;
            parent.pawn.health.AddHediff(hediff);
            return hediff;
        }
    }

    public class CompProperties_AbilityEffect_MasterApprentice : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityEffect_MasterApprentice()
        {
            compClass = typeof(CompAbilityEffect_MasterApprentice);
        }
    }


    public class Dialog_TeachAbility : Window
    {
        private Pawn masterPawn;
        private Pawn apprenticePawn;
        private List<Ability> teachableAbilities;

        private Vector2 scrollPosition = Vector2.zero;
        private const float ButtonHeight = 40f;
        private const float RowHeight = 80f;

        public override Vector2 InitialSize => new Vector2(600f, 600f);

        public Dialog_TeachAbility(Pawn master, Pawn apprentice, List<Ability> abilities)
        {
            this.masterPawn = master;
            this.apprenticePawn = apprentice;
            this.teachableAbilities = abilities;

            doCloseButton = true;
            draggable = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "Teach Ability or Cancel Apprenticeship");
            if (Widgets.ButtonText(new Rect(0f, 50f, inRect.width, ButtonHeight), "Remove Apprentice"))
            {
                RemoveApprentice();
            }
            if (Widgets.ButtonText(new Rect(0f, 100f, inRect.width, ButtonHeight), "Graduate Apprentice"))
            {
                GraduateApprentice();
            }
            Rect outRect = new Rect(0f, 150f, inRect.width, inRect.height - 150f);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, teachableAbilities.Count * RowHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float y = 0f;
            foreach (Ability ability in teachableAbilities)
            {
                DrawAbilityRow(new Rect(0f, y, viewRect.width, RowHeight), ability);
                y += RowHeight;
            }
            Widgets.EndScrollView();
        }

        private void DrawAbilityRow(Rect rect, Ability ability)
        {
            Rect iconRect = new Rect(rect.x, rect.y, 60f, 60f);
            if (ability.def.uiIcon != null)
            {
                Widgets.DrawTextureFitted(iconRect, ability.def.uiIcon, 1f);
            }
            Rect textRect = new Rect(rect.x + 70f, rect.y, rect.width - 200f, rect.height);
            Text.Font = GameFont.Small;
            Widgets.Label(textRect, $"{ability.def.label.CapitalizeFirst()}\n{ability.def.description}");
            if (Widgets.ButtonText(new Rect(rect.width - 120f, rect.y + 40f, 100f, 30f), "Teach"))
            {
                TeachAbility(ability);
            }
        }

        private void TeachAbility(Ability ability)
        {
            var apprenticeComp = apprenticePawn.abilities;
            if (apprenticeComp == null)
            {
                Messages.Message("Force.ApprenticeCannotLearnAbility".Translate(apprenticePawn.Name.ToStringShort), MessageTypeDefOf.RejectInput);
                return;
            }

            apprenticeComp.GainAbility(ability.def);
            ApplyCooldown(masterPawn);
            Messages.Message("Force.TaughtNewAbility".Translate(apprenticePawn.Name.ToStringShort, ability.def.label), MessageTypeDefOf.PositiveEvent);
            Close();
        }

        private void ApplyCooldown(Pawn pawn)
        {
            Hediff cooldownHediff = HediffMaker.MakeHediff(ForceDefOf.Force_TeachingCooldown, pawn);
            pawn.health.AddHediff(cooldownHediff);
        }

        private void RemoveApprentice()
        {
            var masterHediff = masterPawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
            if (masterHediff != null)
            {
                RemoveApprenticeReference();
                Messages.Message("Force.ApprenticeshipRemoved".Translate(apprenticePawn.Name.ToStringShort), MessageTypeDefOf.NeutralEvent);
            }
            Close();
        }

        public void GraduateApprentice()
        {
            var apprenticeStory = apprenticePawn.story;
            if (Force_ModSettings.rankUpApprentice)
            {
                if (apprenticeStory != null)
                {
                    var masterHediff = masterPawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
                    if (masterHediff != null)
                    {
                        masterHediff.apprentices.Remove(apprenticePawn);
                        RemoveApprenticeReference();
                        if (Force_ModSettings.rankUpMaster)
                        {
                            masterHediff.graduatedApprenticesCount++;
                            masterHediff.CheckAndPromoteMasterBackstory();
                        }
                    }
                    Find.WindowStack.Add(new Dialog_SelectBackstory(apprenticePawn));
                }
            }
            Close();
        }

        public void RemoveApprenticeReference()
        {
            var masterHediff = masterPawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
            masterHediff.apprentices.Remove(apprenticePawn);
            apprenticePawn.health.RemoveHediff(apprenticePawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Apprentice));
            apprenticePawn.relations.RemoveDirectRelation(ForceDefOf.Force_MasterRelation, masterPawn);
            masterPawn.relations.RemoveDirectRelation(ForceDefOf.Force_ApprenticeRelation, apprenticePawn);
        }
    }

    public class Dialog_SelectBackstory : Window
    {
        private Pawn targetPawn;
        private List<BackstoryDef> availableBackstories;

        public Dialog_SelectBackstory(Pawn targetPawn)
        {
            this.targetPawn = targetPawn ?? throw new ArgumentNullException(nameof(targetPawn));
            BackstoryModExtension modExtension = targetPawn.story.Childhood.GetModExtension<BackstoryModExtension>();
            if (modExtension != null)
            {
                availableBackstories = modExtension.availableBackstories ?? new List<BackstoryDef>();
            }

            this.closeOnClickedOutside = false;
            this.doCloseX = true;
            doCloseButton = true;
            draggable = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
        }

        public override Vector2 InitialSize => new Vector2(300f, 400f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 40f), "Select an Adult Backstory");
            Text.Font = GameFont.Small;
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            if (availableBackstories != null && availableBackstories.Any())
            {
                foreach (var backstory in availableBackstories)
                {
                    if (listing.ButtonText(backstory.title))
                    {
                        targetPawn.story.Adulthood = backstory;
                        Messages.Message("Force.ApprenticeshipGraduated".Translate(targetPawn.Name.ToStringShort, targetPawn.story.Adulthood.title), MessageTypeDefOf.PositiveEvent);
                        Close();
                    }
                }
            }
            else
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 40f, inRect.width, 30f), "No available backstories to select.");
            }

            listing.End();
        }
    }

    public class BackstoryModExtension : DefModExtension
    {
        public List<BackstoryDef> availableBackstories;
    }
}