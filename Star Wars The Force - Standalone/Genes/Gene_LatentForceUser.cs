using RimWorld;
using System;
using Verse;

namespace TheForce_Standalone.Genes
{
    public class Gene_LatentForceUser : Gene
    {
        // Constants
        private const int CheckIntervalTicks = 60000; // 1 in-game hour
        private const float InitialSensitivityChance = 0.5f;

        // Cached values
        private DefExtension_LatentGeneDates _geneExtension;
        private int _ticksUntilNextCheck;
        private bool _hasInitialized;

        public DefExtension_LatentGeneDates GeneExtension =>
            _geneExtension ??= def.GetModExtension<DefExtension_LatentGeneDates>();

        // Property accessors with fallback values
        public float MeanTimeToActivateSensitivityYears => GeneExtension?.meanTimeToActivateSensitivityYears ?? 10f;
        public float MeanTimeBetweenLevelUpsYears => GeneExtension?.meanTimeBetweenLevelUpsYears ?? 5f;
        public int MaximumForceLevel => GeneExtension?.maximumForceLevel ?? 5;
        public bool ShouldLevelUpUntilMax => GeneExtension?.shouldLevelUpUntilMax ?? true;

        public override void PostAdd()
        {
            base.PostAdd();

            if (pawn == null) return;

            // Initialize check timer with some randomness
            _ticksUntilNextCheck = Rand.Range(0, CheckIntervalTicks);

            // Handle initial setup for pawns not seen by player
            if (!pawn.relations.everSeenByPlayer && !pawn.IsValidForceUser())
            {
                TryGiveInitialForceSensitivity();
            }

            _hasInitialized = true;
        }

        private void TryGiveInitialForceSensitivity()
        {
            if (pawn == null || pawn.Dead) return;

            if (Rand.Chance(InitialSensitivityChance))
            {
                GiveForceSensitivity(pawn);
            }
        }

        public static void GiveForceSensitivity(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.IsValidForceUser()) return;

            // Add Force trait
            TraitDef forceTrait = DefDatabase<TraitDef>.GetNamedSilentFail("Force_NeutralSensitivity");
            if (forceTrait != null && pawn.story?.traits != null && !pawn.story.traits.HasTrait(forceTrait))
            {
                pawn.story.traits.GainTrait(new Trait(forceTrait));
            }

            // Initialize Force component
            var forceComp = pawn.GetComp<CompClass_ForceUser>();
            if (forceComp != null && !forceComp.isInitialized)
            {
                forceComp.isInitialized = true;
                forceComp.RecalculateMaxFP();
                forceComp.RecoverFP(forceComp.MaxFP);
                forceComp.Abilities.AddAbilityPoint(1);
            }

            // Clear sensitivity cache
            ForceSensitivityUtils.ClearCacheForPawn(pawn);
        }

        public override void Tick()
        {
            base.Tick();

            if (pawn == null || !pawn.Spawned || pawn.Dead || !_hasInitialized)
                return;

            // Countdown to next check
            _ticksUntilNextCheck--;

            if (_ticksUntilNextCheck <= 0)
            {
                _ticksUntilNextCheck = CheckIntervalTicks;
                CheckForceProgression();
            }
        }

        private void CheckForceProgression()
        {
            if (!pawn.IsValidForceUser())
            {
                CheckForSensitivityActivation();
            }
            else
            {
                CheckForLevelUp();
            }
        }

        private void CheckForSensitivityActivation()
        {
            if (pawn.genes == null || pawn.Dead) return;

            // Calculate MTB in ticks with age factor
            float meanTimeToActivateYears = MeanTimeToActivateSensitivityYears;
            float meanTimeToActivateTicks = meanTimeToActivateYears * GenDate.TicksPerYear / pawn.genes.BiologicalAgeTickFactor;

            // Proper MTB probability calculation: chance = interval / MTB
            float chance = CheckIntervalTicks / meanTimeToActivateTicks;

            if (Rand.Value < chance)
            {
                GiveForceSensitivity(pawn);

                // Show message to player
                if (pawn.IsColonist)
                {
                    Messages.Message("Force.SensitivityActivated".Translate(pawn.LabelShort),
                        pawn, MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        private void CheckForLevelUp()
        {
            var forceComp = pawn.GetComp<CompClass_ForceUser>();
            if (forceComp == null || pawn.Dead) return;

            // Check if max level reached
            if (ShouldLevelUpUntilMax && forceComp.forceLevel >= MaximumForceLevel)
                return;

            // Calculate level up chance
            float meanTimeBetweenLevelUpsYears = MeanTimeBetweenLevelUpsYears;
            float meanTimeBetweenLevelUpsTicks = meanTimeBetweenLevelUpsYears * GenDate.TicksPerYear / pawn.genes.BiologicalAgeTickFactor;
            float chance = CheckIntervalTicks / meanTimeBetweenLevelUpsTicks;

            if (Rand.Value < chance)
            {
                int oldLevel = forceComp.forceLevel;
                forceComp.Leveling.LevelUp(1);
                int newLevel = forceComp.forceLevel;

                // Show message to player
                if (pawn.IsColonist)
                {
                    Messages.Message("Force.LatentLevelUP".Translate(pawn.LabelShort, newLevel),
                        pawn, MessageTypeDefOf.PositiveEvent);

                    // Max level notification
                    if (newLevel >= MaximumForceLevel)
                    {
                        Messages.Message("Force.ReachedMaxLevel".Translate(pawn.LabelShort, MaximumForceLevel),
                            pawn, MessageTypeDefOf.NeutralEvent);
                    }
                }
            }
        }

        public override void PostRemove()
        {
            base.PostRemove();
        }

        // Expose data for save compatibility
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _ticksUntilNextCheck, "ticksUntilNextCheck", CheckIntervalTicks);
            Scribe_Values.Look(ref _hasInitialized, "hasInitialized", false);
        }
    }

    public class DefExtension_LatentGeneDates : DefModExtension
    {
        public float meanTimeToActivateSensitivityYears = 10f;
        public int maximumForceLevel = 5;
        public float meanTimeBetweenLevelUpsYears = 5f;
        public bool shouldLevelUpUntilMax = true;
    }
}