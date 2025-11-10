using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone.Dialogs;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Apprenticeship
{
    public class CompAbilityEffect_MasterApprentice : CompAbilityEffect
    {
        private bool initialized = false;

        public override void Initialize(AbilityCompProperties props)
        {
            base.Initialize(props);
            initialized = true;
        }

        private CompClass_ForceUser GetMasterComp()
        {
            return parent?.pawn?.GetComp<CompClass_ForceUser>();
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Pawn == null || parent?.pawn == null)
            {
                if (throwMessages) Messages.Message("Force.InvalidTarget".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            var masterComp = GetMasterComp();
            if (masterComp == null || !masterComp.IsValidForceUser)
            {
                if (throwMessages) Messages.Message("Force.NotAForceUser".Translate(parent.pawn.LabelShort), MessageTypeDefOf.RejectInput);
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
            if (targetPawn == null || parent?.pawn == null)
            {
                if (showMessages) Messages.Message("Force.InvalidTarget".Translate(), MessageTypeDefOf.RejectInput);
                return true;
            }

            var masterComp = GetMasterComp();
            var targetComp = targetPawn.GetComp<CompClass_ForceUser>();

            if (masterComp == null)
            {
                if (showMessages) Messages.Message("Force.MasterNotInitialized".Translate(), MessageTypeDefOf.RejectInput);
                return true;
            }

            // Check if target is already someone else's apprentice
            if (targetComp?.Apprenticeship?.master != null && targetComp.Apprenticeship.master != parent.pawn)
            {
                if (showMessages) Messages.Message("Force.TargetIsNotYourApprentice".Translate(), MessageTypeDefOf.RejectInput);
                return true;
            }

            // Check apprentice capacity
            if (masterComp.Apprenticeship.apprentices.Count >= masterComp.Apprenticeship.apprenticeCapacity &&
                !masterComp.Apprenticeship.apprentices.Contains(targetPawn))
            {
                if (showMessages) Messages.Message("Force.ApprenticeLimitReached".Translate(), MessageTypeDefOf.RejectInput);
                return true;
            }

            if (targetComp == null)
            {
                if (showMessages) Messages.Message("Force.NotForceUser".Translate(targetPawn.LabelShort), MessageTypeDefOf.CautionInput);
                return true;
            }

            if (targetComp.forceLevel >= masterComp.forceLevel)
            {
                if (showMessages) Messages.Message("Force.TargetforceLevelTooHigh".Translate(targetPawn.LabelShort), MessageTypeDefOf.CautionInput);
                return true;
            }

            return false;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Pawn == null)
            {
                Log.Warning("Invalid cast target; not a Pawn.");
                return;
            }

            var masterComp = GetMasterComp();
            var targetComp = target.Pawn.GetComp<CompClass_ForceUser>();

            if (masterComp == null || targetComp == null)
            {
                Log.Warning("Master or target comp is null.");
                return;
            }

            // Check if target is already this master's apprentice
            if (targetComp.Apprenticeship.master == parent.pawn)
            {
                if (parent.pawn.health?.hediffSet?.HasHediff(ForceDefOf.Force_TeachingCooldown) == true)
                {
                    Messages.Message("Force.MasterIsInCooldown".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }
                ShowTeachAbilityDialog(target.Pawn);
            }
            else
            {
                masterComp.Apprenticeship.AssignApprentice(target.Pawn);
                if (Force_ModSettings.rankUpApprentice)
                    ChangeBackstoryBasedOnAlignment(target.Pawn);

                // Safe relation handling
                if (parent.pawn.relations != null &&
                    !parent.pawn.relations.DirectRelationExists(ForceDefOf.Force_ApprenticeRelation, target.Pawn))
                {
                    parent.pawn.relations.AddDirectRelation(ForceDefOf.Force_ApprenticeRelation, target.Pawn);
                }

                if (target.Pawn.relations != null &&
                    !target.Pawn.relations.DirectRelationExists(ForceDefOf.Force_MasterRelation, parent.pawn))
                {
                    target.Pawn.relations.AddDirectRelation(ForceDefOf.Force_MasterRelation, parent.pawn);
                }

                Messages.Message("Force.ApprenticeAssigned".Translate(target.Pawn.LabelShort, parent.pawn.LabelShort),
                              MessageTypeDefOf.PositiveEvent);
            }
        }

        private void ChangeBackstoryBasedOnAlignment(Pawn targetPawn)
        {
            if (targetPawn?.story?.Childhood == null) return;

            var masterComp = GetMasterComp();
            if (masterComp == null) return;

            HashSet<string> sithSpawnCategories = new() { "SithChildhood" };
            HashSet<string> jediSpawnCategories = new () { "JediChildhood" };

            float darksideAlignment = masterComp.Apprenticeship.GetDarksideAlignment();
            float lightsideAlignment = masterComp.Apprenticeship.GetLightsideAlignment();

            if (darksideAlignment > lightsideAlignment)
            {
                if (targetPawn.story.Childhood.spawnCategories != null &&
                    targetPawn.story.Childhood.spawnCategories.Any(category => sithSpawnCategories.Contains(category)))
                {
                    Messages.Message("Force.AlreadyHaveCorrectBackstory".Translate(targetPawn.Label, targetPawn.story.Childhood.title), MessageTypeDefOf.RejectInput);
                    return;
                }
                targetPawn.story.Childhood = ForceDefOf.Force_SithApprenticeChosen;
            }
            else if (lightsideAlignment > darksideAlignment)
            {
                if (targetPawn.story.Childhood.spawnCategories != null &&
                    targetPawn.story.Childhood.spawnCategories.Any(category => jediSpawnCategories.Contains(category)))
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
                    if (targetPawn.story.Childhood.spawnCategories != null &&
                        targetPawn.story.Childhood.spawnCategories.Any(category => sithSpawnCategories.Contains(category)))
                    {
                        Messages.Message("Force.AlreadyHaveCorrectBackstory".Translate(targetPawn.Label, targetPawn.story.Childhood.title), MessageTypeDefOf.RejectInput);
                        return;
                    }
                    targetPawn.story.Childhood = ForceDefOf.Force_SithApprenticeChosen;
                }
                else
                {
                    if (targetPawn.story.Childhood.spawnCategories != null &&
                        targetPawn.story.Childhood.spawnCategories.Any(category => jediSpawnCategories.Contains(category)))
                    {
                        Messages.Message("Force.AlreadyHaveCorrectBackstory".Translate(targetPawn.Label, targetPawn.story.Childhood.title), MessageTypeDefOf.RejectInput);
                        return;
                    }
                    targetPawn.story.Childhood = ForceDefOf.Force_JediPadawanChosen;
                }
            }
        }

        

        private void ShowTeachAbilityDialog(Pawn apprenticePawn)
        {
            if (apprenticePawn == null || parent?.pawn == null) return;

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
                    ability?.def?.comps != null &&
                    ability.def.comps.Any(c => c.compClass == typeof(CompAbilityEffect_ForcePower)) &&
                    !apprenticeComp.AllAbilitiesForReading.Any(a => a?.def == ability.def)
                )
                .ToList();

            Find.WindowStack.Add(new Dialog_TeachAbility(parent.pawn, apprenticePawn, teachableAbilities));
        }
    }

    public class CompProperties_AbilityEffect_MasterApprentice : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityEffect_MasterApprentice()
        {
            compClass = typeof(CompAbilityEffect_MasterApprentice);
        }
    }

    public class BackstoryModExtension : DefModExtension
    {
        public List<BackstoryDef> availableBackstories;
    }
}