using RimWorld;
using TheForce_Standalone.Generic;
using TheForce_Standalone.HediffComps;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    internal class CompAbilityEffect_SithGhostSummon : CompAbilityEffect
    {
        public new CompProperties_SithSummon Props => (CompProperties_SithSummon)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Map map = parent.pawn.Map;
            IntVec3 entryCell = target.Cell;

            if (Props.pawnKinds.NullOrEmpty())
            {
                Log.Error("No animal kinds defined in CompProperties_SithSummon");
                return;
            }

            PawnKindDef selectedAnimalKind = Props.pawnKinds.RandomElement();
            int numberToSpawn = Props.numberToSpawn.RandomInRange;

            for (int i = 0; i < numberToSpawn; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(selectedAnimalKind);
                if (ForceSensitivityUtils.IsValidForceUser(pawn))
                {
                    pawn.TryGetComp<CompClass_ForceUser>(out var forceUser);
                    if (forceUser != null)
                    {
                        forceUser.GhostMechanics.TryReturnAsGhost(ForceDefOf.Force_SithGhost, 1);
                    }

                    var bindingHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Force_Sith_Binding");
                    if (bindingHediff != null)
                    {
                        var hediff = HediffMaker.MakeHediff(bindingHediff, pawn);
                        hediff.Severity = 1;
                        if (hediff != null)
                        {
                            HediffComp_LinkWithEffect hediffComp_Link = hediff.TryGetComp<HediffComp_LinkWithEffect>();
                            if (hediffComp_Link != null)
                            {
                                hediffComp_Link.other = parent.pawn;
                                hediffComp_Link.drawConnection = target == parent.pawn;
                            }
                        }
                        pawn.health.AddHediff(hediff);
                    }


                }
                GenSpawn.Spawn(pawn, entryCell, map);
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
}
