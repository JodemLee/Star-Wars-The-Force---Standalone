using RimWorld;
using System.Collections.Generic;
using TheForce_Standalone.Generic;
using Verse;

namespace TheForce_Standalone.Darkside
{
    public class CompAbilityEffect_EssenceTransfer : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect_EssenceTransfer Props => (CompProperties_AbilityEffect_EssenceTransfer)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn targetPawn = target.Pawn;
            if (targetPawn == null || targetPawn.Dead || targetPawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) >= Props.requiredConsciousnessCapacity)
            {
                return;
            }

            TransferEssence(targetPawn, parent.pawn);
            var casterForce = parent.pawn.GetComp<CompClass_ForceUser>();
            if (casterForce != null)
            {
                casterForce.RecoverFP(casterForce.MaxFP);
            }
        }

        public static void TransferEssence(Pawn targetPawn, Pawn casterPawn)
        {
            bool isCasterGhost = ForceGhostUtility.IsForceGhost(casterPawn);
            bool isTargetGhost = ForceGhostUtility.IsForceGhost(targetPawn);

            if (isTargetGhost)
            {
                Find.LetterStack.ReceiveLetter(
                    "Force.EssenceTransferFailed".Translate(),
                    "Force.CannotSwapWithGhost".Translate(),
                    LetterDefOf.NegativeEvent,
                    new List<Pawn> { casterPawn });
                return;
            }

            if (isCasterGhost)
            {
                HandleGhost(casterPawn);
            }

            SwapNames(targetPawn, casterPawn);
            SwapBackstories(targetPawn, casterPawn);
            SwapTraits(targetPawn, casterPawn);
            SwapSkills(targetPawn, casterPawn);
            if (ModsConfig.IdeologyActive)
            {
                SwapIdeo(targetPawn, casterPawn);
            }
            SwapFaction(targetPawn, casterPawn);
            SwapForceUserData(targetPawn, casterPawn);

            PawnComponentsUtility.AddAndRemoveDynamicComponents(targetPawn);
            PawnComponentsUtility.AddAndRemoveDynamicComponents(casterPawn);

            Find.LetterStack.ReceiveLetter(
                 "Force.EssenceTransferComplete".Translate(),
                 "Force.EssenceTransferSuccess".Translate(
                     casterPawn.Named("CASTER"),
                     targetPawn.Named("TARGET")
                 ),
                 LetterDefOf.NeutralEvent,
                 new List<Pawn> { casterPawn, targetPawn }
             );
        }

        private static void SwapNames(Pawn targetPawn, Pawn casterPawn)
        {
            string tempName = targetPawn.Name.ToStringFull;
            targetPawn.Name = casterPawn.Name;
            casterPawn.Name = new NameSingle(tempName);
        }

        private static void SwapBackstories(Pawn targetPawn, Pawn casterPawn)
        {
            var tempChildhood = targetPawn.story.Childhood;
            var tempAdulthood = targetPawn.story.Adulthood;
            targetPawn.story.Childhood = casterPawn.story.Childhood;
            targetPawn.story.Adulthood = casterPawn.story.Adulthood;
            casterPawn.story.Childhood = tempChildhood;
            casterPawn.story.Adulthood = tempAdulthood;
        }

        private static void SwapFaction(Pawn targetPawn, Pawn casterPawn)
        {
            if (targetPawn.Faction != casterPawn.Faction && casterPawn.Faction != null)
            {
                targetPawn.SetFaction(casterPawn.Faction, casterPawn);
            }
        }

        private static void SwapTraits(Pawn targetPawn, Pawn casterPawn)
        {
            var targetTraits = new List<Trait>(targetPawn.story.traits.allTraits);
            var casterTraits = new List<Trait>(casterPawn.story.traits.allTraits);

            targetPawn.story.traits.allTraits.Clear();
            casterPawn.story.traits.allTraits.Clear();

            foreach (var trait in targetTraits)
            {
                if (trait.sourceGene is null)
                {
                    casterPawn.story.traits.GainTrait(new Trait(trait.def, trait.Degree));
                }
            }

            foreach (var trait in casterTraits)
            {
                if (trait.sourceGene is null)
                {
                    targetPawn.story.traits.GainTrait(new Trait(trait.def, trait.Degree));
                }
            }
        }

        private static void SwapSkills(Pawn targetPawn, Pawn casterPawn)
        {
            var tempSkills = new List<(SkillDef def, int level, Passion passion, float xpSinceLastLevel, float xpSinceMidnight)>();

            foreach (var skill in casterPawn.skills.skills)
            {
                tempSkills.Add((
                    skill.def,
                    skill.Level,
                    skill.passion,
                    skill.xpSinceLastLevel,
                    skill.xpSinceMidnight));
            }

            foreach (var skill in targetPawn.skills.skills)
            {
                var casterSkill = casterPawn.skills.GetSkill(skill.def);
                casterSkill.Level = skill.Level;
                casterSkill.passion = skill.passion;
                casterSkill.xpSinceLastLevel = skill.xpSinceLastLevel;
                casterSkill.xpSinceMidnight = skill.xpSinceMidnight;
            }

            foreach (var tempSkill in tempSkills)
            {
                var targetSkill = targetPawn.skills.GetSkill(tempSkill.def);
                targetSkill.Level = tempSkill.level;
                targetSkill.passion = tempSkill.passion;
                targetSkill.xpSinceLastLevel = tempSkill.xpSinceLastLevel;
                targetSkill.xpSinceMidnight = tempSkill.xpSinceMidnight;
            }
        }

        private static void SwapIdeo(Pawn targetPawn, Pawn casterPawn)
        {
            if (targetPawn.ideo.Ideo != null && casterPawn.Ideo != targetPawn.ideo.Ideo)
            {
                targetPawn.ideo.SetIdeo(casterPawn.Ideo);
            }
        }

        private static void SwapForceUserData(Pawn targetPawn, Pawn casterPawn)
        {
            var targetForce = targetPawn.GetComp<CompClass_ForceUser>();
            var casterForce = casterPawn.GetComp<CompClass_ForceUser>();

            if (targetForce == null || casterForce == null)
                return;

            var forceTraitDef = DefDatabase<TraitDef>.GetNamed("Force_NeutralSensitivity");
            if (forceTraitDef != null)
            {
                bool casterHasTrait = casterPawn.story.traits.HasTrait(forceTraitDef);
                bool targetHasTrait = targetPawn.story.traits.HasTrait(forceTraitDef);

                if (!targetForce.IsValidForceUser)
                {
                    targetPawn.story.traits.GainTrait(new Trait(forceTraitDef, degree: 1));
                }
            }

            float tempExp = targetForce.Leveling.ForceExperience;
            targetForce.Leveling.AddForceExperience(casterForce.Leveling.ForceExperience - targetForce.Leveling.ForceExperience);
            casterForce.Leveling.AddForceExperience(tempExp - casterForce.Leveling.ForceExperience);

            int tempLevel = targetForce.forceLevel;
            targetForce.forceLevel = casterForce.forceLevel;
            casterForce.forceLevel = tempLevel;

            float tempFP = targetForce.currentFP;
            targetForce.currentFP = casterForce.currentFP;
            casterForce.currentFP = tempFP;

            float tempDark = targetForce.Alignment.DarkSideAttunement;
            float tempLight = targetForce.Alignment.LightSideAttunement;
            targetForce.Alignment.DarkSideAttunement = casterForce.Alignment.DarkSideAttunement;
            targetForce.Alignment.LightSideAttunement = casterForce.Alignment.LightSideAttunement;
            casterForce.Alignment.DarkSideAttunement = tempDark;
            casterForce.Alignment.LightSideAttunement = tempLight;

            var tempUnlocked = new HashSet<string>(targetForce.unlockedAbiliities);
            targetForce.unlockedAbiliities = new HashSet<string>(casterForce.unlockedAbiliities);
            casterForce.unlockedAbiliities = tempUnlocked;

            targetForce.Abilities.UpdatePawnAbilities();
            casterForce.Abilities.UpdatePawnAbilities();
        }

        private static void HandleGhost(Pawn ghostPawn)
        {
            if (ghostPawn == null || ghostPawn.Destroyed)
                return;

            // Get the force user component
            var forceUser = ghostPawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null)
                return;

            if (forceUser.LinkedObject != null)
            {
                forceUser.GhostMechanics.LinkedObject = null; // Clear the dark side link
            }

            var ghostHediff = ghostPawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Ghost);
            if (ghostHediff != null)
            {
                ghostPawn.health.RemoveHediff(ghostHediff);
            }

            var sithGhostHediff = ghostPawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_SithGhost);
            if (sithGhostHediff != null)
            {
                ghostPawn.health.RemoveHediff(sithGhostHediff);
            }

            var zombieHediff = ghostPawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_SithZombie);
            if (zombieHediff != null)
            {
                ghostPawn.health.RemoveHediff(zombieHediff);
            }

            // Clean up the pawn
            ghostPawn.apparel?.DropAll(ghostPawn.Position);
            ghostPawn.Destroy(DestroyMode.Vanish);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null)
            {
                return false;
            }
            if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) >= Props.requiredConsciousnessCapacity)
            {
                if (throwMessages)
                {
                    Messages.Message("Target's consciousness is too high for essence transfer.", pawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }

    public class CompProperties_AbilityEffect_EssenceTransfer : CompProperties_AbilityEffect
    {
        public float requiredConsciousnessCapacity = 1f; // Default value

        public CompProperties_AbilityEffect_EssenceTransfer()
        {
            compClass = typeof(CompAbilityEffect_EssenceTransfer);
        }
    }
}