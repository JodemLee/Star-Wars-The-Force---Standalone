using RimWorld;
using System.Linq;
using Verse;

namespace TheForce_Standalone
{
    public static class ForceSensitivityUtils
    {
        public static bool IsValidForceUser(this Pawn pawn)
        {
            if (pawn == null || pawn.story?.traits == null)
                return false;

            // Check traits
            if (pawn.story.traits.HasTrait(TraitDef.Named("Force_NeutralSensitivity")) ||
                pawn.story.traits.HasTrait(TraitDef.Named("Force_LightAffinity")) ||
                pawn.story.traits.HasTrait(TraitDef.Named("Force_DarkAffinity")))
            {
                return true;
            }

            // Check kind def extension
            if (pawn.kindDef?.HasModExtension<ModExtension_ForceUser>() == true)
                return true;

            // Check hediffs
            if (pawn.health?.hediffSet?.hediffs.Any(h => h.def.GetModExtension<ModExtension_ForceSensitivity>() != null) == true)
                return true;

            // Check genes
            if (pawn.genes != null && pawn.genes.Endogenes.Any(g => g.def.GetModExtension<ModExtension_ForceSensitivity>() != null || pawn.genes.Xenogenes.Any(g => g.def.GetModExtension<ModExtension_ForceSensitivity>() != null)))
                return true;

            return false;
        }

        public static void MarkForRedraw(this CompClass_ForceUser comp)
        {
            if (comp.parent is Pawn pawn)
            {
                PortraitsCache.SetDirty(pawn);
            }
        }
    }
}
