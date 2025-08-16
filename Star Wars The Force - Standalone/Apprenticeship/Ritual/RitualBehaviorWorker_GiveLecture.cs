using RimWorld;
using Verse;
using Verse.AI.Group;

namespace TheForce_Standalone.Apprenticeship.Ritual
{
    internal class RitualBehaviorWorker_GiveLecture : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_GiveLecture()
        {
        }

        public RitualBehaviorWorker_GiveLecture(RitualBehaviorDef def)
            : base(def)
        {
        }

        protected override LordJob CreateLordJob(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments)
        {
            return new LordJob_Joinable_Class(target, organizer, ritual, def.stages, assignments);
        }
    }
}
