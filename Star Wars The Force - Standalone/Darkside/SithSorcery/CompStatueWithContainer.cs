using HarmonyLib;
using Mono.Security.Authenticode;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheForce_Standalone.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Grammar;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    [StaticConstructorOnStartup]
    public class CompStatueWithContainer : CompStatue, IThingHolder
    {
        private ThingOwner innerContainer;
        private Pawn fakePawn;
        private BodyTypeDef bodyType;
        private HeadTypeDef headType;
        private HairDef hairDef;
        private XenotypeDef xenotype;
        private new BeardDef  beard;
        private Color hairColor;
        private PawnPosture HeldPawnPosture;
        private List<ThingDef> apparel = new List<ThingDef>();
        private Dictionary<int, ThingStyleDef> apparelStyles = new Dictionary<int, ThingStyleDef>();
        private List<GeneDef> nonXenotypeGenes = new List<GeneDef>();
        private List<SavedHediffProps> hediffsWhichAffectRendering = new List<SavedHediffProps>();
        private Name name;
        private Gender gender;
        private int lifestageIndex;
        private int descriptionSeed;
        private Dictionary<string, object> additionalSavedPawnDataForMods = new Dictionary<string, object>();
        
        private bool hasStoredPawn = false;

        public override bool Active => fakePawn != null && IsFakePawnValid();

        private bool IsFakePawnValid()
        {
            return fakePawn != null &&
                   fakePawn.story != null &&
                   fakePawn.story.bodyType != null &&
                   fakePawn.Drawer?.renderer != null;
        }

        public CompStatueWithContainer()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public override void JustCreatedBy(Pawn pawn)
        {

        }

        public override void PostPostGeneratedForTrader(TraderKindDef trader, PlanetTile forTile, Faction forFaction)
        {

        }

        public void StorePawn(Pawn pawn)
        {
            if (pawn == null) return;

            hasStoredPawn = true;
            pawn.jobs.StopAll();
            if (Props.storePhysicalPawn)
            {
                if (pawn.Spawned)
                    pawn.DeSpawn();
                innerContainer.TryAdd(pawn);
            }

            CreateSnapshotOfPawn(pawn);
            InitFakePawn();

            var compArt = parent.TryGetComp<CompArt>();
            if (compArt != null && !compArt.Active)
            {
                compArt.InitializeArt(ArtGenerationContext.Colony);
                compArt.Title = pawn.LabelCap;
            }
        }

        private void CreateSnapshotOfPawn(Pawn p)
        {
            p.jobs.StopAll();
            bodyType = p.story?.bodyType ?? BodyTypeDefOf.Male;
            headType = p.story?.headType ?? HeadTypeDefOf.Skull;
            hairDef = p.story?.hairDef ?? HairDefOf.Bald;
            hairColor = p.story?.HairColor ?? Color.white;
            beard = p.style?.beardDef;
            name = p.Name;
            gender = p.gender;
            lifestageIndex = p.ageTracker.CurLifeStageIndex;
            xenotype = p.genes?.Xenotype;
            apparel.Clear();
            apparelStyles.Clear();
            HeldPawnPosture = PawnPosture.Standing;
            int num = 0;
            if (p.apparel != null)
            {
                foreach (Apparel item in p.apparel.WornApparel)
                {
                    // Include ALL apparel including headgear - remove the filtering
                    apparel.Add(item.def);
                    if (item.StyleDef != null)
                    {
                        apparelStyles.Add(num, item.StyleDef);
                    }
                    num++;
                }
            }

            nonXenotypeGenes.Clear();
            if (ModsConfig.BiotechActive && p.genes != null)
            {
                foreach (Gene item2 in p.genes.GenesListForReading)
                {
                    if (xenotype == null || !xenotype.genes.Contains(item2.def))
                    {
                        nonXenotypeGenes.Add(item2.def);
                    }
                }
            }

            hediffsWhichAffectRendering.Clear();
            if (p.health?.hediffSet != null)
            {
                foreach (Hediff hediff in p.health.hediffSet.hediffs)
                {
                    if (hediff.def.HasDefinedGraphicProperties)
                    {
                        hediffsWhichAffectRendering.Add(new SavedHediffProps(hediff.Part, hediff.def, hediff.Severity));
                    }
                }
            }
        }

        // Recreate the InitFakePawn method with comprehensive null checking
        private void InitFakePawn()
        {
            try
            {
                // Clear any existing fake pawn
                if (fakePawn != null)
                {
                    try
                    {
                        fakePawn.Destroy();
                    }
                    catch { }
                    fakePawn = null;
                }

                fakePawn = (Pawn)ThingMaker.MakeThing(ThingDefOf.Human);
                if (fakePawn == null)
                {
                    Log.Error("Failed to create fake pawn for statue");
                    return;
                }

                // Initialize critical components with null checks
                PawnComponentsUtility.CreateInitialComponents(fakePawn);

                if (fakePawn.story == null)
                    fakePawn.story = new Pawn_StoryTracker(fakePawn);
                if (fakePawn.apparel == null)
                    fakePawn.apparel = new Pawn_ApparelTracker(fakePawn);
                if (fakePawn.health == null)
                    fakePawn.health = new Pawn_HealthTracker(fakePawn);
                if (fakePawn.ageTracker == null)
                    fakePawn.ageTracker = new Pawn_AgeTracker(fakePawn);
                if (fakePawn.style == null)
                    fakePawn.style = new Pawn_StyleTracker(fakePawn);

                if (name != null)
                    fakePawn.Name = name;
                fakePawn.gender = gender;
                fakePawn.kindDef = PawnKindDefOf.Colonist;

                // Initialize genes safely
                if (ModsConfig.BiotechActive)
                {
                    if (fakePawn.genes == null)
                        fakePawn.genes = new Pawn_GeneTracker(fakePawn);

                    if (fakePawn.genes != null)
                    {
                        fakePawn.genes.SetXenotype(xenotype ?? XenotypeDefOf.Baseliner);
                    }
                }

                // Set age and life stage
                if (fakePawn.ageTracker != null)
                {
                    fakePawn.ageTracker.LockCurrentLifeStageIndex(lifestageIndex);
                }

                // Set body type and appearance with fallbacks
                if (fakePawn.story != null)
                {
                    fakePawn.story.bodyType = bodyType ?? BodyTypeDefOf.Male;
                    fakePawn.story.headType = headType ?? HeadTypeDefOf.Skull;
                    fakePawn.story.hairDef = hairDef ?? HairDefOf.Bald;
                    fakePawn.story.HairColor = hairColor;
                    fakePawn.jobs.posture = HeldPawnPosture;
                }

                if (fakePawn.style != null)
                {
                    fakePawn.style.beardDef = beard ?? BeardDefOf.NoBeard;
                }

                // Clear and recreate apparel more safely
                if (fakePawn.apparel != null)
                {
                    fakePawn.apparel.DestroyAll();

                    for (int i = 0; i < apparel.Count; i++)
                    {
                        if (i >= apparel.Count) break;

                        ThingDef thingDef = apparel[i];
                        if (thingDef != null)
                        {
                            try
                            {
                                Apparel apparelItem = thingDef.MadeFromStuff ?
                                    (Apparel)ThingMaker.MakeThing(thingDef, GenStuff.DefaultStuffFor(thingDef)) :
                                    (Apparel)ThingMaker.MakeThing(thingDef);

                                if (apparelItem != null)
                                {
                                    if (apparelStyles.TryGetValue(i, out var styleDef) && styleDef != null)
                                    {
                                        apparelItem.StyleDef = styleDef;
                                    }

                                    fakePawn.apparel.WornApparel.Add(apparelItem);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning($"Failed to create apparel {thingDef.defName} for statue: {ex.Message}");
                            }
                        }
                    }
                }

                // Add genes safely
                if (!nonXenotypeGenes.NullOrEmpty() && ModsConfig.BiotechActive && fakePawn.genes != null)
                {
                    foreach (GeneDef nonXenotypeGene in nonXenotypeGenes)
                    {
                        if (nonXenotypeGene != null)
                        {
                            try
                            {
                                fakePawn.genes.AddGene(nonXenotypeGene, xenogene: true);
                            }
                            catch (Exception ex)
                            {
                                Log.Warning($"Failed to add gene {nonXenotypeGene.defName} to statue: {ex.Message}");
                            }
                        }
                    }
                }

                // Add hediffs safely
                if (!hediffsWhichAffectRendering.NullOrEmpty() && fakePawn.health != null)
                {
                    foreach (SavedHediffProps item in hediffsWhichAffectRendering)
                    {
                        if (item?.hediffDef != null)
                        {
                            try
                            {
                                Hediff hediff = fakePawn.health.AddHediff(item.hediffDef, item.bodyPart);
                                if (hediff != null)
                                {
                                    hediff.Severity = item.severity;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning($"Failed to add hediff {item.hediffDef.defName} to statue: {ex.Message}");
                            }
                        }
                    }
                }

                if (fakePawn.Drawer?.renderer != null)
                {
                    try
                    {
                        fakePawn.Drawer.renderer.SetStatue(parent.Stuff);
                        fakePawn.Drawer.renderer.SetAllGraphicsDirty();
                        fakePawn.Drawer.renderer.EnsureGraphicsInitialized();
                        Notify_ColorChanged();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error initializing pawn renderer: {ex}");
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Error in InitFakePawn: {ex}");
                fakePawn = null;
            }
        }

        public override TaggedString GenerateImageDescription()
        {
            if (!Active)
            {
                if (innerContainer.Count > 0 && innerContainer[0] is Pawn storedPawn)
                {
                    CreateSnapshotOfPawn(storedPawn);
                    InitFakePawn();
                }
                else
                {
                    return "StatueOfUnknown".Translate();
                }
            }

            if (!Active) return "StatueOfUnknown".Translate();

            if (descriptionSeed == 0)
            {
                descriptionSeed = Gen.HashCombineInt(fakePawn.thingIDNumber, name?.GetHashCode() ?? 0);
            }

            GrammarRequest request = default(GrammarRequest);
            request.Includes.Add(Props.descriptionMaker);
            request.Rules.AddRange(GrammarUtility.RulesForPawn("SUBJECT", fakePawn, request.Constants));
            request.Rules.Add(new Rule_String("pawn_name", fakePawn.Name.ToStringFull));
            request.Rules.Add(new Rule_String("pawn_gender", fakePawn.gender.ToString()));

            if (fakePawn.story != null)
            {
                request.Rules.Add(new Rule_String("pawn_title", fakePawn.story.title ?? "None".Translate()));
            }

            Rand.PushState();
            Rand.Seed = descriptionSeed;
            string text = GrammarResolver.Resolve("r_statue_description", request, $"statue_desc_{fakePawn.thingIDNumber}");
            Rand.PopState();

            return text;
        }

        public override void DrawAt(Vector3 drawPos, bool flip = false)
        {
            try
            {
                // Draw base graphic first
                Vector3 loc = new Vector3(drawPos.x, drawPos.y - 0.03658537f, drawPos.z - 0.15f);
                StatueBaseGraphic.Draw(loc, flip ? parent.Rotation.Opposite : parent.Rotation, parent);

                // Ensure fake pawn is initialized and valid before drawing
                if (!Active)
                {
                    if (innerContainer.Count > 0 && innerContainer[0] is Pawn storedPawn)
                    {
                        CreateSnapshotOfPawn(storedPawn);
                        InitFakePawn();
                    }
                    else
                    {
                        return; // No pawn to draw
                    }
                }

                // Double-check that the pawn is valid for rendering
                if (!IsFakePawnValid())
                {
                    Log.Warning("Fake pawn is not valid for rendering, skipping pawn draw");
                    return;
                }

                try
                {
                    float num = fakePawn.ageTracker.CurLifeStage.headSizeFactor ?? 1f;
                    float num2 = (1f - num) * 0.5f;
                    drawPos += Vector3.back * num2;
                    drawPos -= Altitudes.AltIncVect * 0.25f;
                    drawPos.z = loc.z;

                    if (fakePawn.Drawer?.renderer != null)
                    {
                        fakePawn.Drawer.renderer.RenderPawnAt(drawPos, Rot4.South, neverAimWeapon: true);
                    }
                }
                catch (Exception renderEx)
                {
                    Log.Error($"Error rendering pawn for statue: {renderEx}");
                    // Continue without rendering the pawn - at least the base graphic is drawn
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception drawing statue with container: {ex}");
                // Fallback to base graphic only (already drawn above)
            }
        }

        public override void Notify_ColorChanged()
        {
            if (IsFakePawnValid())
            {
                try
                {
                    fakePawn.Drawer.renderer.SetStatue(parent.Stuff);
                    if (!(parent is Building { PaintColorDef: not null } building))
                    {
                        fakePawn.Drawer.renderer.SetStatuePaintColor(null);
                    }
                    else
                    {
                        fakePawn.Drawer.renderer.SetStatuePaintColor(building.PaintColorDef.color);
                    }
                    fakePawn.Drawer.renderer.SetAllGraphicsDirty();
                    fakePawn.Drawer.renderer.EnsureGraphicsInitialized();
                }
                catch (Exception ex)
                {
                    Log.Warning($"Error in Notify_ColorChanged: {ex}");
                }
            }
        }

        public override void PostExposeData()
        {
            // Save our own fields first
            Scribe_Values.Look(ref hasStoredPawn, "hasStoredPawn", false);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Defs.Look(ref bodyType, "bodyType");
            Scribe_Defs.Look(ref headType, "headType");
            Scribe_Defs.Look(ref hairDef, "hairDef");
            Scribe_Defs.Look(ref xenotype, "xenotype");
            Scribe_Defs.Look(ref beard, "beard");
            Scribe_Collections.Look(ref apparel, "apparel", LookMode.Def);
            Scribe_Collections.Look(ref apparelStyles, "apparelStyles", LookMode.Value, LookMode.Def);
            Scribe_Deep.Look(ref name, "name");
            Scribe_Values.Look(ref gender, "gender", Gender.None);
            Scribe_Values.Look(ref lifestageIndex, "lifestageIndex", 0);
            Scribe_Values.Look(ref hairColor, "hairColor");
            Scribe_Values.Look(ref descriptionSeed, "descriptionSeed", 0);
            Scribe_Collections.Look(ref nonXenotypeGenes, "nonXenotypeGenes", LookMode.Def);
            Scribe_Collections.Look(ref hediffsWhichAffectRendering, "hediffsWhichAffectRendering", LookMode.Deep);
            Scribe_Collections.Look(ref additionalSavedPawnDataForMods, "additionalSavedPawnDataForMods", LookMode.Value, LookMode.Deep);

            // Only call base PostExposeData if we don't have a stored pawn
            if (!hasStoredPawn)
            {
                base.PostExposeData();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (descriptionSeed == 0)
                {
                    descriptionSeed = Rand.Int;
                }
                if (additionalSavedPawnDataForMods == null)
                {
                    additionalSavedPawnDataForMods = new Dictionary<string, object>();
                }
                if (apparelStyles == null)
                {
                    apparelStyles = new Dictionary<int, ThingStyleDef>();
                }
                if (apparel == null)
                {
                    apparel = new List<ThingDef>();
                }

                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (hasStoredPawn && innerContainer.Count > 0 && innerContainer[0] is Pawn storedPawn)
                    {
                        CreateSnapshotOfPawn(storedPawn);
                        InitFakePawn();
                    }
                });
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (previousMap != null)
            {
                if (mode == DestroyMode.Deconstruct)
                {
                    innerContainer.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);
                }
                else
                {
                    foreach (Thing thing in innerContainer.ToList())
                    {
                        if (thing is Pawn pawn && !pawn.Dead)
                        {
                            pawn.Destroy();
                            if (pawn.Faction == Faction.OfPlayer)
                            {
                                Find.ColonistBar.MarkColonistsDirty();
                            }
                        }
                    }
                    innerContainer.ClearAndDestroyContents();
                }
            }
            else
            {
                innerContainer.ClearAndDestroyContents();
            }

            base.PostDestroy(mode, previousMap);
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();

            // Only call base if we don't have a stored pawn
            if (!hasStoredPawn)
            {
                string baseString = base.CompInspectStringExtra();
                if (!baseString.NullOrEmpty())
                {
                    sb.Append(baseString);
                }
            }
            else if (Active && name != null)
            {
                sb.Append("Subject".Translate() + ": " + name.ToString());
            }

            if (innerContainer.Count > 0)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.Append("Contains".Translate() + ": ");

                foreach (Thing thing in innerContainer)
                {
                    sb.Append(thing.LabelCap);
                    if (thing != innerContainer.Last())
                        sb.Append(", ");
                }
            }

            return sb.ToString();
        }

        public new CompProperties_StatueWithContainer Props =>
            (CompProperties_StatueWithContainer)props;
    }

    public class CompProperties_StatueWithContainer : CompProperties_Statue
    {
        public bool storePhysicalPawn = true;
        public bool showContainedPawn = true;

        public CompProperties_StatueWithContainer()
        {
            compClass = typeof(CompStatueWithContainer);
        }
    }
}