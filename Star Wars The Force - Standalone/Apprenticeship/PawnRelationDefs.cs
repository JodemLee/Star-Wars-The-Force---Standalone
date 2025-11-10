using RimWorld;
using System;
using Verse;

namespace TheForce_Standalone.Apprenticeship
{
    public class PawnRelationWorker_Apprentice : PawnRelationWorker
    {
        public override bool InRelation(Pawn me, Pawn other)
        {
            var masterComp = me?.GetComp<CompClass_ForceUser>();
            return masterComp?.Apprenticeship?.apprentices?.Contains(other) == true;
        }

        public override void CreateRelation(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            try
            {
                // Skip if either pawn is invalid or not a force user
                if (generated?.GetComp<CompClass_ForceUser>() == null || other?.GetComp<CompClass_ForceUser>() == null)
                    return;

                var masterComp = generated.GetComp<CompClass_ForceUser>();
                var apprenticeComp = other.GetComp<CompClass_ForceUser>();

                // Skip if at capacity
                if (masterComp.Apprenticeship.apprentices.Count >= masterComp.Apprenticeship.apprenticeCapacity)
                    return;

                // Add apprentice if not already present
                if (!masterComp.Apprenticeship.apprentices.Contains(other))
                {
                    masterComp.Apprenticeship.apprentices.Add(other);
                    apprenticeComp.Apprenticeship.master = generated;

                    // Add relations if missing
                    if (!generated.relations.DirectRelationExists(ForceDefOf.Force_MasterRelation, other))
                        generated.relations.AddDirectRelation(ForceDefOf.Force_MasterRelation, other);

                    if (!other.relations.DirectRelationExists(ForceDefOf.Force_ApprenticeRelation, generated))
                        other.relations.AddDirectRelation(ForceDefOf.Force_ApprenticeRelation, generated);

                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to create apprentice relationship between {generated?.LabelShort} and {other?.LabelShort}: {ex.Message}");
            }
        }

        public override float GenerationChance(Pawn generated, Pawn other, PawnGenerationRequest request)
        {
            // Only allow if both are force users
            if (generated?.GetComp<CompClass_ForceUser>() == null || other?.GetComp<CompClass_ForceUser>() == null)
                return 0f;

            var masterComp = generated.GetComp<CompClass_ForceUser>();

            // Check capacity
            if (masterComp.Apprenticeship.apprentices.Count >= masterComp.Apprenticeship.apprenticeCapacity)
                return 0f;

            // Master should be higher level than apprentice
            if (masterComp.forceLevel <= other.GetComp<CompClass_ForceUser>().forceLevel)
                return 0f;

            // Base chance with level difference bonus
            float baseChance = 0.1f;
            float levelBonus = (masterComp.forceLevel - other.GetComp<CompClass_ForceUser>().forceLevel) * 0.05f;

            return baseChance + levelBonus;
        }
    }

    public class PawnRelationWorker_Master : PawnRelationWorker
    {
        public override bool InRelation(Pawn me, Pawn other)
        {
            var apprenticeComp = me?.GetComp<CompClass_ForceUser>();
            return apprenticeComp?.Apprenticeship?.master == other;
        }

        public override void CreateRelation(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            try
            {
                // Skip if either pawn is invalid or not a force user
                if (generated?.GetComp<CompClass_ForceUser>() == null || other?.GetComp<CompClass_ForceUser>() == null)
                    return;

                var apprenticeComp = generated.GetComp<CompClass_ForceUser>();
                var masterComp = other.GetComp<CompClass_ForceUser>();

                // Skip if apprentice already has a master
                if (apprenticeComp.Apprenticeship.master != null)
                    return;

                // Skip if master is at capacity
                if (masterComp.Apprenticeship.apprentices.Count >= masterComp.Apprenticeship.apprenticeCapacity)
                    return;

                // Set up the relationship
                apprenticeComp.Apprenticeship.master = other;
                masterComp.Apprenticeship.apprentices.Add(generated);

                // Add reciprocal relations
                if (!generated.relations.DirectRelationExists(ForceDefOf.Force_ApprenticeRelation, other))
                    generated.relations.AddDirectRelation(ForceDefOf.Force_ApprenticeRelation, other);

                if (!other.relations.DirectRelationExists(ForceDefOf.Force_MasterRelation, generated))
                    other.relations.AddDirectRelation(ForceDefOf.Force_MasterRelation, generated);

            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to create master relationship between {generated?.LabelShort} and {other?.LabelShort}: {ex.Message}");
            }
        }

        public override float GenerationChance(Pawn generated, Pawn other, PawnGenerationRequest request)
        {
            // Only allow if both are force users
            if (generated?.GetComp<CompClass_ForceUser>() == null || other?.GetComp<CompClass_ForceUser>() == null)
                return 0f;

            var apprenticeComp = generated.GetComp<CompClass_ForceUser>();
            var masterComp = other.GetComp<CompClass_ForceUser>();

            // Check if apprentice already has a master
            if (apprenticeComp.Apprenticeship.master != null)
                return 0f;

            // Check master capacity
            if (masterComp.Apprenticeship.apprentices.Count >= masterComp.Apprenticeship.apprenticeCapacity)
                return 0f;

            // Master should be higher level than apprentice
            if (masterComp.forceLevel <= apprenticeComp.forceLevel)
                return 0f;

            // Base chance with level difference bonus
            float baseChance = 0.1f;
            float levelBonus = (masterComp.forceLevel - apprenticeComp.forceLevel) * 0.05f;

            return baseChance + levelBonus;
        }
    }
}