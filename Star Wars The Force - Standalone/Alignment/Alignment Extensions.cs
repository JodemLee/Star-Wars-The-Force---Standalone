using Verse;

namespace TheForce_Standalone.Alignment
{
    public class MeditationBuilding_Alignment : DefModExtension
    {
        public AlignmentType alignmenttoIncrease;
        public AlignmentType alignmenttoDecrease;
    }

    public class ThoughtAlignmentExtension : DefModExtension
    {
        public AlignmentType alignment;
        public float alignmentIncrease = 0.1f;
    }

    public enum AlignmentType
    {
        Darkside,
        Lightside
    }
}
