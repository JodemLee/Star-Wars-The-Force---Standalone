using RimWorld;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Telekinesis
{
    public class CompAbilityEffect_ForceWave : CompAbilityEffect
    {
        public float baseDamage = 1f;
        private Force_ModSettings modSettings = new Force_ModSettings();
        public bool usePsycastStat = false;
        public int offsetMultiplier { get; set; }
        public float coneAngle = 30f;
        public float range = 5f;

        public CompAbilityEffect_ForceWave()
        {
            modSettings = new Force_ModSettings();
        }

        public int GetOffsetMultiplier()
        {
            offsetMultiplier = Force_ModSettings.usePsycastStat
                ? (int)(offsetMultiplier * parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity))
                : (int)Force_ModSettings.offSetMultiplier;
            return offsetMultiplier;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (!target.IsValid || parent.pawn?.Map == null) return;

            var affectedCells = TelekinesisUtility.GetConeCells(
                parent.pawn.Position,
                target.Cell,
                coneAngle,
                range,
                parent.pawn.Map
            );

            foreach (var cell in affectedCells)
            {
                if (cell.GetFirstPawn(parent.pawn.Map) is Pawn targetPawn && targetPawn != parent.pawn)
                {
                    IntVec3 pushPos = TelekinesisUtility.CalculatePushPosition(
                        parent.pawn.Position,
                        cell,
                        GetOffsetMultiplier(),
                        parent.pawn.Map,
                        out bool hitWall
                    );

                    TelekinesisUtility.LaunchPawn(
                        targetPawn,
                        pushPos,
                        parent,
                        ForceDefOf.Force_ThrownPawnWave,
                        parent.pawn.Map,
                        hitWall,
                        hitWall ? pushPos : (IntVec3?)null
                    );
                }
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return base.CanApplyOn(target, dest) && Valid(target);
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.Valid(target, showMessages))
            {
                return false;
            }
            return true;
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            base.DrawEffectPreview(target);

            if (target.IsValid)
            {
                GenDraw.DrawFieldEdges(
                    TelekinesisUtility.GetConeCells(
                        parent.pawn.Position,
                        target.Cell,
                        coneAngle,
                        range,
                        parent.pawn.Map
                    ).ToList()
                );
            }
        }
    }
}

