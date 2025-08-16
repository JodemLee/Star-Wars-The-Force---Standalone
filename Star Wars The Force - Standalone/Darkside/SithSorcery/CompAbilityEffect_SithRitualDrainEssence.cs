using RimWorld;
using System.Collections.Generic;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    internal class CompAbilityEffect_SithRitualDrainEssence : CompAbilityEffect
    {
        private const float DrainAmount = 0.1f;
        private const int DurationTicks = 600;

        private int ticksLeft = DurationTicks;
        private bool shouldDrain = false;
        private List<Pawn> enemyPawnsToDrain = new List<Pawn>();

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (target.Thing is Pawn enemy && enemy.Faction != parent.pawn.Faction)
            {
                enemyPawnsToDrain.Add(enemy);
                shouldDrain = true;
            }
        }

        private void ApplyDrainEffect(Pawn enemy)
        {
            if (enemy.needs?.rest != null)
            {
                enemy.needs.rest.CurLevel -= DrainAmount;
            }

            if (enemy.needs?.food != null)
            {
                enemy.needs.food.CurLevel -= DrainAmount;
            }

            if (parent.pawn.needs?.rest != null)
            {
                parent.pawn.needs.rest.CurLevel += DrainAmount / 2;
            }

            if (parent.pawn.needs?.food != null)
            {
                parent.pawn.needs.food.CurLevel += DrainAmount / 2;
            }

            if (parent.pawn.TryGetComp<CompClass_ForceUser>(out var forceUser))
            {
                forceUser.RecoverFP(DrainAmount * 10f);
                forceUser.Leveling.AddForceExperience(DrainAmount);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (shouldDrain && ticksLeft > 0)
            {
                ticksLeft--;
                if (ticksLeft % 60 == 0)
                {
                    foreach (var enemy in enemyPawnsToDrain)
                    {
                        if (enemy != null && !enemy.Destroyed)
                        {
                            ApplyDrainEffect(enemy);
                        }
                    }
                }

                if (ticksLeft == 0)
                {
                    shouldDrain = false;
                    enemyPawnsToDrain.Clear();
                }
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Pawn == null)
            {
                if (throwMessages)
                {
                    Messages.Message("AbilityMustTargetPawn".Translate(), MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            if (target.Pawn.Faction == parent.pawn.Faction)
            {
                if (throwMessages)
                {
                    Messages.Message("AbilityMustTargetEnemy".Translate(), MessageTypeDefOf.RejectInput);
                }
                return false;
            }

            return base.Valid(target, throwMessages);
        }
    }
}
