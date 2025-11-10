using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Darkside.SithSorcery.Alchemy
{
    public class ITab_Alchemy : ITab
    {
        private const int ApparatusSlotIndex = 0;
        private const int IngredientSlotCount = 4;
        private const int TotalSlots = 5;

        private Thing[] slots = new Thing[TotalSlots];
        private readonly Rect[] slotRects = new Rect[TotalSlots];
        private readonly int[] ingredientCounts = new int[TotalSlots];
        private readonly List<RecipeDef> availableRecipes = new List<RecipeDef>();

        private static readonly Vector2 TabSize = new Vector2(400f, 500f);
        private Vector2 scrollPosition = Vector2.zero;
        private string statusMessage = "";
        private MessageTypeDef statusMessageType = MessageTypeDefOf.NeutralEvent;

        // NEW: Reference to the autonomous station
        private Building_AlchemyStationAutonomous AlchemyStation => SelThing as Building_AlchemyStationAutonomous;

        public ITab_Alchemy()
        {
            labelKey = "TabAlchemy";
            tutorTag = "Alchemy";
        }

        protected override void UpdateSize()
        {
            size = TabSize;
        }

        protected override void FillTab()
        {
            Rect canvas = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new(canvas.x, canvas.y, canvas.width, 30f), "Alchemy Station");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // NEW: Show current bill status
            if (AlchemyStation?.ActiveBill != null)
            {
                var bill = AlchemyStation.ActiveBill;
                Rect statusRect = new(canvas.x, canvas.y + 35f, canvas.width, 25f);
                GUI.color = bill.State == AlchemyFormingState.Forming ? Color.yellow :
                           bill.State == AlchemyFormingState.Completed ? Color.green : Color.white;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(statusRect, GetBillStatusText(bill));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            float centerX = canvas.x + canvas.width / 2f;
            float centerY = canvas.y + canvas.height / 2f - 30f;
            float slotSize = 70f;
            float spacing = 90f;

            Rect apparatusRect = new(centerX - slotSize / 2f, centerY - slotSize / 2f, slotSize, slotSize);
            DrawSlot(apparatusRect, ApparatusSlotIndex, "Apparatus (Optional)");

            DrawSlot(new(centerX - slotSize / 2f, centerY - slotSize / 2f - spacing, slotSize, slotSize), 1, "Ingredient");
            DrawSlot(new(centerX + spacing - slotSize / 2f, centerY - slotSize / 2f, slotSize, slotSize), 2, "Ingredient");
            DrawSlot(new(centerX - slotSize / 2f, centerY + spacing - slotSize / 2f, slotSize, slotSize), 3, "Ingredient");
            DrawSlot(new(centerX - spacing - slotSize / 2f, centerY - slotSize / 2f, slotSize, slotSize), 4, "Ingredient");

            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = Color.gray;
            Widgets.Label(new(canvas.x, centerY - spacing - 85f, canvas.width, 20f), "Click slots to add items");
            Widgets.Label(new(canvas.x, centerY - spacing - 65f, canvas.width, 20f), "Right-click to remove");
            Widgets.Label(new(canvas.x, centerY - spacing - 45f, canvas.width, 20f), "Shift/Control click to adjust count");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            if (!statusMessage.NullOrEmpty())
            {
                GUI.color = statusMessageType == MessageTypeDefOf.PositiveEvent ? Color.green :
                            statusMessageType == MessageTypeDefOf.RejectInput ? Color.red : Color.yellow;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new(canvas.x, canvas.y + 60f, canvas.width, 30f), statusMessage);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            Rect craftButtonRect = new(centerX - 75f, canvas.y + canvas.height - 40f, 150f, 30f);

            // UPDATED: Button text and behavior based on current state
            string buttonText = "Brew Potion";
            bool buttonEnabled = true;

            if (AlchemyStation?.ActiveBill != null)
            {
                switch (AlchemyStation.ActiveBill.State)
                {
                    case AlchemyFormingState.Forming:
                        buttonText = $"Brewing... ({AlchemyStation.CurrentBillFormingPercent.ToStringPercent()})";
                        buttonEnabled = false;
                        break;
                    case AlchemyFormingState.Completed:
                        buttonText = "Collect Potion";
                        break;
                    case AlchemyFormingState.Waiting:
                        buttonText = "Start Brewing";
                        break;
                }
            }

            if (Widgets.ButtonText(craftButtonRect, buttonText) && buttonEnabled)
            {
                if (AlchemyStation?.ActiveBill?.State == AlchemyFormingState.Completed)
                {
                    // Collect completed potion
                    AlchemyStation.EjectContents();
                    AlchemyStation.ActiveBill.Reset();
                    SetStatusMessage("Potion collected!", MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    TryBrewPotion();
                }
            }

            // NEW: Cancel button for active bills
            if (AlchemyStation?.ActiveBill != null && AlchemyStation.ActiveBill.State != AlchemyFormingState.Completed)
            {
                Rect cancelRect = new(centerX - 75f, canvas.y + canvas.height - 75f, 150f, 25f);
                if (Widgets.ButtonText(cancelRect, "Cancel Brewing"))
                {
                    AlchemyStation.ActiveBill.Reset();
                    AlchemyStation.EjectContents();
                    SetStatusMessage("Brewing cancelled", MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        // NEW: Helper method for bill status text
        private string GetBillStatusText(Bill_AlchemyAutonomous bill)
        {
            return bill.State switch
            {
                AlchemyFormingState.Waiting => $"Waiting: {bill.recipe.LabelCap}",
                AlchemyFormingState.Forming => $"Brewing: {bill.recipe.LabelCap} ({AlchemyStation.CurrentBillFormingPercent.ToStringPercent()})",
                AlchemyFormingState.Completed => $"Ready: {bill.recipe.LabelCap}",
                _ => ""
            };
        }

        private void DrawSlot(Rect slotRect, int slotIndex, string label)
        {
            slotRects[slotIndex] = slotRect;
            Widgets.DrawBox(slotRect);

            // NEW: Disable slots if currently brewing
            if (AlchemyStation?.ActiveBill?.State == AlchemyFormingState.Forming)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
            }

            if (slots[slotIndex] != null)
            {
                Widgets.ThingIcon(new(slotRect.x + 5f, slotRect.y + 5f, slotRect.width - 10f, slotRect.height - 10f), slots[slotIndex]);

                if (slotIndex > ApparatusSlotIndex && ingredientCounts[slotIndex] > 1)
                {
                    Text.Anchor = TextAnchor.LowerRight;
                    GUI.color = Color.white;
                    Widgets.Label(new(slotRect.x, slotRect.y, slotRect.width - 4f, slotRect.height - 4f), ingredientCounts[slotIndex].ToString());
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                }

                if (Mouse.IsOver(slotRect) && AlchemyStation?.ActiveBill?.State != AlchemyFormingState.Forming)
                {
                    TooltipHandler.TipRegion(slotRect, slots[slotIndex].LabelCap);

                    if (Event.current.type == EventType.MouseDown)
                    {
                        if (Event.current.button == 1)
                        {
                            slots[slotIndex] = null;
                            ingredientCounts[slotIndex] = 0;
                            Event.current.Use();
                        }
                        else if (Event.current.button == 0 && Event.current.control)
                        {
                            if (ingredientCounts[slotIndex] > 5)
                            {
                                ingredientCounts[slotIndex] -= 5;
                                Event.current.Use();
                            }
                        }
                        else if (Event.current.button == 0 && Event.current.shift)
                        {
                            int maxAvailable = GetMaxAvailableCount(slots[slotIndex]);
                            if (ingredientCounts[slotIndex] < maxAvailable)
                            {
                                ingredientCounts[slotIndex] = Mathf.Min(ingredientCounts[slotIndex] + 5, maxAvailable);
                                Event.current.Use();
                            }
                        }
                    }
                }
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
                Widgets.Label(slotRect, label);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(slotRect) && AlchemyStation?.ActiveBill?.State != AlchemyFormingState.Forming)
                {
                    Find.WindowStack.Add(new FloatMenu(GetSlotOptions(slotIndex)));
                }
            }

            if (slotIndex > ApparatusSlotIndex)
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
                Widgets.Label(new(slotRect.x + 2f, slotRect.y + 2f, 20f, 15f), slotIndex.ToString());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            GUI.color = Color.white; // Reset alpha
        }

        private int GetMaxAvailableCount(Thing thing)
        {
            if (thing == null) return 0;

            int totalCount = 0;

            // Check regular storage first
            foreach (Thing t in Find.CurrentMap.listerThings.AllThings)
            {
                if (t.def == thing.def && t.Spawned && t.IsInValidStorage() &&
                    t.Position.IsValid && !slots.Contains(t))
                {
                    totalCount += t.stackCount;
                }
            }

            // SEPARATE CHECK: Check linked buildings with refuelable components
            totalCount += GetCountFromLinkedRefuelableBuildings(thing.def);

            return totalCount;
        }

        // SEPARATE CHECK: Method to get items from linked buildings with refuelable comp
        private int GetCountFromLinkedRefuelableBuildings(ThingDef thingDef)
        {
            int count = 0;

            foreach (var building in GetLinkedRefuelableBuildings())
            {
                var refuelableComp = building.TryGetComp<CompRefuelable>();
                if (refuelableComp != null && refuelableComp.Props.fuelFilter.Allows(thingDef))
                {
                    // Check fuel in the refuelable comp
                    if (refuelableComp.Fuel > 0f)
                    {
                        // Convert fuel amount to approximate stack count
                        count += Mathf.FloorToInt(refuelableComp.Fuel / refuelableComp.Props.FuelMultiplierCurrentDifficulty);
                    }
                }
            }

            return count;
        }

        // Method to get all linked buildings with refuelable components
        private List<Thing> GetLinkedRefuelableBuildings()
        {
            var linkedBuildings = new List<Thing>();

            if (SelThing == null || !SelThing.Spawned)
                return linkedBuildings;

            // Check for facility linking comp
            var facilityComp = SelThing.TryGetComp<CompAffectedByFacilities>();
            if (facilityComp != null)
            {
                foreach (var building in facilityComp.LinkedFacilitiesListForReading)
                {
                    if (building != null && building.Spawned && building.TryGetComp<CompRefuelable>() != null)
                    {
                        linkedBuildings.Add(building);
                    }
                }
            }

            // Alternative: check for buildings in proximity with refuelable comp
            if (linkedBuildings.Count == 0)
            {
                var nearbyBuildings = GenRadial.RadialDistinctThingsAround(SelThing.Position, SelThing.Map, 10f, false)
                    .OfType<Thing>()
                    .Where(t => t != SelThing && t.TryGetComp<CompRefuelable>() != null);

                linkedBuildings.AddRange(nearbyBuildings);
            }

            return linkedBuildings;
        }

        private List<FloatMenuOption> GetSlotOptions(int slotIndex)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            if (slotIndex == ApparatusSlotIndex)
            {
                foreach (Thing thing in Find.CurrentMap.listerThings.AllThings)
                {
                    if (thing.def.HasModExtension<ApparatusModExtension>() &&
                        thing.Spawned &&
                        thing.Position.IsValid &&
                        !slots.Contains(thing))
                    {
                        options.Add(new FloatMenuOption(thing.LabelCap, delegate
                        {
                            slots[slotIndex] = thing;
                            ingredientCounts[slotIndex] = 1;
                        }));
                    }
                }
            }
            else
            {
                foreach (Thing thing in Find.CurrentMap.listerThings.AllThings)
                {
                    if (!thing.def.HasModExtension<ApparatusModExtension>() &&
                        thing.Spawned && thing.IsInValidStorage() &&
                        thing.Position.IsValid &&
                        !slots.Contains(thing))
                    {
                        int maxAvailable = GetMaxAvailableCount(thing);
                        options.Add(new FloatMenuOption($"{thing.LabelCap} ({maxAvailable} available)", delegate
                        {
                            slots[slotIndex] = thing;
                            ingredientCounts[slotIndex] = Mathf.Max(5, Mathf.Min(thing.stackCount, maxAvailable));
                        }));
                    }
                }
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("No suitable items found", null));
            }

            if (slots[slotIndex] != null)
            {
                options.Insert(0, new FloatMenuOption("Clear slot", delegate
                {
                    slots[slotIndex] = null;
                    ingredientCounts[slotIndex] = 0;
                }));
            }

            return options;
        }

        private void TryBrewPotion()
        {
            // NEW: Check if already has an active bill
            if (AlchemyStation?.ActiveBill != null && AlchemyStation.ActiveBill.State != AlchemyFormingState.Completed)
            {
                SetStatusMessage("Already brewing another potion!", MessageTypeDefOf.RejectInput);
                return;
            }

            bool hasIngredients = false;
            for (int i = 1; i < TotalSlots; i++)
            {
                if (slots[i] != null)
                {
                    hasIngredients = true;
                    break;
                }
            }

            if (!hasIngredients)
            {
                SetStatusMessage("Add at least one ingredient", MessageTypeDefOf.RejectInput);
                return;
            }

            FindMatchingRecipe();

            if (availableRecipes.Count == 0)
            {
                SetStatusMessage("No matching recipe found", MessageTypeDefOf.RejectInput);
                return;
            }

            if (availableRecipes.Count == 1)
            {
                CreateAutonomousBill(availableRecipes[0]);
            }
            else
            {
                ShowRecipeSelectionMenu();
            }
        }

        // NEW: Method to create autonomous bill instead of immediate brewing
        private void CreateAutonomousBill(RecipeDef recipe)
        {
            if (!CheckIngredientSufficiency(recipe))
            {
                SetStatusMessage("Not enough ingredients", MessageTypeDefOf.RejectInput);
                return;
            }

            var bill = new Bill_AlchemyAutonomous(recipe);

            // Add ingredients from slots to the bill
            for (int i = 0; i < TotalSlots; i++)
            {
                if (slots[i] != null)
                {
                    if (i == ApparatusSlotIndex)
                    {
                        bill.SetApparatus(slots[i]);
                    }
                    else
                    {
                        bill.AddIngredient(slots[i], ingredientCounts[i]);
                    }
                }
            }

            // Set the bill on the autonomous station
            AlchemyStation?.SetAlchemyBill(bill);

            SetStatusMessage($"Started brewing {recipe.LabelCap}!", MessageTypeDefOf.PositiveEvent);

            // Clear slots after starting the bill
            ClearSlots();
        }

        // NEW: Clear slots method
        private void ClearSlots()
        {
            for (int i = 0; i < TotalSlots; i++)
            {
                slots[i] = null;
                ingredientCounts[i] = 0;
            }
            availableRecipes.Clear();
        }

        // UPDATED: Modified to create autonomous bills
        private void ShowRecipeSelectionMenu()
        {
            List<FloatMenuOption> recipeOptions = new List<FloatMenuOption>();

            foreach (RecipeDef recipe in availableRecipes)
            {
                recipeOptions.Add(new FloatMenuOption(recipe.LabelCap, delegate
                {
                    CreateAutonomousBill(recipe);
                }));
            }

            if (recipeOptions.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(recipeOptions));
            }
        }

        private bool CheckIngredientSufficiency(RecipeDef recipe)
        {
            Dictionary<ThingDef, int> availableIngredients = GetAvailableIngredientCounts();

            foreach (IngredientCount requiredIngredient in recipe.ingredients)
            {
                bool hasEnough = false;

                foreach (ThingDef availableDef in availableIngredients.Keys)
                {
                    if (requiredIngredient.filter.Allows(availableDef))
                    {
                        int requiredCount = requiredIngredient.CountRequiredOfFor(availableDef, recipe);
                        if (availableIngredients[availableDef] >= requiredCount)
                        {
                            hasEnough = true;
                            break;
                        }
                    }
                }

                if (!hasEnough)
                {
                    return false;
                }
            }

            return true;
        }

        private Dictionary<ThingDef, int> GetAvailableIngredientCounts()
        {
            Dictionary<ThingDef, int> availableCounts = new Dictionary<ThingDef, int>();

            for (int i = 0; i < TotalSlots; i++)
            {
                if (slots[i] != null)
                {
                    ThingDef def = slots[i].def;
                    int count = (i == ApparatusSlotIndex) ? 1 : ingredientCounts[i];

                    if (availableCounts.ContainsKey(def))
                    {
                        availableCounts[def] += count;
                    }
                    else
                    {
                        availableCounts[def] = count;
                    }
                }
            }

            // SEPARATE CHECK: Add counts from linked refuelable buildings
            foreach (var building in GetLinkedRefuelableBuildings())
            {
                var refuelableComp = building.TryGetComp<CompRefuelable>();
                if (refuelableComp != null)
                {
                    // Check fuel in the refuelable comp
                    if (refuelableComp.Fuel > 0f)
                    {
                        // For each allowed fuel type, add the available amount
                        foreach (ThingDef allowedDef in refuelableComp.Props.fuelFilter.AllowedThingDefs)
                        {
                            if (allowedDef != null)
                            {
                                float fuelAmount = refuelableComp.Fuel;
                                int stackCount = Mathf.FloorToInt(fuelAmount / refuelableComp.Props.FuelMultiplierCurrentDifficulty);

                                if (availableCounts.ContainsKey(allowedDef))
                                {
                                    availableCounts[allowedDef] += stackCount;
                                }
                                else
                                {
                                    availableCounts[allowedDef] = stackCount;
                                }
                            }
                        }
                    }
                }
            }

            return availableCounts;
        }

        private void FindMatchingRecipe()
        {
            List<RecipeDef> allRecipes = DefDatabase<RecipeDef>.AllDefsListForReading;
            availableRecipes.Clear();

            Dictionary<ThingDef, int> providedIngredients = new Dictionary<ThingDef, int>();

            for (int i = 1; i < TotalSlots; i++)
            {
                if (slots[i] != null)
                {
                    if (providedIngredients.ContainsKey(slots[i].def))
                    {
                        providedIngredients[slots[i].def] += ingredientCounts[i];
                    }
                    else
                    {
                        providedIngredients[slots[i].def] = ingredientCounts[i];
                    }
                }
            }

            if (slots[ApparatusSlotIndex] != null)
            {
                providedIngredients[slots[ApparatusSlotIndex].def] = 1;
            }

            // SEPARATE CHECK: Add ingredients from linked buildings
            foreach (var building in GetLinkedRefuelableBuildings())
            {
                var refuelableComp = building.TryGetComp<CompRefuelable>();
                if (refuelableComp != null)
                {
                    // Add fuel from refuelable comp
                    if (refuelableComp.Fuel > 0f)
                    {
                        foreach (ThingDef allowedDef in refuelableComp.Props.fuelFilter.AllowedThingDefs)
                        {
                            if (allowedDef != null)
                            {
                                float fuelAmount = refuelableComp.Fuel;
                                int stackCount = Mathf.FloorToInt(fuelAmount / refuelableComp.Props.FuelMultiplierCurrentDifficulty);

                                if (providedIngredients.ContainsKey(allowedDef))
                                {
                                    providedIngredients[allowedDef] += stackCount;
                                }
                                else
                                {
                                    providedIngredients[allowedDef] = stackCount;
                                }
                            }
                        }
                    }
                }
            }

            foreach (RecipeDef recipe in allRecipes)
            {
                if (recipe.products == null || recipe.products.Count == 0)
                    continue;

                bool matches = true;

                foreach (IngredientCount requiredIngredient in recipe.ingredients)
                {
                    bool hasIngredient = false;

                    foreach (ThingDef providedDef in providedIngredients.Keys)
                    {
                        if (requiredIngredient.filter.Allows(providedDef) &&
                            providedIngredients[providedDef] >= requiredIngredient.CountRequiredOfFor(providedDef, recipe))
                        {
                            hasIngredient = true;
                            break;
                        }
                    }

                    if (!hasIngredient)
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    availableRecipes.Add(recipe);
                }
            }
        }

        private void SetStatusMessage(string message, MessageTypeDef type)
        {
            statusMessage = message;
            statusMessageType = type;
        }

        public override void OnOpen()
        {
            base.OnOpen();
            availableRecipes.Clear();
            statusMessage = "";
        }

        protected override void ExtraOnGUI()
        {
            base.ExtraOnGUI();

            if (slots[ApparatusSlotIndex] != null)
            {
                Rect apparatusRect = slotRects[ApparatusSlotIndex];
                Vector2 center = new Vector2(apparatusRect.x + apparatusRect.width / 2f, apparatusRect.y + apparatusRect.height / 2f);

                for (int i = 1; i < TotalSlots; i++)
                {
                    if (slots[i] != null)
                    {
                        Rect ingredientRect = slotRects[i];
                        Vector2 ingredientCenter = new Vector2(ingredientRect.x + ingredientRect.width / 2f, ingredientRect.y + ingredientRect.height / 2f);
                        Widgets.DrawLine(center, ingredientCenter, new Color(1f, 1f, 1f, 0.3f), 1f);
                    }
                }
            }
        }

        public class ApparatusModExtension : DefModExtension
        {
            public float efficiency = 1f;
            public string apparatusType;
        }
    }
}