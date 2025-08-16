using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    public class Dialog_SelectAnimalAppearance : Window
    {
        private readonly Comp_RandomAnimalAppearance comp;
        private Vector2 scrollPosition;
        private string searchText = "";
        private Dictionary<BodyDef, List<PawnKindDef>> bodyDefCategories;

        public Dialog_SelectAnimalAppearance(Comp_RandomAnimalAppearance comp)
        {
            this.comp = comp;
            doCloseButton = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            bodyDefCategories = CategorizeByBodyDef();
        }

        public override Vector2 InitialSize => new Vector2(800f, 600f);

        private Dictionary<BodyDef, List<PawnKindDef>> CategorizeByBodyDef()
        {
            var categories = new Dictionary<BodyDef, List<PawnKindDef>>();

            foreach (var kind in Comp_RandomAnimalAppearance.GetValidAnimalKinds())
            {
                var bodyDef = kind.race.race.body;
                if (bodyDef == null) continue;

                if (!categories.ContainsKey(bodyDef))
                    categories[bodyDef] = new List<PawnKindDef>();

                categories[bodyDef].Add(kind);
            }

            return categories.OrderBy(x => x.Key.defName).ToDictionary(x => x.Key, x => x.Value);
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Window title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Force.SithSorcery_SelectAnimalTitle".Translate());
            Text.Font = GameFont.Small;

            // Search box
            Rect searchRect = new Rect(0f, 40f, inRect.width, 30f);
            searchText = Widgets.TextField(searchRect, searchText);
            TooltipHandler.TipRegion(searchRect, "Force.SithSorcery_SearchTooltip".Translate());

            // Scroll view
            Rect outRect = new Rect(0f, 75f, inRect.width, inRect.height - 100f);
            float totalHeight = bodyDefCategories.Sum(x =>
                x.Value.Count(k => MatchesSearch(k)) > 0 ?
                (x.Value.Count(k => MatchesSearch(k)) * 35f) + 25f : 0f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, totalHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float yPos = 0f;
            foreach (var category in bodyDefCategories)
            {
                var matchingKinds = category.Value.Where(k => MatchesSearch(k)).ToList();
                if (matchingKinds.Count == 0) continue;

                // Category header
                Rect headerRect = new Rect(0f, yPos, viewRect.width, 25f);
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                Widgets.DrawLineHorizontal(0f, yPos, viewRect.width);
                GUI.color = Color.white;

                Rect labelRect = new Rect(30f, yPos, viewRect.width - 30f, 25f);
                Widgets.Label(labelRect, category.Key.LabelCap);
                yPos += 25f;

                // Animal options
                foreach (var kind in matchingKinds)
                {
                    Rect rowRect = new Rect(0f, yPos, viewRect.width, 30f);
                    DrawAnimalOption(rowRect, kind, ref yPos);
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawAnimalOption(Rect rect, PawnKindDef kind, ref float yPos)
        {
            // Animal preview icon
            Rect previewRect = new Rect(5f, rect.y + 2f, 26f, 26f);
            Texture2D previewTex = ContentFinder<Texture2D>.Get(kind.lifeStages[0].bodyGraphicData.texPath + "_north") ??
                                  ContentFinder<Texture2D>.Get(kind.lifeStages[0].bodyGraphicData.texPath);
            if (previewTex != null)
            {
                GUI.color = comp.animalColor;
                GUI.DrawTexture(previewRect, previewTex);
                GUI.color = Color.white;
            }

            // Selectable label
            Rect selectRect = new Rect(35f, rect.y, rect.width - 35f, 30f);
            if (Widgets.ButtonInvisible(selectRect))
            {
                comp.SetAppearance(kind);
                SoundDefOf.Click.PlayOneShotOnCamera();
                Close();
                Messages.Message("Force.SithSorcery_AnimalSelected".Translate(kind.LabelCap),
                              MessageTypeDefOf.PositiveEvent);
            }

            // Highlight on hover
            if (Mouse.IsOver(selectRect))
            {
                Widgets.DrawHighlight(selectRect);
                TooltipHandler.TipRegion(selectRect, "Force.SithSorcery_SelectTooltip".Translate());
            }

            Widgets.Label(selectRect, kind.LabelCap);
            yPos += 35f;
        }

        private bool MatchesSearch(PawnKindDef kind)
        {
            return searchText.NullOrEmpty() ||
                   kind.LabelCap.ToString().ToLower().Contains(searchText.ToLower()) ||
                   kind.race.race.body.defName.ToLower().Contains(searchText.ToLower());
        }
    }
}