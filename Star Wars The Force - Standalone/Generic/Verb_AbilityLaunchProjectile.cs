using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.Generic
{
    internal class Verb_AbilityLaunchProjectile : Verb_Shoot, IAbilityVerb
    {
        private int shotsFiredThisBurst = 0;
        private bool useBurstLogic => ShotsPerBurst > 1;

        private Ability ability;

        public Ability Ability
        {
            get
            {
                return ability;
            }
            set
            {
                ability = value;
            }
        }


        protected override bool TryCastShot()
        {

            if (Ability == null)
            {
                Log.Error("[Verb_AbilityLaunchProjectile] Ability is NULL! This shouldn't happen.");
                return false;
            }

            if (!Ability.CanCast)
            {
                Log.Warning($"[Verb_AbilityLaunchProjectile] TryCastShot failed: Ability '{Ability.def.defName}' cannot cast");
                return false;
            }

            bool shotFired = base.TryCastShot();

            if (useBurstLogic)
            {
                // Burst fire logic
                shotsFiredThisBurst++;
                if (shotFired && shotsFiredThisBurst >= ShotsPerBurst)
                {
                    int cooldownTicks = Ability.def.cooldownTicksRange.RandomInRange;
                    Ability.StartCooldown(cooldownTicks);
                    shotsFiredThisBurst = 0;
                }
            }
            else
            {
                // Single shot logic - same as Verb_AbilityShoot
                if (shotFired)
                {
                    int cooldownTicks = Ability.def.cooldownTicksRange.RandomInRange;
                    Ability.StartCooldown(cooldownTicks);
                }
            }

            return shotFired;
        }

        public override void Reset()
        {
            base.Reset();
            shotsFiredThisBurst = 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shotsFiredThisBurst, "shotsFiredThisBurst", 0);
            Scribe_References.Look(ref ability, "ability");
        }
    }
}