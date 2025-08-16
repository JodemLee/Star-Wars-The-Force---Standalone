using RimWorld;
using System.Collections.Generic;
using TheForce_Standalone.Alignment;
using Verse;

namespace TheForce_Standalone
{
    public class ForceAbilityDefExtension : DefModExtension
    {
        public int RequiredLevel = 1;

        // Abilities
        public List<AbilityDef> requiredAbilities;
        public bool requireAllAbilities = true;

        // Traits
        public List<TraitDef> requiredTraits;
        public bool requireAllTraits = true;

        // Hediffs
        public List<HediffDef> requiredHediffs;
        public bool requireAllHediffs = true;

        // Alignment
        public AlignmentValue requiredAlignment;
    }

    public class AlignmentValue
    {
        public AlignmentType alignmentType;
        public float value;
    }

}
