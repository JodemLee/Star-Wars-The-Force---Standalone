using RimWorld;
using System;
using System.Text.RegularExpressions;
using TheForce_Standalone.Apprenticeship;
using TheForce_Standalone.Dialogs;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    [StaticConstructorOnStartup]
    public class ITab_Pawn_Alignment : ITab
    {
        private Vector2 thoughtScrollPosition;
        public static readonly Vector3 PawnTextureCameraOffset = default;
        private bool showHat;
        private readonly Rot4 rot = new Rot4(2);
        public const float pawnPanelSize = 128f;
        private const float padding = 10f;
        private const float sectionSpacing = 8f;

        private Pawn PawnToShowInfoAbout
        {
            get
            {
                if (base.SelPawn != null) return base.SelPawn;
                if (base.SelThing is Corpse corpse) return corpse.InnerPawn;
                return null;
            }
        }

        public override bool IsVisible
        {
            get
            {
                if (base.SelPawn == null) return false;
                var forceComp = base.SelPawn.GetComp<CompClass_ForceUser>();
                return forceComp != null && forceComp.IsValidForceUser;
            }
        }


        public ITab_Pawn_Alignment()
        {
            labelKey = "Force.Alignment.TabLabel";
            tutorTag = "Force.Alignment".Translate();
            size = new Vector2(320f, 400f);
        }

        static ITab_Pawn_Alignment()
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                foreach (var def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.race?.Humanlike ?? false)
                    {
                        // Check if tab already exists to avoid duplicates
                        if (def.inspectorTabsResolved?.Any(t => t is ITab_Pawn_Alignment) != true)
                        {
                            def.inspectorTabs?.Add(typeof(ITab_Pawn_Alignment));
                            def.inspectorTabsResolved?.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Alignment)));
                        }
                    }
                }
            });
        }

        protected override void FillTab()
        {
            try
            {
                Rect tabRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(padding);
                Pawn pawn = PawnToShowInfoAbout;
                if (pawn.story == null) return;

                GUI.BeginGroup(tabRect);
                try
                {
                    float curY = 0f;

                    Rect headerRect = new(0f, curY, tabRect.width, 30f);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Medium;
                    Widgets.Label(headerRect, "Force.Alignment.Header".Translate());
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                    curY += headerRect.height + sectionSpacing;

                    Rect contentRect = new(0f, curY, tabRect.width, tabRect.height - curY - 35f);

                    Rect rowRect = new(0f, curY, contentRect.width, pawnPanelSize);
                    GUI.BeginGroup(rowRect);
                    {
                        Rect pawnRect = new(0f, 0f, pawnPanelSize, pawnPanelSize);
                        DrawColonist(pawnRect, pawn);

                        Rect statsRect = new(
                            pawnRect.xMax + padding,
                            0f,
                            rowRect.width - pawnRect.width - padding,
                            pawnPanelSize
                        );
                        ForceAlignmentUtility.DrawAlignmentStats(statsRect, pawn);
                    }
                    GUI.EndGroup();
                    curY += rowRect.height + sectionSpacing;

                    float infoBoxHeight = 80f;


                    if (ForceSensitivityUtils.IsMaster(pawn))
                    {
                        Rect masterRect = new(
                            0f,
                            curY,
                            contentRect.width,
                            infoBoxHeight
                        );
                        ForceAlignmentUtility.DrawMasterInfo(masterRect, pawn);
                        curY += masterRect.height + sectionSpacing;
                    }

                    if (ForceSensitivityUtils.IsApprentice(pawn))
                    {
                        Rect apprenticeRect = new(
                            0f,
                            curY,
                            contentRect.width,
                            infoBoxHeight
                        );
                        ForceAlignmentUtility.DrawApprenticeInfo(apprenticeRect, pawn);
                        curY += apprenticeRect.height + sectionSpacing;
                    }
                }
                finally
                {
                    GUI.EndGroup();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Force.Error.AlignmentTabError".Translate(ex.ToString()));
            }
        }

        private void DrawColonist(Rect rect, Pawn pawn)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(rect, PortraitsCache.Get(
                pawn,
                rect.size,
                rot,
                PawnTextureCameraOffset,
                1.1f
            ));

            GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.4f);
            Widgets.DrawBox(rect, 2);
            GUI.color = Color.white;
        }
    }
}