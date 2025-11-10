using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Generic
{
    public class ModExtension_SyncedApparelColor : DefModExtension
    {
        public List<ColorSyncRule> colorSyncRules;
    }

    public class ColorSyncRule
    {
        public ApparelLayerDef sourceLayer;
        public BodyPartGroupDef sourceBodyPartGroup;
        public ApparelLayerDef targetLayer;
        public BodyPartGroupDef targetBodyPartGroup;
        public bool copyColorToTarget = true;
    }

}
