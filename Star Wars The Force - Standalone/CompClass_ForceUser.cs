using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class CompClass_ForceUser : ThingComp
    {
        public float currentFP;
        public int forceLevel = 0;
        public HashSet<string> unlockedAbiliities = new HashSet<string>();


        // Subsystem references
        public ForceUserAbilities Abilities { get; private set; }
        public ForceUserAlignment Alignment { get; private set; }
        public ForceUserLeveling Leveling { get; private set; }
        public ForceUserGhostMechanics GhostMechanics { get; private set; }
        public ForceUserLimbs Limbs { get; private set; }

        public Thing LinkedObject => GhostMechanics.LinkedObject;

        public CompProperties_ForceUser Props => (CompProperties_ForceUser)props;
        public Pawn Pawn => parent as Pawn;
        public bool IsValidForceUser => Pawn != null && ForceSensitivityUtils.IsValidForceUser(Pawn);

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            InitializeSubsystems();
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
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!IsValidForceUser) return;
            if (!respawningAfterLoad)
            {
                Leveling.Initialize();

                Abilities.Initialize();
                Alignment.Initialize();

                if (wraithAnimalTexPath.NullOrEmpty())
                {
                    RandomizeWraithAppearance();
                }
            }
            RecalculateMaxFP();
            RecoverFP(MaxFP);
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
            }

        }

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            Abilities.CompTickInterval(delta);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentFP, "currentFP");
            Scribe_Values.Look(ref maxFP, "maxFP");
            Scribe_Values.Look(ref forceLevel, "forceLevel", 1);
            Scribe_Collections.Look(ref unlockedAbiliities, "unlockedAbiliities", LookMode.Value);
            Scribe_Values.Look(ref wraithAnimalTexPath, "wraithAnimalTexPath");
            Scribe_Values.Look(ref wraithAnimalDrawSize, "wraithAnimalDrawSize", Vector2.one);
            Scribe_Values.Look(ref wraithAnimalColor, "wraithAnimalColor", new Color(0.2f, 0.2f, 0.2f, 1f));

            // Subsystem data
            Alignment.ExposeData();
            Leveling.ExposeData();
            Abilities.ExposeData();
            GhostMechanics.ExposeData();
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
        }

        public int AvailableAbilityPoints => Abilities.AvailableAbilityPoints;
        public void AddAbilityPoint(int amount = 1) => Abilities.AddAbilityPoint(amount);

        public override float GetStatOffset(StatDef stat)
        {
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


   




    public class DevActions_ForceUser
    {
        [DebugAction("The Force", "Force Level Up", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void forceLevelUp()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
            {
                CompClass_ForceUser forceUser = pawn.GetComp<CompClass_ForceUser>();
                if (forceUser != null)
                {
                    list.Add(new FloatMenuOption(pawn.LabelShortCap, () =>
                    {
                        forceUser.Leveling.LevelUp(1);
                        Messages.Message($"Forced level up for {pawn.LabelShortCap} to level {forceUser.forceLevel}", MessageTypeDefOf.PositiveEvent);
                    }));
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        [DebugAction("The Force", "Add Ability Point", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddAbilityPoint()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
            {
                CompClass_ForceUser forceUser = pawn.GetComp<CompClass_ForceUser>();
                if (forceUser != null)
                {
                    list.Add(new FloatMenuOption(pawn.LabelShortCap, () =>
                    {
                        forceUser.AddAbilityPoint(1);
                        Messages.Message($"Added ability point to {pawn.LabelShortCap} (Total: {forceUser.AvailableAbilityPoints})", MessageTypeDefOf.PositiveEvent);
                    }));
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        [DebugAction("The Force", "Check FP Status", allowedGameStates = AllowedGameStates.Playing)]
        private static void CheckFPStatus()
        {
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(Options()));
        }

        private static List<DebugMenuOption> Options()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawns)
            {
                var forceUser = pawn.TryGetComp<CompClass_ForceUser>();
                if (forceUser != null)
                {
                    list.Add(new DebugMenuOption(pawn.LabelShortCap, DebugMenuOptionMode.Action, () =>
                    {
                        Messages.Message($"{pawn.LabelShortCap} FP: {forceUser.currentFP}/{forceUser.MaxFP}", MessageTypeDefOf.NeutralEvent);
                    }));
                }
            }
            return list;
        }
    }
}