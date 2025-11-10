using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Lightside
{
    internal class CompAbilityEffect_SecondWind : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            foreach (Need need in parent.pawn.needs.AllNeeds)
            {
                need.CurLevel = need.MaxLevel;
            }

            CompClass_ForceUser forceUser = parent.pawn.TryGetComp<CompClass_ForceUser>();
        }
    }

    public class CompProperties_AbilitySecondWind : CompProperties_AbilityEffect
    {
        public CompProperties_AbilitySecondWind()
        {
            compClass = typeof(CompAbilityEffect_SecondWind);
        }
    }
}
