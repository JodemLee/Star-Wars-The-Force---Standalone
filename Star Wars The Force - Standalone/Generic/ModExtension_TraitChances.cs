using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Generic
{
    internal class ModExtension_TraitChances : DefModExtension
    {
        public List<TraitChance> traitChances;

        public override IEnumerable<string> ConfigErrors()
        {
            if (traitChances != null)
            {
                foreach (var tc in traitChances)
                {
                    if (tc.def == null)
                        yield return "Force.TraitChanceNullDef".Translate();
                    if (tc.chance <= 0f)
                        yield return "Force.TraitChanceInvalidChance".Translate(tc.def?.defName ?? "null");
                }
            }
        }
    }

    public class TraitChance : IExposable
    {
        public TraitDef def;
        public int? degree;
        public float chance;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref degree, "degree");
            Scribe_Values.Look(ref chance, "chance", 0f);
        }

        public string GetTraitLabel()
        {
            if (def == null)
            {
                return "Force.UnknownTrait".Translate();
            }

            if (!degree.HasValue)
            {
                return def.degreeDatas.FirstOrDefault()?.label ?? def.LabelCap;
            }

            var degreeData = def.degreeDatas.FirstOrDefault(d => d.degree == degree.Value);
            if (degreeData != null)
            {
                return degreeData.label;
            }

            return def.LabelCap;
        }
    }
}