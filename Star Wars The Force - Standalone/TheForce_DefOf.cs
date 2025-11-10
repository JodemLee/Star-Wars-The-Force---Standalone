using RimWorld;
using Verse;

namespace TheForce_Standalone
{
    [DefOf]
    public static class ForceDefOf
    {
        // ThingDefs
        public static HediffDef Force_Master;
        public static HediffDef ForceBond_MasterApprentice;
        public static HediffDef Force_Apprentice;
        public static HediffDef Force_TeachingCooldown;
        public static HediffDef Force_Phantom;
        public static DamageDef Force_Lightning;

        // BackStoryDefs
        public static BackstoryDef Force_SithApprenticeChosen;
        public static BackstoryDef Force_JediPadawanChosen;
        public static BackstoryDef Force_SithMaster;
        public static BackstoryDef Force_JediMaster;


        public static PawnRelationDef Force_ApprenticeRelation;
        public static PawnRelationDef Force_MasterRelation;

        public static StatDef Force_Darkside_Attunement;
        public static StatDef Force_Lightside_Attunement;

        public static HediffDef Force_Ghost;
        public static HediffDef Force_SithGhost;
        public static HediffDef Force_SithZombie;
        public static HediffDef Force_ProtectionBubble;

        public static ThingDef Force_ThrownPawnPush;
        public static ThingDef Force_ThrownPawnWave;
        public static ThingDef Force_ThrownPawnRepulse;
        public static ThingDef Force_ThrownPawnPull;
        public static ThingDef Force_ThrownPawnAttract;
        public static ThingDef Force_ThrownPawnThrow;
        public static ThingDef Force_ChokedPawn;


        public static ThingDef Force_NightsisterSoulIchor;
        public static FleckDef Force_MagickSkipFlashEntry;
        public static FleckDef Force_MagickSkipInnerExit;
        public static FleckDef Force_MagickSkipOuterRingExit;
        public static FleckDef Force_MagickIchor;
        public static EffecterDef Force_Magick_Exit;
        public static EffecterDef Force_Magick_Entry;
        public static HediffDef Force_TemporaryHearingLoss;

        public static AbilityDef Force_Apprenticeship;

        public static PawnKindDef Force_DarksideWraith;
        public static ThingDef Force_DarksideWraithRace;

        public static IncidentDef Ambush;

        public static RecordDef Force_DarksideActions;
        public static RecordDef Force_LightsideActions;

        [MayRequireBiotech]
        public static HediffDef Force_MechuLinkImplant;
        [MayRequireBiotech]
        public static HediffDef Force_MechControl;
        [MayRequireBiotech]
        public static HediffDef Force_ImplantFlare;
        [MayRequireBiotech]
        public static HediffDef Force_MechuTuneOverclocking;
        [MayRequireBiotech]
        public static PawnKindDef Force_Mech_Inquisitor;

        public static HediffDef Force_SithRitualDrainEssence;



    }
}
