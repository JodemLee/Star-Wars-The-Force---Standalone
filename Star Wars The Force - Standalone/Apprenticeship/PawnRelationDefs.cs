using RimWorld;
using System;
using Verse;

namespace TheForce_Standalone.Apprenticeship
{

    public class PawnRelationWorker_Apprentice : PawnRelationWorker
    {
        private HediffDef masterHediffDef = ForceDefOf.Force_Master;

        public override bool InRelation(Pawn me, Pawn other)
        {
            var masterHediff = me.health?.hediffSet?.GetFirstHediffOfDef(masterHediffDef) as Hediff_Master;
            return masterHediff != null && masterHediff.apprentices.Contains(other);
        }

        public override void CreateRelation(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            try
            {
                // Skip if either pawn is invalid
                if (generated?.health?.hediffSet == null || other?.health?.hediffSet == null)
                    return;

                // Get or add master hediff if missing
                var masterHediff = generated.health.hediffSet.GetFirstHediffOfDef(masterHediffDef) as Hediff_Master;
                if (masterHediff == null)
                {
                    masterHediff = HediffMaker.MakeHediff(masterHediffDef, generated) as Hediff_Master;
                    generated.health.AddHediff(masterHediff);
                }

                // Skip if at capacity (instead of throwing error)
                if (masterHediff.apprentices.Count >= masterHediff.apprenticeCapacity)
                    return;

                // Add apprentice if not already present
                if (!masterHediff.apprentices.Contains(other))
                {
                    masterHediff.apprentices.Add(other);

                    // Add apprentice hediff
                    var apprenticeHediff = other.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Apprentice) as Hediff_Apprentice;
                    if (apprenticeHediff == null)
                    {
                        apprenticeHediff = HediffMaker.MakeHediff(ForceDefOf.Force_Apprentice, other) as Hediff_Apprentice;
                        apprenticeHediff.master = generated;
                        other.health.AddHediff(apprenticeHediff);
                    }

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
    }

    public class PawnRelationWorker_Master : PawnRelationWorker
    {
        private HediffDef apprenticeHediffDef = ForceDefOf.Force_Apprentice;

        public override bool InRelation(Pawn me, Pawn other)
        {
            // Check if 'me' is the apprentice and 'other' is the master
            var apprenticeHediff = me.health.hediffSet.GetFirstHediffOfDef(apprenticeHediffDef) as Hediff_Apprentice;
            return apprenticeHediff != null && apprenticeHediff.master == other;
        }

        public override void CreateRelation(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            // Make sure the apprentice has the Hediff_Apprentice hediff
            var apprenticeHediff = generated.health.hediffSet.GetFirstHediffOfDef(apprenticeHediffDef) as Hediff_Apprentice;
            if (apprenticeHediff == null)
            {
                apprenticeHediff = HediffMaker.MakeHediff(ForceDefOf.Force_Apprentice, generated) as Hediff_Apprentice;
                apprenticeHediff.master = other;
                generated.health.AddHediff(apprenticeHediff);

                // Ensure the master has Hediff_Master tracking the apprentice
                var masterHediff = other.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
                masterHediff?.apprentices.Add(generated);

                // Add reciprocal relations
                if (!generated.relations.DirectRelationExists(ForceDefOf.Force_ApprenticeRelation, other))
                {
                    generated.relations.AddDirectRelation(ForceDefOf.Force_ApprenticeRelation, other);
                }
                if (!other.relations.DirectRelationExists(ForceDefOf.Force_MasterRelation, generated))
                {
                    other.relations.AddDirectRelation(ForceDefOf.Force_MasterRelation, generated);
                }
            }
            else
            {
                throw new InvalidOperationException("Apprentice already has a master or apprentice hediff missing.");
            }
        }
    }
}
   