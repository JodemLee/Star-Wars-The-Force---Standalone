using System;
using System.Collections.Generic;
using Verse;

namespace TheForce_Standalone.HediffComps
{
    internal class HediffComp_StunDuration : HediffComp
    {
        private bool stunApplied = false;
        public int stunTicksRemaining = 0;
        public int initialStunDuration = 0;

        public HediffCompProperties_StunDuration Props => (HediffCompProperties_StunDuration)props;

        public override string CompDebugString()
        {
            return base.CompDebugString() +
                   $"\nStun applied: {stunApplied}" +
                   $"\nStun ticks remaining: {stunTicksRemaining}" +
                   $"\nInitial stun duration: {initialStunDuration}" +
                   $"\nPawn stunned: {Pawn.stances.stunner.Stunned}" +
                   $"\nPawn stun ticks left: {Pawn.stances.stunner.StunTicksLeft}";
        }

        public override void CompPostPostRemoved()
        {
            Pawn.stances.stunner.StopStun();
        }
    }

    public class HediffCompProperties_StunDuration : HediffCompProperties
    {
        public HediffCompProperties_StunDuration()
        {
            this.compClass = typeof(HediffComp_StunDuration);
        }
    }
}