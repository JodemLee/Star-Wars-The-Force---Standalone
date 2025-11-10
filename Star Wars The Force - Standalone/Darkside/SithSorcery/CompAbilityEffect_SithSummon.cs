using RimWorld;
using System.Collections.Generic;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    internal class CompAbilityEffect_SithSummon : CompAbilityEffect
    {
        public new CompProperties_SithSummon Props => (CompProperties_SithSummon)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Map map = parent.pawn.Map;
            IntVec3 entryCell = FindRandomEntryCell(map);

            if (Props.pawnKinds.NullOrEmpty())
            {
                Log.Error("No animal kinds defined in CompProperties_SithSummon");
                return;
            }

            PawnKindDef selectedAnimalKind = Props.pawnKinds.RandomElement();
            int numberToSpawn = Props.numberToSpawn.RandomInRange;

            for (int i = 0; i < numberToSpawn; i++)
            {
                Pawn animal = PawnGenerator.GeneratePawn(selectedAnimalKind);

                GenSpawn.Spawn(animal, entryCell, map);

                var hediff = HediffMaker.MakeHediff(HediffDef.Named("Force_SithExperiment_DarkCorruption"), animal);
                hediff.Severity = Rand.Range(1, 10);
                animal.health.AddHediff(hediff);
                animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
                animal.mindState.exitMapAfterTick = Find.TickManager.TicksGame + Rand.Range(25000, 35000);
            }

            Find.LetterStack.ReceiveLetter(
                "Force.Summon_AbilityName".Translate(),
                "Force.Summon_AbilityDesc".Translate(parent.pawn.NameShortColored, selectedAnimalKind.label, numberToSpawn),
                LetterDefOf.PositiveEvent,
                new TargetInfo(entryCell, map)
            );
        }

        private IntVec3 FindRandomEntryCell(Map map)
        {
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 entryCell, map, CellFinder.EdgeRoadChance_Animal))
                entryCell = CellFinder.RandomEdgeCell(map);
            return entryCell;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (parent.pawn.Map == null)
            {
                if (throwMessages)
                {
                    Messages.Message("AbilityMustBeUsedOnMap".Translate(), MessageTypeDefOf.RejectInput);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }

    public class CompProperties_SithSummon : CompProperties_AbilityEffect
    {
        public List<PawnKindDef> pawnKinds;
        public IntRange numberToSpawn = new IntRange(1, 3);

        public CompProperties_SithSummon()
        {
            compClass = typeof(CompAbilityEffect_SithSummon);
        }
    }
}