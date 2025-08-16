using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TheForce_Standalone.Nightsister
{
    public class CompAbilityEffect_SoulPyre : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!(target.Thing is Corpse corpse) || corpse.Map == null)
                return;

            FireUtility.TryStartFireIn(
                corpse.Position,
                corpse.Map,
                0.75f,
                parent.pawn
            );

            ThingWithComps shield = (ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("Force_MagickSoulPyre"));
            GenPlace.TryPlaceThing(shield, corpse.Position, corpse.Map, ThingPlaceMode.Direct);

            FleckMaker.ThrowSmoke(corpse.Position.ToVector3Shifted(), corpse.Map, 2f);
            FleckMaker.ThrowFireGlow(corpse.Position.ToVector3Shifted(), corpse.Map, 2f);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!(target.Thing is Corpse))
            {
                if (throwMessages)
                {
                    Messages.Message("AbilityMustTargetCorpse".Translate(), target.ToTargetInfo(parent.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo target, LocalTargetInfo dest)
                {
                    if (target.Thing is Corpse corpse)
                    {
                       
                        FleckMaker.ThrowSmoke(corpse.Position.ToVector3Shifted(), corpse.Map, 1f);
                        FleckMaker.ThrowHeatGlow(corpse.Position, corpse.Map, 1f);
                    }
                },
                ticksAwayFromCast = 10
            };
        }
    }
}

