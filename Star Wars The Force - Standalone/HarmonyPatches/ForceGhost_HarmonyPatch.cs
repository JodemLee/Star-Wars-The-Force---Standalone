using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheForce_Standalone.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.HarmonyPatches
{
    [StaticConstructorOnStartup]
    public static class ForceGhost_HarmonyPatch
    {
        [HarmonyPatch(typeof(HealthCardUtility), "DrawMedOperationsTab")]
        public static class PatchDrawMedOperationsTab
        {
            public static bool Prefix(Pawn pawn)
            {
                return !ForceGhostUtility.IsForceGhost(pawn);
            }
        }

        [HarmonyPatch(typeof(PawnDiedOrDownedThoughtsUtility), "TryGiveThoughts")]
        [HarmonyPatch(new Type[] { typeof(Pawn), typeof(DamageInfo?), typeof(PawnDiedOrDownedThoughtsKind) })]
        public static class TryGiveThoughts_Patch
        {
            public static bool Prefix(Pawn victim)
            {
                return !ForceGhostUtility.IsForceGhost(victim);
            }
        }

        [HarmonyPatch(typeof(CompAbilityEffect_BloodfeederBite), "Valid")]
        public static class CompAbilityEffect_BloodfeederBitePostfixPatch
        {
            public static void Postfix(ref bool __result, LocalTargetInfo target, bool throwMessages)
            {
                Pawn pawn = target.Pawn;
                if (pawn != null && ForceGhostUtility.IsForceGhost(pawn))
                {
                    if (throwMessages)
                    {
                        Messages.Message("Force.MessageCannotFeedOnGhost".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                    }
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(TradeDeal), "TryExecute", new[] { typeof(bool) }, new[] { ArgumentType.Out })]
        public static class TradeDeal_TryExecute_Warning_Patch
        {
            public static bool Prefix(List<Tradeable> ___tradeables)
            {
                bool LinkedObjectFound = ___tradeables.Any(tradeable => IsLinkedObject(tradeable.ThingDef));

                if (LinkedObjectFound)
                {
                    Messages.Message("Force.WarningLinkedObjectsInTrade".Translate(), MessageTypeDefOf.RejectInput);
                }
                return true;
            }

            private static bool IsLinkedObject(ThingDef def)
            {
                foreach (var map in Find.Maps)
                {
                    var forceMapComp = map.GetComponent<ForceMapComponent>();
                    if (forceMapComp == null) continue;

                    foreach (var linkedThing in forceMapComp.GetAllLinkedObjects())
                    {
                        if (linkedThing.def == def)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.GetInspectString))]
        public static class Patch_Thing_GetInspectString
        {
            private static readonly Dictionary<Thing, string> cachedResults = new Dictionary<Thing, string>();
            private static int lastCachedTick = -1;

            public static void Postfix(Thing __instance, ref string __result)
            {
                if (__instance == null || __instance.Destroyed || Find.CurrentMap == null)
                    return;

                int currentTick = Find.TickManager.TicksGame;
                if (lastCachedTick == currentTick && cachedResults.TryGetValue(__instance, out var cachedResult))
                {
                    __result = cachedResult;
                    return;
                }

                if (lastCachedTick != currentTick)
                {
                    cachedResults.Clear();
                    lastCachedTick = currentTick;
                }

                var forceMapComp = Find.CurrentMap.GetComponent<ForceMapComponent>();
                if (forceMapComp == null || !forceMapComp.IsObjectLinked(__instance))
                {
                    cachedResults[__instance] = __result;
                    return;
                }

                var stringBuilder = new StringBuilder(__result);
                bool firstEntry = true;

                foreach (var pawn in forceMapComp.GetPawnsLinkedTo(__instance))
                {
                    if (pawn == null || pawn.Destroyed)
                        continue;

                    if (!firstEntry)
                    {
                        stringBuilder.AppendLine();
                    }
                    else if (stringBuilder.Length > 0 && !stringBuilder.ToString().EndsWith("\n"))
                    {
                        stringBuilder.AppendLine();
                    }

                    stringBuilder.Append("Force.SithGhostLabel".Translate(pawn.LabelCap));
                    firstEntry = false;
                }

                __result = stringBuilder.ToString();
                cachedResults[__instance] = __result;
            }
        }
    }

    [HarmonyPatch(typeof(Corpse), nameof(Corpse.GetGizmos))]
    public static class Patch_Corpse_GetGizmos
    {
        static void Postfix(Corpse __instance, ref IEnumerable<Gizmo> __result)
        {
            try
            {
                var gizmos = __result?.ToList() ?? new List<Gizmo>();
                var pawn = __instance.InnerPawn;
                if (pawn == null) return;
                if (pawn.Faction != Faction.OfPlayer) return;
                var forceUser = pawn.GetComp<CompClass_ForceUser>();
                if (forceUser == null) return;
                if (!forceUser.IsValidForceUser) return;
                if (forceUser.Alignment?.LightSideAttunement >= 100)
                {
                    gizmos.Add(new Command_Action
                    {
                        defaultLabel = "Force.ReturnAsForceGhost".Translate(),
                        defaultDesc = "Force.ReturnAsForceGhostDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Abilities/Lightside/ForceGhost", false),
                        action = () =>
                        {
                            forceUser.GhostMechanics?.TryReturnAsGhost(ForceDefOf.Force_Ghost, 1f);
                            pawn.apparel?.LockAll();
                        }
                    });
                }
                else if (forceUser.Alignment?.DarkSideAttunement >= 100)
                {
                    if (forceUser.LinkedObject != null)
                    {
                        gizmos.Add(new Command_Action
                        {
                            defaultLabel = "Force.ReturnAsSithGhost".Translate(),
                            defaultDesc = "Force.ReturnAsSithGhostDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Abilities/Darkside/SithGhost", false),
                            action = () =>
                            {
                                forceUser.GhostMechanics?.TryReturnAsGhost(ForceDefOf.Force_SithGhost, 1f);
                                pawn.apparel?.LockAll();
                            }
                        });

                        gizmos.Add(new Command_Action
                        {
                            defaultLabel = "Force.FocusOnPhylactery".Translate(),
                            defaultDesc = "Force.FocusOnPhylacteryDesc".Translate(),
                            icon = ForceUserGhostMechanics.GetIconFor(forceUser.LinkedObject.def, forceUser.LinkedObject.Stuff),
                            action = () => CameraJumper.TryJumpAndSelect(forceUser.LinkedObject)
                        });
                    }
                }

                __result = gizmos;
            }
            catch (Exception ex)
            {
                Log.Error("Force.ErrorInCorpseGizmoPatch".Translate(ex.ToString()));
            }
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.Destroy))]
    public static class Thing_Destroy_Patch
    {
        static void Postfix(Thing __instance, DestroyMode mode)
        {
            try
            {
                if (mode == DestroyMode.Vanish || __instance == null)
                    return;

                var allPawns = new List<Pawn>();
                foreach (var map in Find.Maps)
                {
                    if (map?.mapPawns?.AllPawns != null)
                        allPawns.AddRange(map.mapPawns.AllPawns);
                }

                foreach (var pawn in allPawns)
                {
                    var forceUser = pawn?.GetComp<CompClass_ForceUser>();
                    if (forceUser != null && forceUser.LinkedObject == __instance)
                    {
                        forceUser.ReceiveCompSignal("LinkedObjectDestroyed");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Force.ErrorInThingDestroyPatch".Translate(ex.ToString()));
            }
        }
    }
}