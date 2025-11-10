using HarmonyLib;
using RimWorld;
using System;
using System.IO;
using System.Runtime.InteropServices;
using TheForce_Standalone.HarmonyPatches;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class Force_ModSettings : ModSettings
    {
        public static bool usePsycastStat = false;
        public static float offSetMultiplier = 1;
        public static int apprenticeCapacity = 1;
        public static bool darksideVisuals = false;
        public static float insanityChance = 1f;
        public static bool IncreaseDarksideOnKill = true;
        public static bool rankUpApprentice = false;
        public static bool rankUpMaster = false;
        public static int requiredGraduatedApprentices = 1;
        public static int forceXpMultiplier = 1;
        public static int requiredforceLevel = 10;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref offSetMultiplier, "offSetMultiplier", 3f);
            Scribe_Values.Look(ref usePsycastStat, "usePsycastStat");
            Scribe_Values.Look(ref apprenticeCapacity, "apprenticeCapacity", 1);
            Scribe_Values.Look(ref insanityChance, "insanityChance", 0.25f);
            Scribe_Values.Look(ref rankUpApprentice, "rankUpApprentice", false);
            Scribe_Values.Look(ref rankUpMaster, "rankUpMaster", false);
            Scribe_Values.Look(ref requiredGraduatedApprentices, "requiredGraduatedApprentices", 1);
            Scribe_Values.Look(ref requiredforceLevel, "requiredforceLevel", 10);
            Scribe_Values.Look(ref darksideVisuals, "darksideVisuals", false);
            Scribe_Values.Look(ref forceXpMultiplier, "forceXpMultiplier", 1);
        }
    }

    public class TheForce_Mod : Mod
    {
        private enum Tab
        {
            General,
            Projectiles
        }

        private Tab currentTab = Tab.General;
        Force_ModSettings settings;
        Vector2 scrollPosition = Vector2.zero;
        public static TheForce_Mod Force_Mod;

        public TheForce_Mod(ModContentPack content) : base(content)
        {
            Force_Mod = this;
            settings = GetSettings<Force_ModSettings>();

        }

        public static void ShaderFromAssetBundle(ShaderTypeDef __instance, ref Shader ___shaderInt)
        {
            if (__instance is not ForceShaderDef) return;
            ___shaderInt = ForceContentDatabaseStandalone.ForceBundle.LoadAsset<Shader>(__instance.shaderPath);

            if (___shaderInt is null)
            {
                Log.Error("Force.Error.ShaderLoadFailed".Translate(__instance.shaderPath));
            }
        }

        public AssetBundle MainBundle
        {
            get
            {
                string text = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "StandaloneOSX"
                    : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "StandaloneWindows64"
                    : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "StandaloneLinux64"
                    : throw new PlatformNotSupportedException("Force.Error.UnsupportedPlatform".Translate());

                string bundlePath = Path.Combine(Content.RootDir,
                    @"Asset_Bundles\" + text + "\\force_shaders.assetbundle");

                AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

                if (bundle == null)
                {
                    Log.Error("Force.Error.BundleLoadFailed".Translate(bundlePath));
                }

                return bundle;
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            Rect tabRect = new(inRect.x, inRect.y - 30f, inRect.width, 30);
            Rect generalTabRect = tabRect.LeftHalf().ContractedBy(4f);
            Rect projectilesTabRect = tabRect.RightHalf().ContractedBy(4f);

            if (Widgets.ButtonText(generalTabRect, "Force.Settings.GeneralTab".Translate()))
            {
                currentTab = Tab.General;
            }

            // Space after tabs
            listingStandard.Gap(36f);

            switch (currentTab)
            {
                case Tab.General:
                    DrawGeneralSettings(inRect);
                    break;
            }

            listingStandard.End();
        }

        private void DrawGeneralSettings(Rect inRect)
        {
            Rect scrollRect = new(0, 0, inRect.width - 16f, 1000);
            Widgets.BeginScrollView(inRect, ref scrollPosition, scrollRect);

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(scrollRect);

            listingStandard.CheckboxLabeled("Force.Settings.UsePsycastStat".Translate(),
                ref Force_ModSettings.usePsycastStat,
                "Force.Settings.UsePsycastStatDesc".Translate());

            if (!Force_ModSettings.usePsycastStat)
            {
                listingStandard.Label("Force.Settings.OffsetMultiplier".Translate(Force_ModSettings.offSetMultiplier));
                Force_ModSettings.offSetMultiplier = listingStandard.Slider(Force_ModSettings.offSetMultiplier, 1f, 10f);
            }

            listingStandard.Label("Force.Settings.ApprenticeCapacity".Translate(Force_ModSettings.apprenticeCapacity));
            Force_ModSettings.apprenticeCapacity = (int)listingStandard.Slider(Force_ModSettings.apprenticeCapacity, 1, 10);

            listingStandard.Label("Force.Settings.ForceXPMultiplier".Translate(Force_ModSettings.forceXpMultiplier));
            Force_ModSettings.forceXpMultiplier = (int)listingStandard.Slider(Force_ModSettings.forceXpMultiplier, 1, 10);

            listingStandard.CheckboxLabeled("Force.Settings.ApprenticeRankup".Translate(),
                ref Force_ModSettings.rankUpApprentice,
                "Force.Settings.ApprenticeRankupDesc".Translate());

            if (Force_ModSettings.rankUpApprentice)
            {
                listingStandard.CheckboxLabeled("Force.Settings.MasterRankup".Translate(),
                    ref Force_ModSettings.rankUpMaster,
                    "Force.Settings.MasterRankupDesc".Translate());

                if (Force_ModSettings.rankUpMaster)
                {
                    listingStandard.Label("Force.Settings.RequiredApprentices".Translate(Force_ModSettings.requiredGraduatedApprentices));
                    Force_ModSettings.requiredGraduatedApprentices = (int)listingStandard.Slider(Force_ModSettings.requiredGraduatedApprentices, 1, 10);

                    listingStandard.Label("Force.Settings.RequiredPsycast".Translate(Force_ModSettings.requiredforceLevel));
                    Force_ModSettings.requiredforceLevel = (int)listingStandard.Slider(Force_ModSettings.requiredforceLevel, 1, 100);
                }
            }

            listingStandard.CheckboxLabeled("Force.Settings.DarksideVisuals".Translate(),
                ref Force_ModSettings.darksideVisuals,
                "Force.Settings.DarksideVisualsDesc".Translate());

            listingStandard.Gap();
            if (listingStandard.ButtonText("Force.Settings.ResetToDefault".Translate()))
            {
                ResetToDefaultValues();
            }

            listingStandard.End();
            Widgets.EndScrollView();
        }

        private void ResetToDefaultValues()
        {
            Force_ModSettings.usePsycastStat = false;
            Force_ModSettings.offSetMultiplier = 3f;
            Force_ModSettings.apprenticeCapacity = 1;
            Force_ModSettings.rankUpMaster = false;
            Force_ModSettings.rankUpApprentice = false;
            Force_ModSettings.requiredGraduatedApprentices = 1;
            Force_ModSettings.requiredforceLevel = 5;
        }

        public override string SettingsCategory()
        {
            return "Force.Settings.ModName".Translate();
        }
    }
}