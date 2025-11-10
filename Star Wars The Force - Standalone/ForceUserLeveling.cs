using RimWorld;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class ForceUserLeveling
    {
        private readonly CompClass_ForceUser parent;
        private float forceExperience;
        private float xpForNextLevel;
        public float ForceExperience => forceExperience;
        public int XPRequiredForNextLevel => Mathf.RoundToInt(xpForNextLevel);

        public ForceUserLeveling(CompClass_ForceUser parent)
        {
            this.parent = parent;
        }

        public void Initialize()
        {
            if (parent?.Pawn == null) return;
            var forceUserExt = parent.Pawn.kindDef?.GetModExtension<ModExtension_ForceUser>();
            if (forceUserExt != null)
            {
                parent.forceLevel += forceUserExt.forceLevelRange.RandomInRange;
            }
            CalculateXPForNextLevel();
        }

        public void Reset()
        {
            // Reset experience
            forceExperience = 0f;
            parent.forceLevel = 1;
            CalculateXPForNextLevel();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref forceExperience, "forceExperience", 0);
            Scribe_Values.Look(ref xpForNextLevel, "xpForNextLevel", 100f);
        }

        public void AddForceExperience(float amount)
        {
            if (!parent.IsValidForceUser) return;
            var multiplier = amount * Force_ModSettings.forceXpMultiplier * parent.parent.GetStatValueForPawn(StatDef.Named("Force_XPGain"), parent.Pawn, true);
            forceExperience += multiplier;
            CheckForLevelUp();
        }

        private void CheckForLevelUp()
        {
            while (forceExperience >= xpForNextLevel)
            {
                forceExperience -= xpForNextLevel;
                LevelUp();
                CalculateXPForNextLevel();
            }
        }

        public void LevelUp(int amount = 1)
        {
            parent.forceLevel += amount;
            parent.Abilities.AddAbilityPoint(amount);
            parent.RecalculateMaxFP();

            if (parent.Pawn != null)
            {
                Messages.Message("Force.LevelUP".Translate(parent.Pawn.LabelShort, parent.forceLevel),
                    parent.Pawn, MessageTypeDefOf.PositiveEvent);
                parent.Abilities.UpdatePawnAbilities();
            }
        }

        public void CalculateXPForNextLevel()
        {
            xpForNextLevel = parent.forceLevel * 100f;
        }
    }
}
