using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.HediffComps
{
    [StaticConstructorOnStartup]
    public class HediffComp_Shield : HediffComp
    {
        private Material BubbleMat => MaterialPool.MatFrom(Props.bubbleGraphicPath, ShaderDatabase.TransparentPostLight);

        private MaterialPropertyBlock propertyBlock;
        private Material baseBubbleMat;
        private Mote mote;
        private int lastMoteSpawnTick = -9999;
        private const int MoteSpawnInterval = 10;

        private MaterialPropertyBlock PropertyBlock
        {
            get
            {
                if (propertyBlock == null)
                {
                    propertyBlock = new MaterialPropertyBlock();
                }
                return propertyBlock;
            }
        }

        private Material BaseBubbleMat
        {
            get
            {
                if (baseBubbleMat == null)
                {
                    baseBubbleMat = MaterialPool.MatFrom(Props.bubbleGraphicPath, ShaderDatabase.TransparentPostLight);
                }
                return baseBubbleMat;
            }
        }

        public float EnergyMax => Props.energyMax;
        private float EnergyLossPerDamage => Props.energyLossPerDamage;
        private float EnergyOnReset => Props.energyOnReset;
        private float EnergyGainPerTick => Props.energyGainPerTick;
        private int StartingTicksToReset => Props.ticksToReset;
        private int KeepDisplayingTicks => Props.keepDisplayingTicks;
        private float MinDrawSize => Props.minDrawSize;
        private float MaxDrawSize => Props.maxDrawSize;
        private float MaxDamagedJitterDist => Props.maxDamagedJitterDist;
        private int JitterDurationTicks => Props.jitterDurationTicks;
        public CompProperties_Shield Props => (CompProperties_Shield)props;

        private float energy;
        private Vector3 impactAngleVect;
        private int lastAbsorbDamageTick = -9999;
        private int lastKeepDisplayTick = -9999;
        private int ticksToReset = -1;

        public string LabelCap => base.Def.LabelCap;
        public string Label => base.Def.label;
        public float Energy => energy;

        public ShieldState ShieldState => ticksToReset > 0 ? ShieldState.Resetting : ShieldState.Active;

        public override void CompPostMake()
        {
            base.CompPostMake();
            this.energy = EnergyMax;
        }

        private bool ShouldDisplay
        {
            get
            {
                if (Pawn.Dead || Pawn.Downed)
                {
                    if (!Pawn.Faction.HostileTo(Faction.OfPlayer))
                    {
                        return Find.TickManager.TicksGame < lastKeepDisplayTick + KeepDisplayingTicks;
                    }
                }
                else if (Pawn.IsPrisonerOfColony)
                {
                    var mentalState = Pawn.MentalStateDef;
                    if (mentalState == null || !mentalState.IsAggro)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref energy, "energy", 0f);
            Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
            Scribe_Values.Look(ref lastKeepDisplayTick, "lastKeepDisplayTick", 0);
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Find.Selector.SingleSelectedThing == Pawn)
            {
                yield return new Gizmo_HediffShieldStatus { shield = this };
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn == null)
            {
                energy = 0f;
                if (mote != null && !mote.Destroyed)
                {
                    mote.Destroy();
                }
                return;
            }

            switch (ShieldState)
            {
                case ShieldState.Resetting:
                    if (--ticksToReset <= 0) Reset();
                    break;

                case ShieldState.Active:
                    energy = Mathf.Min(energy + EnergyGainPerTick, EnergyMax);
                    break;
            }

            if (Props.useMoteInsteadOfBubble && ShouldDisplay)
            {
                if (Find.TickManager.TicksGame > lastMoteSpawnTick + MoteSpawnInterval)
                {
                    UpdateMote();
                    lastMoteSpawnTick = Find.TickManager.TicksGame;
                }
            }
            else if (mote != null && !mote.Destroyed)
            {
                mote.Destroy();
                mote = null;
            }
        }

        private void UpdateMote()
        {
            if (mote == null || mote.Destroyed)
            {
                mote = MoteMaker.MakeAttachedOverlay(Pawn, DefDatabase<ThingDef>.GetNamed(Props.moteDefName), Vector3.zero);
                if (mote == null) return;
            }
            float size = Mathf.Lerp(MinDrawSize, MaxDrawSize, energy);
            Vector3 offset = Vector3.zero;
            int ticksSinceDamage = Find.TickManager.TicksGame - lastAbsorbDamageTick;
            if (ticksSinceDamage < JitterDurationTicks)
            {
                float jitterOffset = (JitterDurationTicks - ticksSinceDamage) / (float)JitterDurationTicks * MaxDamagedJitterDist;
                offset = impactAngleVect * jitterOffset;
                size -= jitterOffset;
            }

            mote.exactPosition = Pawn.DrawPos + offset;

            Color drawColor = Props.shieldColor;
            if (Props.usePawnFavoriteColor && Pawn.story != null && ModsConfig.IdeologyActive)
            {
                drawColor = parent.pawn.story.favoriteColor.color;
            }

            if (mote is MoteAttached moteAttached)
            {
                moteAttached.instanceColor = drawColor;
            }
        }

        public bool CheckPreAbsorbDamage(DamageInfo dinfo)
        {
            if (ShieldState != ShieldState.Active) return false;
            if (dinfo.Instigator != null && dinfo.Instigator.Position.AdjacentTo8WayOrInside(Pawn.Position) && !dinfo.Def.isExplosive) return false;
            if (dinfo.Instigator is AttachableThing attachable && attachable.parent == Pawn) return false;

            energy -= dinfo.Amount * EnergyLossPerDamage;

            if (energy < 0f)
                Break();
            else
                AbsorbedDamage(dinfo);

            return true;
        }

        public void KeepDisplaying() => lastKeepDisplayTick = Find.TickManager.TicksGame;

        private void AbsorbedDamage(DamageInfo dinfo)
        {
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
            impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);

            Vector3 loc = Pawn.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
            float flashSize = Mathf.Min(10f, 2f + dinfo.Amount / 10f);

            FleckMaker.Static(loc, Pawn.Map, FleckDefOf.ExplosionFlash, flashSize);

            for (int i = 0; i < (int)flashSize; i++)
            {
                FleckMaker.ThrowDustPuff(loc, Pawn.Map, Rand.Range(0.8f, 1.2f));
            }

            lastAbsorbDamageTick = Find.TickManager.TicksGame;
            KeepDisplaying();
        }

        private void Break()
        {
            SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
            Vector3 center = Pawn.TrueCenter();

            FleckMaker.Static(center, Pawn.Map, FleckDefOf.ExplosionFlash, 12f);

            for (int i = 0; i < 6; i++)
            {
                Vector3 pos = center + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f);
                FleckMaker.ThrowDustPuff(pos, Pawn.Map, Rand.Range(0.8f, 1.2f));
            }

            energy = 0f;
            ticksToReset = StartingTicksToReset;

            if (mote != null && !mote.Destroyed)
            {
                mote.Destroy();
                mote = null;
            }
        }

        private void Reset()
        {
            if (Pawn.Spawned)
            {
                SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
                FleckMaker.ThrowLightningGlow(Pawn.TrueCenter(), Pawn.Map, 3f);
            }
            ticksToReset = -1;
            energy = EnergyOnReset;
        }

        public void DrawWornExtras()
        {
            if (Props.useMoteInsteadOfBubble) return;
            if (ShieldState != ShieldState.Active || !ShouldDisplay) return;

            float size = Mathf.Lerp(MinDrawSize, MaxDrawSize, energy);
            Vector3 drawPos = Pawn.Drawer.DrawPos;
            drawPos.y = AltitudeLayer.Blueprint.AltitudeFor();

            int ticksSinceDamage = Find.TickManager.TicksGame - lastAbsorbDamageTick;
            if (ticksSinceDamage < JitterDurationTicks)
            {
                float jitterOffset = (JitterDurationTicks - ticksSinceDamage) / (float)JitterDurationTicks * MaxDamagedJitterDist;
                drawPos += impactAngleVect * jitterOffset;
                size -= jitterOffset;
            }

            Color drawColor = Props.shieldColor;
            if (Props.usePawnFavoriteColor && Pawn.story != null && ModsConfig.IdeologyActive)
            {
                drawColor = parent.pawn.story.favoriteColor.color;
            }

            PropertyBlock.SetColor(ShaderPropertyIDs.Color, drawColor);

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.AngleAxis(Rand.Range(0, 360), Vector3.up),
                new Vector3(size, 1f, size)
            );

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                BaseBubbleMat,
                0,
                null,
                0,
                PropertyBlock
            );
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (mote != null && !mote.Destroyed)
            {
                mote.Destroy();
            }
        }
    }

    public class CompProperties_Shield : HediffCompProperties
    {
        public float energyMax = 1.1f;
        public float energyLossPerDamage = 0.027f;
        public float energyOnReset = 0.2f;
        public float energyGainPerTick = 0.0021666666f;
        public int ticksToReset = 3200;
        public int keepDisplayingTicks = 1000;

        public float minDrawSize = 1.2f;
        public float maxDrawSize = 1.55f;
        public float maxDamagedJitterDist = 0.05f;
        public int jitterDurationTicks = 8;
        public string bubbleGraphicPath = "Other/ShieldBubble";
        public bool usePawnFavoriteColor = false;
        public Color shieldColor = Color.white;

        public string moteDefName = "Force_EnergyShield";
        public bool useMoteInsteadOfBubble = true;

        public CompProperties_Shield()
        {
            this.compClass = typeof(HediffComp_Shield);
        }
    }

    [StaticConstructorOnStartup]
    internal class Gizmo_HediffShieldStatus : Gizmo
    {
        private static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public HediffComp_Shield shield;

        private int uniqueID;
        private static int nextID = 984688;

        public Gizmo_HediffShieldStatus()
        {
            uniqueID = nextID++;
        }

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);

            Find.WindowStack.ImmediateWindow(uniqueID, rect, WindowLayer.GameUI, () =>
            {
                Rect contentRect = rect.AtZero().ContractedBy(6f);
                Rect labelRect = contentRect;
                labelRect.height = rect.height / 2f;

                Text.Font = GameFont.Tiny;
                Widgets.Label(labelRect, shield.LabelCap);

                Rect barRect = contentRect;
                barRect.yMin = rect.height / 2f;

                float fillPercent = shield.Energy / Mathf.Max(1f, shield.EnergyMax);
                Widgets.FillableBar(barRect, fillPercent, FullShieldBarTex, EmptyShieldBarTex, false);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(barRect, $"{shield.Energy * 100f:F0} / {shield.EnergyMax * 100f:F0}");
                Text.Anchor = TextAnchor.UpperLeft;
            });

            return new GizmoResult(GizmoState.Clear);
        }
    }

    public enum ShieldState
    {
        Active,
        Resetting
    }
}