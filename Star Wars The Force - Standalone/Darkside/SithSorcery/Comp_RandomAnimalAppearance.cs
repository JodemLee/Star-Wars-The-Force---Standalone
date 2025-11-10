using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    public class Comp_RandomAnimalAppearance : ThingComp
    {
        public string cachedAnimalTexPath;
        public Vector2 cachedDrawSize = Vector2.one;
        public Color animalColor = new Color(0.4f, 0.4f, 0.4f, .7f);

        public Graphic CachedAnimalGraphic { get; private set; }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!respawningAfterLoad && CachedAnimalGraphic == null)
            {
                RandomizeAppearance();
            }
        }

        public void RandomizeAppearance()
        {
            var animalKinds = GetValidAnimalKinds();
            if (animalKinds.Count > 0)
            {
                SetAppearance(animalKinds.RandomElement());
            }
        }

        public void SetAppearance(PawnKindDef kindDef)
        {
            var graphicData = kindDef.lifeStages[0].bodyGraphicData;
            cachedAnimalTexPath = graphicData.texPath;
            cachedDrawSize = graphicData.drawSize;
            UpdateGraphic();

            if (parent is Pawn pawn)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
                PortraitsCache.SetDirty(pawn);
            }
        }

        private void UpdateGraphic()
        {
            if (!cachedAnimalTexPath.NullOrEmpty())
            {
                CachedAnimalGraphic = GraphicDatabase.Get<Graphic_Multi>(
                    cachedAnimalTexPath,
                    ShaderDatabase.CutoutSkin,
                    cachedDrawSize,
                    animalColor,
                    animalColor
                );
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref cachedAnimalTexPath, "animalTexPath");
            Scribe_Values.Look(ref cachedDrawSize, "animalDrawSize", Vector2.one);
            Scribe_Values.Look(ref animalColor, "animalColor", new Color(0.2f, 0.2f, 0.2f, 1f));

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (UnityData.IsInMainThread)
                {
                    UpdateGraphic();
                }
                else
                {
                    LongEventHandler.QueueLongEvent(
                        () => UpdateGraphic(),
                        "Updating animal appearance",
                        false,
                        null
                    );
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Change Appearance",
                    defaultDesc = "Select a new animal form for this wraith",
                    icon = ContentFinder<Texture2D>.Get("UI/Misc/BadTexture"),
                    action = () => Find.WindowStack.Add(new Dialog_SelectAnimalAppearance(this))
                };
            }

            foreach (var g in base.CompGetGizmosExtra()) yield return g;
        }

        public static List<PawnKindDef> GetValidAnimalKinds()
        {
            return DefDatabase<PawnKindDef>.AllDefs
                .Where(k => !k.RaceProps.IsAnomalyEntity &&
                           k.lifeStages?.Any(ls => ls.bodyGraphicData != null) == true)
                .ToList();
        }
    }

    public class CompProperties_RandomAnimalAppearance : CompProperties
    {
        public CompProperties_RandomAnimalAppearance()
        {
            compClass = typeof(Comp_RandomAnimalAppearance);
        }
    }
}
