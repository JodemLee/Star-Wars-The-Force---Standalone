using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Nightsister
{
    internal class CompAbilityEffect_Tempest : CompAbilityEffect
    {
        private static readonly ThingDef TornadoDef = ThingDef.Named("Tornado");

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.Cell == null) return;
            GenSpawn.Spawn(TornadoDef, target.Cell, parent.pawn.Map);
            FleckMaker.ThrowSmoke(target.Cell.ToVector3Shifted(), parent.pawn.Map, 1.5f);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Cell == null)
            {
                if (throwMessages)
                    Messages.Message("AbilityMustTargetValidLocation".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            return base.Valid(target, throwMessages);
        }

    }
}
