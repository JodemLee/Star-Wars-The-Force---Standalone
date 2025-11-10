using HarmonyLib;
using Mono.Security;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheForce_Standalone.Apprenticeship;
using TheForce_Standalone.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Misc
{
    public static class ForceDyadUtility
    {
        public static void TryCreateForceDyadPairs(List<Pawn> pawns, Faction faction)
        {
            if (pawns.Count == 0)
            {
                return;
            }

            // Find pawns with the dyad backstory
            var dyadPawns = pawns.Where(p =>
                (p.story?.Childhood?.defName == "Force_DyadTwin" ||
                 p.story?.Adulthood?.defName == "Force_DyadTwin"))
                .ToList();

            if (dyadPawns.Count == 0)
            {
                return;
            }

            int pairsCreated = 0;
            var pawnsToAdd = new List<Pawn>();

            foreach (var dyadPawn in dyadPawns)
            {
                // Skip if this pawn already has a sibling in the group
                if (HasSiblingInGroup(dyadPawn, pawns))
                {
                    continue;
                }

                Log.Message($"[ForceDyad] Creating twin for {dyadPawn.Name}");

                // Create a new twin pawn
                Pawn twin = CreateDyadTwin(dyadPawn, faction);
                if (twin != null)
                {
                    pawnsToAdd.Add(twin);
                    pairsCreated++;

                    Log.Message($"[ForceDyad] Successfully created twin: {twin.Name} for {dyadPawn.Name}");
                }
            }

            // Add all created twins to the group
            if (pawnsToAdd.Count > 0)
            {
                pawns.AddRange(pawnsToAdd);
            }

            Log.Message($"[ForceDyad] Successfully created {pairsCreated} dyad pairs");
        }

        private static bool HasSiblingInGroup(Pawn pawn, List<Pawn> group)
        {
            // Check if any pawn in the group shares a mother with this pawn
            return group.Any(p => p != pawn && AreSiblings(pawn, p));
        }

        private static bool AreSiblings(Pawn pawn1, Pawn pawn2)
        {
            // Use ParentRelationUtility to check if they share the same mother
            return ParentRelationUtility.HasSameMother(pawn1, pawn2);
        }

        private static Pawn CreateDyadTwin(Pawn original, Faction faction)
        {
            try
            {
                // Use PawnCloningUtility to create an exact duplicate
                Pawn twin = PawnCloningUtility.Duplicate(original);
                PawnCloningUtility.CopyApparel(original, twin);
                PawnCloningUtility.CopyEquipment(original, twin);
                twin.story.birthLastName = original.story.birthLastName;
                twin.Name = PawnBioAndNameGenerator.GeneratePawnName(twin);

                // Copy the mother relationship to make them siblings
                Pawn mother = ParentRelationUtility.GetMother(original);
                if (mother != null)
                {
                    ParentRelationUtility.SetMother(twin, mother);
                }

                return twin;
            }
            catch (Exception ex)
            {
                Log.Error($"[ForceDyad] Error creating twin for {original.Name}: {ex}");
                return null;
            }
        }
    }

    [HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawns))]
    public static class Patch_GeneratePawns_Dyad
    {
        [HarmonyPostfix]
        public static void PostfixGeneratePawns(PawnGroupMakerParms parms, ref IEnumerable<Pawn> __result)
        {
            // Use the same condition as master-apprentice system
            if (parms.faction?.def.GetModExtension<FactionExtension_ForceUsers>()?.enableMasterApprenticeSystem == true)
            {
                var pawnListForDyad = __result.ToList();

                ForceDyadUtility.TryCreateForceDyadPairs(pawnListForDyad, parms.faction);

                __result = pawnListForDyad;
            }
        }
    }
}