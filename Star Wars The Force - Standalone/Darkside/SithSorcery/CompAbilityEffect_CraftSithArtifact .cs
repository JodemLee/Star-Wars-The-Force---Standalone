using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    public class CompProperties_AbilityCraftSithArtifact : CompProperties_AbilityEffect
    {
        public List<TargetArtifactMapping> targetArtifactMappings;

        private Dictionary<string, List<ThingDef>> targetArtifactMapByDef;
        private Dictionary<string, List<ThingDef>> targetArtifactMapByCategory;
        private bool mappingsInitialized;

        public CompProperties_AbilityCraftSithArtifact()
        {
            compClass = typeof(CompAbilityEffect_CraftSithArtifact);
        }

        public void InitializeMappings()
        {
            if (mappingsInitialized || targetArtifactMappings == null)
                return;

            targetArtifactMapByDef = targetArtifactMappings
                .Where(mapping => mapping.targetDefNames != null)
                .SelectMany(mapping => mapping.targetDefNames.Select(defName => new { defName, mapping.craftableArtifacts }))
                .ToDictionary(x => x.defName, x => x.craftableArtifacts);

            targetArtifactMapByCategory = targetArtifactMappings
                .Where(mapping => mapping.targetCategories != null)
                .SelectMany(mapping => mapping.targetCategories.Select(category => new { category, mapping.craftableArtifacts }))
                .ToDictionary(x => x.category, x => x.craftableArtifacts);

            mappingsInitialized = true;
        }

        public List<ThingDef> GetCraftableArtifactsForTarget(Thing target)
        {
            if (!mappingsInitialized)
                InitializeMappings();

            var targetDefName = target.def.defName;
            var targetCategories = target.def.thingCategories?.Select(c => c.defName) ?? Enumerable.Empty<string>();

            if (targetArtifactMapByDef != null && targetArtifactMapByDef.TryGetValue(targetDefName, out var artifactsByDef))
            {
                return artifactsByDef;
            }

            if (targetArtifactMapByCategory != null)
            {
                foreach (var category in targetCategories)
                {
                    if (targetArtifactMapByCategory.TryGetValue(category, out var artifactsByCategory))
                    {
                        return artifactsByCategory;
                    }
                }
            }

            return new List<ThingDef>();
        }

        public IEnumerable<ThingDef> GetAllPossibleArtifacts()
        {
            if (!mappingsInitialized)
                InitializeMappings();

            var allArtifacts = new HashSet<ThingDef>();

            if (targetArtifactMappings != null)
            {
                foreach (var mapping in targetArtifactMappings)
                {
                    if (mapping.craftableArtifacts != null)
                    {
                        foreach (var artifact in mapping.craftableArtifacts)
                        {
                            allArtifacts.Add(artifact);
                        }
                    }
                }
            }

            return allArtifacts;
        }
    }

    public class CompAbilityEffect_CraftSithArtifact : CompAbilityEffect
    {
        public const float FPPerValue = 0.1f; // 1 FP per 10 silver value
        private const int MinResourcesToDestroy = 1;

        public new CompProperties_AbilityCraftSithArtifact Props => (CompProperties_AbilityCraftSithArtifact)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.HasThing && Valid(target, true))
            {
                Find.WindowStack.Add(new Dialog_ChooseArtifactToCraft(this, target.Thing));
            }
            else
            {
                Messages.Message("Force.InvalidTargetForCrafting".Translate(), MessageTypeDefOf.RejectInput, false);
            }
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = false)
        {
            if (!base.Valid(target, showMessages)) return false;
            if (!target.HasThing) return false;

            // Check if pawn has enough FP
            var forceUser = parent.pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null || forceUser.currentFP <= 0)
            {
                if (showMessages)
                {
                    Messages.Message("Not enough Force Points to craft artifacts.", MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }

            // Check valid target materials
            string targetDefName = target.Thing.def.defName;
            var targetCategories = target.Thing.def.thingCategories.Select(c => c.defName).ToList();

            bool isValidTarget = Props.targetArtifactMappings?
                .Any(mapping =>
                {
                    bool defMatches = mapping.targetDefNames != null && mapping.targetDefNames.Contains(targetDefName);
                    bool categoryMatches = mapping.targetCategories != null && mapping.targetCategories.Any(category => targetCategories.Contains(category));
                    return defMatches || categoryMatches;
                }) ?? false;

            if (!isValidTarget && showMessages)
            {
                Messages.Message("Force.InvalidTargetForCrafting".Translate(), MessageTypeDefOf.RejectInput, false);
            }

            return isValidTarget;
        }

        public List<ThingDef> GetCraftableArtifactsForTarget(Thing target)
        {
            string targetDefName = target.def.defName;
            var targetCategories = target.def.thingCategories.Select(c => c.defName).ToList();

            var mapping = Props.targetArtifactMappings?
                .FirstOrDefault(x =>
                    (x.targetDefNames != null && x.targetDefNames.Contains(targetDefName)) ||
                    (x.targetCategories != null && x.targetCategories.Any(category => targetCategories.Contains(category)))
                );

            return mapping?.craftableArtifacts ?? new List<ThingDef>();
        }

        public void CraftArtifact(ThingDef artifactDef, Thing targetThing)
        {
            var forceUser = parent.pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null) return;

            // Calculate FP cost based on artifact value
            float artifactValue = artifactDef.BaseMarketValue;
            float fpCost = Mathf.Clamp(artifactValue * FPPerValue, 5f, forceUser.MaxFP * 0.5f); // Minimum 5 FP, maximum 50% of max FP

            // Check if pawn can afford the FP cost
            if (!forceUser.TrySpendFP(fpCost))
            {
                Messages.Message("Not enough Force Points to craft this artifact.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Create artifact
            ThingDef stuffDef = targetThing.def.stuffProps != null ? targetThing.def : ThingDefOf.Steel;
            Thing artifact = artifactDef.MadeFromStuff ?
                ThingMaker.MakeThing(artifactDef, stuffDef) :
                ThingMaker.MakeThing(artifactDef);

            GenSpawn.Spawn(artifact, targetThing.Position, parent.pawn.Map);

            // Consume resources
            int resourcesToDestroy = Mathf.Clamp(Mathf.FloorToInt(artifactValue / 100f), MinResourcesToDestroy, targetThing.stackCount);

            if (resourcesToDestroy < targetThing.stackCount)
            {
                targetThing.stackCount -= resourcesToDestroy;
            }
            else
            {
                targetThing.Destroy();
            }

            Messages.Message($"Crafted {artifactDef.LabelCap} for {fpCost.ToString("F1")} FP", parent.pawn, MessageTypeDefOf.PositiveEvent);
        }
    }

    public class Dialog_ChooseArtifactToCraft : Window
    {
        private CompAbilityEffect_CraftSithArtifact compEffect;
        private Thing targetThing;
        private Vector2 scrollPosition = Vector2.zero;

        public Dialog_ChooseArtifactToCraft(CompAbilityEffect_CraftSithArtifact compEffect, Thing targetThing)
        {
            this.compEffect = compEffect;
            this.targetThing = targetThing;
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(600f, 400f);

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // Display current FP status
            var forceUser = compEffect.parent.pawn.GetComp<CompClass_ForceUser>();
            if (forceUser != null)
            {
                listing.Label($"Current FP: {forceUser.currentFP.ToString("F1")}/{forceUser.MaxFP.ToString("F1")}");
            }

            listing.Label("Select artifact to craft:");
            listing.End();

            List<ThingDef> artifacts = compEffect.GetCraftableArtifactsForTarget(targetThing);
            if (artifacts.NullOrEmpty())
            {
                Widgets.Label(new(inRect.x, inRect.y + 30f, inRect.width, 30f), "No artifacts available for this target.");
                return;
            }

            float viewHeight = artifacts.Sum(a => 150f + Text.CalcHeight(a.description, inRect.width - 120f));
            Rect viewRect = new(0f, 0f, inRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(new(inRect.x, inRect.y + 60f, inRect.width, inRect.height - 100f),
                ref scrollPosition, viewRect);

            float y = 0f;
            foreach (ThingDef def in artifacts)
            {
                float artifactValue = def.BaseMarketValue;
                float fpCost = Mathf.Clamp(artifactValue * CompAbilityEffect_CraftSithArtifact.FPPerValue, 5f, forceUser?.MaxFP * 0.5f ?? 50f);

                float height = 150f + Text.CalcHeight(def.description, viewRect.width - 120f);
                DrawArtifactOption(new(0f, y, viewRect.width, height), def, fpCost);
                y += height;
            }

            Widgets.EndScrollView();
        }

        private void DrawArtifactOption(Rect rect, ThingDef def, float fpCost)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.3f));

            // Icon
            Rect iconRect = new(rect.x + 10f, rect.y + 10f, 100f, 100f);
            Widgets.DrawTextureFitted(iconRect, def.uiIcon, 1f);

            // Label
            Rect labelRect = new(iconRect.xMax + 10f, rect.y + 10f, rect.width - iconRect.width - 80f, 30f);
            Widgets.Label(labelRect, def.LabelCap);

            // FP Cost
            Rect fpRect = new(iconRect.xMax + 10f, labelRect.yMax + 5f, rect.width - iconRect.width - 80f, 25f);
            Widgets.Label(fpRect, $"FP Cost: {fpCost.ToString("F1")}");

            // Value
            Rect valueRect = new(iconRect.xMax + 10f, fpRect.yMax + 5f, rect.width - iconRect.width - 80f, 25f);
            Widgets.Label(valueRect, $"Value: {def.BaseMarketValue.ToString()}");

            // Description
            Text.WordWrap = true;
            Rect descRect = new(iconRect.xMax + 10f, valueRect.yMax + 5f, rect.width - iconRect.width - 20f,
                Text.CalcHeight(def.description, rect.width - iconRect.width - 20f));
            Widgets.Label(descRect, def.description);

            // Craft button
            Rect buttonRect = new(rect.xMax - 70f, rect.yMax - 40f, 60f, 30f);
            bool canAfford = compEffect.parent.pawn.GetComp<CompClass_ForceUser>()?.currentFP >= fpCost;

            GUI.color = canAfford ? Color.white : Color.gray;
            if (Widgets.ButtonText(buttonRect, "Craft"))
            {
                if (canAfford)
                {
                    compEffect.CraftArtifact(def, targetThing);
                    Close();
                }
                else
                {
                    Messages.Message("Not enough Force Points", MessageTypeDefOf.RejectInput, false);
                }
            }
            GUI.color = Color.white;
        }
    }

    public class TargetArtifactMapping
    {
        public List<string> targetDefNames;
        public List<string> targetCategories;
        public List<ThingDef> craftableArtifacts;
    }
}


