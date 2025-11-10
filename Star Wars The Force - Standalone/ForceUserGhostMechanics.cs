using RimWorld;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class ForceUserGhostMechanics
    {
        private readonly CompClass_ForceUser parent;
        private Thing linkedObject;
        private bool showNestedGizmos = false;

        private Color? originalSkinColor;
        private Color? originalHairColor;
        private Dictionary<Apparel, Color> originalApparelColors = new Dictionary<Apparel, Color>();

        public void Reset()
        {
            // Reset ghost mechanics if needed
            LinkedObject = null;
        }


        public Thing LinkedObject
        {
            get => linkedObject;
            set
            {
                var mapComp = parent.parent.Map?.GetComponent<ForceMapComponent>();
                if (linkedObject != null && mapComp != null)
                {
                    mapComp.UnregisterLinkedObject(linkedObject, parent.Pawn);
                }

                linkedObject = value;
                if (value != null && mapComp != null)
                {
                    mapComp.RegisterLinkedObject(value, parent.Pawn);
                }
            }
        }

        public ForceUserGhostMechanics(CompClass_ForceUser parent)
        {
            this.parent = parent;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref linkedObject, "linkedObject");
            Scribe_Values.Look(ref originalSkinColor, "originalSkinColor");
            Scribe_Values.Look(ref originalHairColor, "originalHairColor");

            // Apparel colors are not saved as they can be recreated when needed
        }

        public IEnumerable<Gizmo> GetGhostGizmos()
        {
            if (parent.Pawn.IsColonistPlayerControlled && parent.IsValidForceUser)
            {
                bool isGhost = parent.Pawn.health.hediffSet.HasHediff(ForceDefOf.Force_Ghost) ||
                                parent.Pawn.health.hediffSet.HasHediff(ForceDefOf.Force_SithGhost);
                bool hasSithZombie = parent.Pawn.health.hediffSet.HasHediff(ForceDefOf.Force_SithZombie);

                if (parent.Pawn.health.hediffSet.HasHediff(ForceDefOf.Force_Ghost) && !hasSithZombie)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Force_ReturnToFlesh".Translate(),
                        defaultDesc = "Force_ReturnToFlesh".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Abilities/Lightside/ForceGhost", true),
                        action = () =>
                        {
                            var ghost = parent.Pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Ghost);
                            if (ghost != null)
                            {
                                RestoreOriginalColors();
                                parent.Pawn.health.RemoveHediff(ghost);
                                parent.Pawn.Kill(null);
                            }
                        }
                    };
                }
                else
                {
                    if (parent.Pawn.health.hediffSet.HasHediff(ForceDefOf.Force_SithGhost) && !hasSithZombie)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "Force_ReturnToFlesh".Translate(),
                            defaultDesc = "Force_ReturnToFlesh".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Abilities/Darkside/SithGhost", true),
                            action = () =>
                            {
                                var ghost = parent.Pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_SithGhost);
                                if (ghost != null)
                                {
                                    RestoreOriginalColors();
                                    parent.Pawn.health.RemoveHediff(ghost);
                                    parent.Pawn.Kill(null);
                                    LinkedObject = null;
                                    Messages.Message($"{parent.Pawn.LabelCap} has been killed and the linked object has been cleared.", MessageTypeDefOf.NegativeEvent);
                                }
                            }
                        };
                    }

                    if (parent.Alignment.DarkSideAttunement >= 100)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "Force_LinkedObjectAction".Translate(),
                            defaultDesc = "Force_LinkedObjectActionDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Abilities/Darkside/SithGhost", true),
                            action = () => showNestedGizmos = !showNestedGizmos
                        };

                        if (showNestedGizmos)
                        {
                            if (LinkedObject == null && !parent.Pawn.Dead)
                            {
                                yield return new Command_Action
                                {
                                    defaultLabel = "Force_LinktoObject".Translate(),
                                    defaultDesc = "Force_LinktoObjectDesc".Translate(),
                                    icon = ContentFinder<Texture2D>.Get("UI/Abilities/Darkside/SithGhost", true),
                                    action = () => Find.Targeter.BeginTargeting(
                                    ForceUserGhostMechanics.GetLinkTargetingParameters(),
                                    target => parent.GhostMechanics.LinkedObject = target.Thing)
                                };
                            }

                            foreach (var gizmo in GetLinkedObjectGizmos(parent.Pawn))
                            {
                                yield return gizmo;
                            }
                        }
                    }
                }
            }
        }

        private void StoreOriginalColors()
        {
            if (parent.Pawn == null) return;

            originalSkinColor = parent.Pawn.story.SkinColor;
            originalHairColor = parent.Pawn.story.HairColor;

            originalApparelColors.Clear();
            if (parent.Pawn.apparel != null)
            {
                foreach (var apparel in parent.Pawn.apparel.WornApparel)
                {
                    originalApparelColors[apparel] = apparel.DrawColor;
                }
            }
        }

        private void RestoreOriginalColors()
        {
            if (parent.Pawn == null) return;

            if (originalSkinColor.HasValue)
            {
                parent.Pawn.story.skinColorOverride = originalSkinColor.Value;
            }

            if (originalHairColor.HasValue)
            {
                parent.Pawn.story.HairColor = originalHairColor.Value;
            }

            if (parent.Pawn.apparel != null)
            {
                foreach (var apparel in parent.Pawn.apparel.WornApparel)
                {
                    if (originalApparelColors.TryGetValue(apparel, out var originalColor))
                    {
                        apparel.SetColor(originalColor);
                    }
                }
            }
        }

        private IEnumerable<Gizmo> GetLinkedObjectGizmos(Pawn pawn)
        {
            var mapComp = pawn.Map?.GetComponent<ForceMapComponent>();
            if (mapComp == null || linkedObject == null)
                yield break;

            int linkedPawnsCount = mapComp.GetPawnsLinkedTo(linkedObject).Count();
            yield return new Command_Action
            {
                defaultLabel = "Force_LinkedTo".Translate(LinkedObject.LabelCap),
                defaultDesc = "Force_LinkedToDesc".Translate(),
                icon = GetIconFor(linkedObject.def, linkedObject.Stuff),
                action = () => CameraJumper.TryJumpAndSelect(linkedObject)
            };

            if (!pawn.health.hediffSet.HasHediff(ForceDefOf.Force_SithGhost))
            {
                yield return new Command_Action
                {
                    defaultLabel = "Force_Unlink".Translate(),
                    defaultDesc = "Force_UnlinkDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate", true),
                    action = () =>
                    {
                        mapComp.UnregisterLinkedObject(LinkedObject, pawn);
                        LinkedObject = null;
                        Messages.Message("Force_UnlinkMessage".Translate(), MessageTypeDefOf.PositiveEvent);
                    }
                };
            }
        }

        public static TargetingParameters GetLinkTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetPawns = false,
                canTargetBuildings = true,
                canTargetItems = true,
                canTargetLocations = false,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = target => target.HasThing && IsValidLinkTarget(target.Thing)
            };
        }

        public static bool IsValidLinkTarget(Thing thing)
        {
            return thing != null &&
                   thing.stackCount == 1 &&
                   (thing.def.category == ThingCategory.Building ||
                    thing.def.IsApparel ||
                    thing.def.IsWeapon);
        }

        public static Texture2D GetIconFor(ThingDef thingDef, ThingDef stuffDef = null, ThingStyleDef thingStyleDef = null, int? graphicIndexOverride = null)
        {
            Texture2D result = thingDef.GetUIIconForStuff(stuffDef);

            if (thingStyleDef != null && thingStyleDef.UIIcon != null)
            {
                result = (!graphicIndexOverride.HasValue) ? thingStyleDef.UIIcon : thingStyleDef.IconForIndex(graphicIndexOverride.Value);
            }
            else if (thingDef.graphic is Graphic_Appearances graphic_Appearances)
            {
                result = (Texture2D)graphic_Appearances.SubGraphicFor(stuffDef ?? GenStuff.DefaultStuffFor(thingDef)).MatAt(thingDef.defaultPlacingRot).mainTexture;
            }

            return result;
        }

        public void HandleLinkedObjectDestroyed()
        {
            if (parent.Pawn.health.hediffSet.HasHediff(ForceDefOf.Force_SithGhost))
            {
                var ghostHediff = parent.Pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_SithGhost);
                if (ghostHediff != null)
                {
                    RestoreOriginalColors();
                    parent.Pawn.health.RemoveHediff(ghostHediff);
                    parent.Pawn.Kill(null);
                }
            }
            LinkedObject = null;
        }

        public bool ShouldBecomeGhostOnDeath()
        {
            if (parent?.Pawn == null) return false;
            bool hasHighDarkSide = parent.Alignment.DarkSideAttunement >= 100;
            bool hasHighLightSide = parent.Alignment.LightSideAttunement >= 100;

            return hasHighDarkSide || hasHighLightSide;
        }

        public bool TryReturnAsGhost(HediffDef ghostHediffDef, float severity)
        {
            if (parent.Pawn == null || parent.Pawn.health == null || parent.Pawn.health.hediffSet.HasHediff(ghostHediffDef))
                return false;
            StoreOriginalColors();

            if (parent.Pawn.Dead)
            {
                ResurrectionUtility.TryResurrect(parent.Pawn);
            }
            Hediff ghostHediff = HediffMaker.MakeHediff(ghostHediffDef, parent.Pawn);
            ghostHediff.Severity = severity;
            parent.Pawn.health.AddHediff(ghostHediff);
            FleckMaker.ThrowLightningGlow(parent.Pawn.DrawPos, parent.Pawn.Map, 2f);
            Messages.Message("MessagePawnReturnedAsGhost".Translate(parent.Pawn.LabelShort),
            parent.Pawn, MessageTypeDefOf.PositiveEvent);

            if (ghostHediff != null)
            {
                Color ghostColor = ForceGhostUtility.GetGhostColor(parent.Pawn) ?? Color.white;

                if (parent.Pawn.apparel != null)
                {
                    foreach (Apparel apparel in parent.Pawn.apparel.WornApparel)
                    {
                        apparel.SetColor(ghostColor);
                    }
                }
            }

            return true;
        }
    }
}