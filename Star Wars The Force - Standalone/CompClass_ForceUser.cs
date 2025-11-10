using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheForce_Standalone.Apprenticeship;
using TheForce_Standalone.Generic;
using TheForce_Standalone.PawnRenderNodes;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class CompClass_ForceUser : ThingComp
    {
        public float currentFP;
        public int forceLevel = 1;
        public HashSet<string> unlockedAbiliities = new HashSet<string>();
        public Color barColor;
        public Color textColor = Color.white;
        public float midichlorianCount = -1f;
        private bool midichloriansCalculated = false;
        public float MidichlorianCount
        {
            get
            {
                if (!midichloriansCalculated && midichlorianCount < 0 && Pawn != null)
                {
                    midichlorianCount = Rand.Value;
                    midichloriansCalculated = true;
                }
                return midichlorianCount;
            }
            set
            {
                midichlorianCount = value;
                midichloriansCalculated = true;
            }
        }

        public ForceUserAbilities Abilities { get; private set; }
        public ForceUserAlignment Alignment { get; private set; }
        public ForceUserLeveling Leveling { get; private set; }
        public ForceUserGhostMechanics GhostMechanics { get; private set; }
        public ForceUserLimbs Limbs { get; private set; }
        public Force_ApprenticeshipSystem Apprenticeship { get; private set; }

        public Thing LinkedObject => GhostMechanics.LinkedObject;

        public CompProperties_ForceUser Props => (CompProperties_ForceUser)props;
        public Pawn Pawn => parent as Pawn;
        public bool IsValidForceUser => Pawn != null && ForceSensitivityUtils.IsValidForceUser(Pawn);
        public bool isInitialized;
        public bool enableCrystalTransformation = true;

        public Dictionary<string, AbilityPreset> AbilityPresets => Abilities.abilityPresets;
        public string CurrentPreset
        {
            get => Abilities.currentPreset;
            set => Abilities.currentPreset = value;
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            try
            {
                InitializeSubsystems();
                if (Pawn != null)
                {
                    Alignment?.Initialize();
                    Leveling?.Initialize();
                    Abilities?.Initialize();
                    Apprenticeship?.Initialize();
                    SetBarColor(Pawn);
                    midichlorianCount = Rand.Value;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize ForceUser components: {ex}");
                isInitialized = false;
            }
        }

        public string wraithAnimalTexPath;
        public PawnKindDef wraithAnimalKind;
        public Vector2 wraithAnimalDrawSize = Vector2.one;
        public Color wraithAnimalColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        public static List<PawnKindDef> GetValidAnimalKinds()
        {
            return DefDatabase<PawnKindDef>.AllDefs
                .Where(k => k.race?.race?.Animal == true &&
                           k.lifeStages?.Any(ls => ls.bodyGraphicData != null) == true)
                .ToList();
        }

        public void RandomizeWraithAppearance()
        {
            var validAnimals = GetValidAnimalKinds();
            if (validAnimals.Count > 0)
            {
                var randomKind = validAnimals.RandomElement();
                wraithAnimalKind = randomKind;
                wraithAnimalTexPath = randomKind.lifeStages[0].bodyGraphicData.texPath;
                wraithAnimalDrawSize = randomKind.lifeStages[0].bodyGraphicData.drawSize;
            }
        }

        public void MarkForRedraw() => PortraitsCache.SetDirty(Pawn);

        private void InitializeSubsystems()
        {
            Abilities = new ForceUserAbilities(this);
            Alignment = new ForceUserAlignment(this);
            Leveling = new ForceUserLeveling(this);
            GhostMechanics = new ForceUserGhostMechanics(this);
            Limbs = new ForceUserLimbs(this);
            Apprenticeship = new Force_ApprenticeshipSystem(this);
        }

        public virtual void SetBarColor(Pawn pawn)
        {
            if (pawn == null)
            {
                barColor = Color.black;
                textColor = Color.white;
                return;
            }

            try
            {
                if (ModsConfig.IdeologyActive && pawn.story != null && pawn.story.favoriteColor != null)
                {
                    barColor = pawn.story.favoriteColor.color;
                    textColor = GetContrastColor(barColor);
                }
                else
                {
                    List<ColorDef> colorDefs = DefDatabase<ColorDef>.AllDefs.ToList();
                    ColorDef randomColorDef = colorDefs.RandomElement();
                    barColor = randomColorDef?.color ?? Color.white;
                    textColor = GetContrastColor(barColor);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to set bar color for pawn {pawn.Label}: {ex}");
                barColor = Color.white;
                textColor = Color.black;
            }
        }

        private Color GetContrastColor(Color backgroundColor)
        {
            float luminance = (0.299f * backgroundColor.r + 0.587f * backgroundColor.g + 0.114f * backgroundColor.b);
            return luminance > 0.5f ? Color.black : Color.white;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // Initialize subsystems first if they don't exist
            if (Abilities == null) Abilities = new ForceUserAbilities(this);
            if (Alignment == null) Alignment = new ForceUserAlignment(this);
            if (Leveling == null) Leveling = new ForceUserLeveling(this);
            if (GhostMechanics == null) GhostMechanics = new ForceUserGhostMechanics(this);
            if (Limbs == null) Limbs = new ForceUserLimbs(this);
            if (Apprenticeship == null) Apprenticeship = new Force_ApprenticeshipSystem(this);

            if (!midichloriansCalculated && midichlorianCount < 0)
            {
                MidichlorianCount = Rand.Value;
            }

            if (!IsValidForceUser) return;

            if (!isInitialized)
            {
                Leveling.Initialize();
                Abilities.Initialize();
                Alignment.Initialize();
                Apprenticeship.Initialize();
                RecalculateMaxFP();
                RecoverFP(MaxFP);

                if (wraithAnimalTexPath.NullOrEmpty())
                {
                    RandomizeWraithAppearance();
                }

                if (!respawningAfterLoad)
                {
                    Abilities.AddAbilityPoint(1);
                }

                SetBarColor(Pawn);
                isInitialized = true;
            }

            // Backwards compatibility: Migrate from hediffs to comp system
            if (respawningAfterLoad)
            {
                MigrateFromHediffs();
            }

            // Always recalculate FP on load
            RecalculateMaxFP();

            if (respawningAfterLoad)
            {
                Abilities.EnsureDefaultPreset();
                Abilities.UpdatePawnAbilities();
            }
        }

        private void MigrateFromHediffs()
        {
            if (Pawn == null) return;

            var masterHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
            if (masterHediff != null)
            {
                Apprenticeship.apprentices = masterHediff.apprentices ?? new HashSet<Pawn>();
                Apprenticeship.graduatedApprenticesCount = masterHediff.graduatedApprenticesCount;
                Apprenticeship.hasBackstoryChanged = masterHediff.hasBackstoryChanged;

                if (Apprenticeship.apprentices.Count > 0)
                {
                    foreach (Pawn apprentice in Apprenticeship.apprentices)
                    {
                        if (apprentice != null)
                        {
                            Apprenticeship.AssignApprentice(apprentice);
                            var apprenticeComp = apprentice.GetComp<CompClass_ForceUser>();
                        }
                        else
                        {
                            Log.Warning($"  - Null apprentice found during migration for master {Pawn.Label}");
                        }
                    }
                }
                else
                {
                    Log.Message($"Master {Pawn.Label} has no apprentices to migrate");
                }
                Pawn.health.RemoveHediff(masterHediff);
            }
            var apprenticeHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Apprentice) as Hediff_Apprentice;
            if (apprenticeHediff != null)
            {
                Apprenticeship.master = apprenticeHediff.master;
                Apprenticeship.ticksSinceLastXPGain = apprenticeHediff.ticksSinceLastXPGain;
                Apprenticeship.ticksSinceLastBondAttempt = apprenticeHediff.ticksSinceLastBondAttempt;
                if (Apprenticeship.master != null)
                {
                    var masterComp = Apprenticeship.master.GetComp<CompClass_ForceUser>();
                    if (masterComp?.Apprenticeship != null)
                    {
                        bool masterKnowsApprentice = masterComp.Apprenticeship.apprentices.Contains(Pawn);
                        if (!masterKnowsApprentice)
                        {
                            Log.Message($"  Adding apprentice to master's list...");
                            masterComp.Apprenticeship.apprentices.Add(Pawn);
                        }
                    }
                }
                else
                {
                    Log.Warning($"Apprentice {Pawn.Label} has null master during migration");
                }
                Pawn.health.RemoveHediff(apprenticeHediff);
            }
            VerifyRelationships();
        }

        private void VerifyRelationships()
        {
            if (Pawn == null) return;

            var forceComp = Pawn.GetComp<CompClass_ForceUser>();
            if (forceComp?.Apprenticeship == null) return;

            if (forceComp.Apprenticeship.apprentices.Count > 0)
            {
                foreach (Pawn apprentice in forceComp.Apprenticeship.apprentices)
                {
                    if (apprentice != null)
                    {
                        var apprenticeComp = apprentice.GetComp<CompClass_ForceUser>();
                        bool hasMasterRelation = apprentice.relations.DirectRelationExists(ForceDefOf.Force_MasterRelation, Pawn);
                        bool compKnowsMaster = apprenticeComp?.Apprenticeship?.master == Pawn;
                        if (!hasMasterRelation)
                        {
                            apprentice.relations.AddDirectRelation(ForceDefOf.Force_MasterRelation, Pawn);
                        }
                        if (!compKnowsMaster && apprenticeComp?.Apprenticeship != null)
                        {
                            apprenticeComp.Apprenticeship.master = Pawn;
                        }
                    }
                }
            }

            // Check apprentice relationships
            if (forceComp.Apprenticeship.master != null)
            {
                var master = forceComp.Apprenticeship.master;
                var masterComp = master.GetComp<CompClass_ForceUser>();
                bool hasApprenticeRelation = master.relations.DirectRelationExists(ForceDefOf.Force_ApprenticeRelation, Pawn);
                bool masterKnowsApprentice = masterComp?.Apprenticeship?.apprentices?.Contains(Pawn) == true;
                if (!hasApprenticeRelation)
                {
                    master.relations.AddDirectRelation(ForceDefOf.Force_ApprenticeRelation, Pawn);
                }
                if (!masterKnowsApprentice && masterComp?.Apprenticeship != null)
                {
                    masterComp.Apprenticeship.apprentices.Add(Pawn);
                }
            }
        }

        // FP Management
        private float maxFP;
        public float MaxFP
        {
            get => maxFP;
            private set => maxFP = value;
        }
        public float FPRatio => MaxFP > 0 ? currentFP / MaxFP : 0f;

        public void RecalculateMaxFP()
        {
            float baseFP = 20f + (forceLevel * 5f);
            if (Pawn != null)
            {
                float psychicSensitivity = Pawn.GetStatValue(StatDef.Named("Force_FPMax"), true, -1);
                MaxFP = baseFP * (psychicSensitivity <= 0 ? 1f : psychicSensitivity);
            }
            else
            {
                MaxFP = baseFP;
            }
            currentFP = Mathf.Min(currentFP, MaxFP);
        }

        public bool TrySpendFP(float amount)
        {
            if (!IsValidForceUser || currentFP < amount) return false;
            currentFP -= amount;
            return true;
        }

        public void RecoverFP(float amount) => currentFP = Mathf.Min(currentFP + amount, MaxFP);
        public void DrainFP(float amount) => currentFP = Mathf.Max(currentFP - amount, 0f);

        public override void CompTick()
        {
            base.CompTick();
            if (!IsValidForceUser) return;

            if (Find.TickManager.TicksGame % 60 == 0)
            {
                RecalculateMaxFP();
                if (currentFP < MaxFP && !Pawn.Drafted)
                {
                    RecoverFP(Pawn.GetStatValue(StatDef.Named("Force_FPRecovery"), true, 1));
                }
                if (currentFP < MaxFP && Pawn.Drafted)
                {
                    var combatDebuff = Pawn.GetStatValue(StatDef.Named("Force_FPRecovery")) / 2f;
                    RecoverFP(combatDebuff);
                }
            }

            // Tick apprenticeship system
            Apprenticeship?.Tick();
        }

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            Abilities.CompTickInterval(delta);
            Apprenticeship?.CompTickInterval(delta);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentFP, "currentFP");
            Scribe_Values.Look(ref maxFP, "maxFP");
            Scribe_Values.Look(ref forceLevel, "forceLevel", 1);
            Scribe_Values.Look(ref textColor, "textColor", Color.white);
            Scribe_Values.Look(ref barColor, "barColor", Color.black);
            Scribe_Collections.Look(ref unlockedAbiliities, "unlockedAbiliities", LookMode.Value);
            Scribe_Values.Look(ref wraithAnimalTexPath, "wraithAnimalTexPath");
            Scribe_Values.Look(ref wraithAnimalDrawSize, "wraithAnimalDrawSize", Vector2.one);
            Scribe_Values.Look(ref wraithAnimalColor, "wraithAnimalColor", new Color(0.2f, 0.2f, 0.2f, 1f));
            Scribe_Values.Look(ref isInitialized, "isInitialized", false);
            Scribe_Values.Look(ref enableCrystalTransformation, "enableCrystalTransformation", true);

            Alignment.ExposeData();
            Leveling.ExposeData();
            Abilities.ExposeData();
            GhostMechanics.ExposeData();
            Apprenticeship.ExposeData();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!IsValidForceUser || !Pawn.IsColonistPlayerControlled)
                yield break;

            yield return new ForceUserGizmo(this);

            foreach (var gizmo in GhostMechanics.GetGhostGizmos())
                yield return gizmo;

            foreach (var gizmo in GizmoUtils.GetForceUserGizmos(this))
                yield return gizmo;

            // Add apprenticeship gizmos
            //foreach (var gizmo in Apprenticeship.GetGizmos())
            //    yield return gizmo;

            if (enableCrystalTransformation)
            {
                if (ModsConfig.IsActive("lee.theforce.lightsaber"))
                {
                    foreach (var gizmo in Alignment.CrystalTransformation.GetTransformationGizmos())
                        yield return gizmo;
                }
            }
        }

        public override float GetStatOffset(StatDef stat)
        {
            if (stat == StatDef.Named("Force_MidichlorianCount"))
                midichlorianCount += stat.defaultBaseValue;
            return Alignment.GetStatOffset(stat);
        }

        public override float GetStatFactor(StatDef stat)
        {
            return 1f;
        }

        public List<PawnRenderNode> activeRenderNodes;

        public override List<PawnRenderNode> CompRenderNodes()
        {
            try
            {
                if (!Props.renderNodeProperties.NullOrEmpty() &&
                    parent != null &&
                    parent is Pawn pawn)
                {
                    List<PawnRenderNode> list = new List<PawnRenderNode>();
                    foreach (PawnRenderNodeProperties renderNodeProperty in Props.renderNodeProperties)
                    {
                        if (renderNodeProperty?.nodeClass != null && pawn.Drawer?.renderer?.renderTree != null)
                        {
                            try
                            {
                                PawnRenderNode node = (PawnRenderNode)Activator.CreateInstance(
                                    renderNodeProperty.nodeClass,
                                    pawn,
                                    renderNodeProperty,
                                    pawn.Drawer.renderer.renderTree
                                );
                                if (node != null)
                                {
                                    list.Add(node);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Failed to create render node: {ex}");
                            }
                        }
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in CompRenderNodes: {ex}");
            }

            return base.CompRenderNodes();
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);

            if (signal == "LinkedObjectDestroyed" && LinkedObject != null && parent is Pawn pawn)
            {
                GhostMechanics.HandleLinkedObjectDestroyed();
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (GhostMechanics != null && ForceGhostUtility.IsForceGhost(Pawn))
            {
                if (dinfo.Def != ForceDefOf.Force_Lightning)
                {
                    absorbed = true;
                    return;
                }
            }
        }
    }

    public class CompProperties_ForceUser : CompProperties
    {
        public List<PawnRenderNodeProperties> renderNodeProperties;

        public CompProperties_ForceUser()
        {
            compClass = typeof(CompClass_ForceUser);
        }
    }

    [StaticConstructorOnStartup]
    public static class InitializeForceUserComp
    {
        static InitializeForceUserComp()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race?.Humanlike ?? false)
                {
                    bool hasForceUser = def.comps?.Any(cp => cp.compClass == typeof(CompClass_ForceUser)) ?? false;
                    if (!hasForceUser)
                    {
                        CompProperties_ForceUser props = new CompProperties_ForceUser()
                        {
                            compClass = typeof(CompClass_ForceUser),
                            renderNodeProperties = GetDefaultRenderNodes()
                        };

                        def.comps.Add(props);
                        props.ResolveReferences(def);
                    }
                }
            }
        }

        private static List<PawnRenderNodeProperties> GetDefaultRenderNodes()
        {
            return new List<PawnRenderNodeProperties>
            {
                new PawnRenderNodeProperties_AlignmentCondition()
                {
                    texPath = "Things/Pawn/Humanlike/HeadAttachments/SithEyes/Male/SithEyes_Male",
                    texPathFemale = "Things/Pawn/Humanlike/HeadAttachments/SithEyes/Female/SithEyes_Female",
                    workerClass = typeof(PawnRenderNodeWorker_AlignmentConditions),
                    parentTagDef = PawnRenderNodeTagDefOf.Head,
                    anchorTag = PawnDrawUtility.AnchorTagEyeRight,
                    alignment = Alignment.AlignmentType.Darkside,
                    requiredAlignment = 100f,
                    rotDrawMode = RotDrawMode.Fresh | RotDrawMode.Rotting,
                    drawSize = new Vector2(0.2f, 0.2f),
                    side = PawnRenderNodeProperties.Side.Right,
                    drawData = (DrawData)CreateDrawDataForRightEye()
                },
                new PawnRenderNodeProperties_AlignmentCondition()
                {
                    texPath = "Things/Pawn/Humanlike/HeadAttachments/SithEyes/Male/SithEyes_Male",
                    texPathFemale = "Things/Pawn/Humanlike/HeadAttachments/SithEyes/Female/SithEyes_Female",
                    workerClass = typeof(PawnRenderNodeWorker_AlignmentConditions),
                    parentTagDef = PawnRenderNodeTagDefOf.Head,
                    anchorTag = PawnDrawUtility.AnchorTagEyeLeft,
                    alignment = Alignment.AlignmentType.Darkside,
                    requiredAlignment = 100f,
                    rotDrawMode = RotDrawMode.Fresh | RotDrawMode.Rotting,
                    drawSize = new Vector2(0.2f, 0.2f),
                    side = PawnRenderNodeProperties.Side.Left,
                    drawData = (DrawData)CreateDrawDataForLeftEye()
                }
            };
        }

        private static object CreateDrawDataForRightEye()
        {
            Type drawDataType = typeof(PawnRenderNodeProperties).Assembly.GetType("Verse.DrawData");
            Type rotationalDataType = drawDataType.GetNestedType("RotationalData");

            object defaultRotationalData = CreateRotationalData(rotationalDataType,
                rotation: null,
                layer: 54f,
                offset: new Vector3(0f, 0f, -0.25f));

            MethodInfo newWithDataMethod = drawDataType.GetMethod("NewWithData", BindingFlags.Public | BindingFlags.Static);
            if (newWithDataMethod != null)
            {
                Array rotationalDataArray = Array.CreateInstance(rotationalDataType, 1);
                rotationalDataArray.SetValue(defaultRotationalData, 0);
                return newWithDataMethod.Invoke(null, new object[] { rotationalDataArray });
            }

            return null;
        }

        private static object CreateDrawDataForLeftEye()
        {
            Type drawDataType = typeof(PawnRenderNodeProperties).Assembly.GetType("Verse.DrawData");
            Type rotationalDataType = drawDataType.GetNestedType("RotationalData");

            object defaultRotationalData = CreateRotationalData(rotationalDataType,
                rotation: null,
                layer: 54f,
                offset: new Vector3(0f, 0f, -0.25f),
                flip: true);

            object dataWestRotationalData = CreateRotationalData(rotationalDataType,
                rotation: Rot4.West,
                flip: false);

            MethodInfo newWithDataMethod = drawDataType.GetMethod("NewWithData", BindingFlags.Public | BindingFlags.Static);
            if (newWithDataMethod != null)
            {
                Array rotationalDataArray = Array.CreateInstance(rotationalDataType, 2);
                rotationalDataArray.SetValue(defaultRotationalData, 0);
                rotationalDataArray.SetValue(dataWestRotationalData, 1);
                return newWithDataMethod.Invoke(null, new object[] { rotationalDataArray });
            }

            return null;
        }

        private static object CreateRotationalData(Type rotationalDataType, Rot4? rotation = null, float? layer = null,
            Vector3? offset = null, float? rotationOffset = null, Vector2? pivot = null, bool? flip = null)
        {
            object rotationalData = Activator.CreateInstance(rotationalDataType);

            if (rotation.HasValue)
            {
                FieldInfo rotationField = rotationalDataType.GetField("rotation");
                rotationField.SetValue(rotationalData, rotation.Value);
            }

            if (layer.HasValue)
            {
                FieldInfo layerField = rotationalDataType.GetField("layer");
                layerField.SetValue(rotationalData, layer.Value);
            }

            if (offset.HasValue)
            {
                FieldInfo offsetField = rotationalDataType.GetField("offset");
                offsetField.SetValue(rotationalData, offset.Value);
            }

            if (rotationOffset.HasValue)
            {
                FieldInfo rotationOffsetField = rotationalDataType.GetField("rotationOffset");
                rotationOffsetField.SetValue(rotationalData, rotationOffset.Value);
            }

            if (pivot.HasValue)
            {
                FieldInfo pivotField = rotationalDataType.GetField("pivot");
                pivotField.SetValue(rotationalData, pivot.Value);
            }

            if (flip.HasValue)
            {
                FieldInfo flipField = rotationalDataType.GetField("flip");
                flipField.SetValue(rotationalData, flip.Value);
            }

            return rotationalData;
        }
    }
}