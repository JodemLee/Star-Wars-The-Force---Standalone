using Verse;

namespace TheForce_Standalone.HediffComps
{
    internal class Hediff_ForceReplacement : Hediff_AddedPart
    {
        public override string Label
        {
            get
            {
                string labelInBrackets = LabelInBrackets;
                string bodyPartLabel = Part?.Label ?? "";
                string combinedSuffix = "";

                if (!labelInBrackets.NullOrEmpty() || !bodyPartLabel.NullOrEmpty())
                {
                    combinedSuffix = " (";
                    if (!labelInBrackets.NullOrEmpty()) combinedSuffix += labelInBrackets;
                    if (!bodyPartLabel.NullOrEmpty())
                    {
                        if (!labelInBrackets.NullOrEmpty()) combinedSuffix += " on ";
                        combinedSuffix += bodyPartLabel;
                    }
                    combinedSuffix += ")";
                }

                return LabelBase + combinedSuffix;
            }
        }
    }
}
