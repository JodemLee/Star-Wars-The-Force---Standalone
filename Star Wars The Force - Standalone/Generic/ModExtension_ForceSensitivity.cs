using RimWorld;
using System.Collections.Generic;
using Verse;

namespace TheForce_Standalone
{
    public class ModExtension_ForceSensitivity : DefModExtension
    {
    }

    public class ModExtension_ForceUser : DefModExtension
    {
        public IntRange forceLevelRange = new IntRange(1, 1);
        public FloatRange lightSideRange = new FloatRange(0f, 0f);
        public FloatRange darkSideRange = new FloatRange(0f, 0f);
        public float initialFP = 0f;
        public int abilityPointsToGrant = 0;

        public List<AbilityCategoryDef> abilityCategories;

        public bool grantRandomAbilities = false;
        public int minRandomAbilities = 0;
        public int maxRandomAbilities = 1;
        public float abilityChance = 1f;
    }

}
