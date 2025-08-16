using RimWorld;
using Verse;

namespace TheForce_Standalone.Apprenticeship.Ritual
{
    internal class RitualStage_SelectedTarget : RitualStage
    {
        public override TargetInfo GetSecondFocus(LordJob_Ritual ritual)
        {
            return ritual.selectedTarget;
        }
    }
}
