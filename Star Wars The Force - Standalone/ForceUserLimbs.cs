using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone
{
    public class ForceUserLimbs
    {
        private readonly CompClass_ForceUser parent;
        private List<BodyPartRecord> cachedMissingLimbs;
        private int lastMissingLimbsCheckTick = -1;

        public ForceUserLimbs(CompClass_ForceUser parent)
        {
            this.parent = parent;
        }

        public bool HasReplaceableLimbs()
        {
            return GetMissingLimbs().Count > 0;
        }

        public void ReplaceMissingLimbs()
        {
            var missingLimbs = GetMissingLimbs();
            if (missingLimbs.Count == 0) return;

            foreach (var part in missingLimbs)
            {
                Hediff existing = parent.Pawn.health.hediffSet.hediffs
                    .FirstOrDefault(h => h.Part == part && h.def == HediffDefOf.MissingBodyPart);
                if (existing != null)
                    parent.Pawn.health.RemoveHediff(existing);

                bool isArm = part.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbCore);
                HediffDef forceLimbDef = isArm
                    ? HediffDef.Named("Force_ConstructedArm")
                    : HediffDef.Named("Force_ConstructedLimb");

                Hediff forceLimb = HediffMaker.MakeHediff(forceLimbDef, parent.Pawn, part);
                parent.Pawn.health.AddHediff(forceLimb, part);
                FleckMaker.ThrowLightningGlow(parent.Pawn.DrawPos, parent.Pawn.Map, 1.5f);
            }

            Messages.Message("Force.Limbs.ReplacementMessage".Translate(parent.Pawn.LabelShort),
                MessageTypeDefOf.PositiveEvent);
        }

        private List<BodyPartRecord> GetMissingLimbs()
        {
            if (cachedMissingLimbs == null || lastMissingLimbsCheckTick + 60 < Find.TickManager.TicksGame)
            {
                cachedMissingLimbs = parent.Pawn.health.hediffSet
                    .GetMissingPartsCommonAncestors()
                    .Select(h => h.Part)
                    .ToList();
                lastMissingLimbsCheckTick = Find.TickManager.TicksGame;
            }
            return cachedMissingLimbs;
        }
    }
}