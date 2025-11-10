using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;

namespace TheForce_Standalone.Darkside.SithSorcery.Alchemy
{
    public class Bill_AlchemyAutonomous : Bill
    {
        private AlchemyFormingState state = AlchemyFormingState.Waiting;
        public List<Thing> providedIngredients = new List<Thing>();
        public Dictionary<ThingDef, int> ingredientCounts = new Dictionary<ThingDef, int>();
        public float formingTicks;
        public Thing apparatus;

        public AlchemyFormingState State
        {
            get => state;
            set => state = value;
        }

        public Bill_AlchemyAutonomous() : base() { }

        public Bill_AlchemyAutonomous(RecipeDef recipe, Precept_ThingStyle precept = null) : base(recipe, precept)
        {
            formingTicks = recipe.workAmount;
        }

        // FIXED: Implement the required abstract method
        public override bool ShouldDoNow()
        {
            return state == AlchemyFormingState.Waiting && HasSufficientIngredients();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref state, "alchemyState");
            Scribe_Values.Look(ref formingTicks, "formingTicks");
            Scribe_Collections.Look(ref providedIngredients, "providedIngredients", LookMode.Reference);
            Scribe_References.Look(ref apparatus, "apparatus");
            Scribe_Collections.Look(ref ingredientCounts, "ingredientCounts", LookMode.Def, LookMode.Value);
        }

        public virtual void BillTick()
        {
            if (state == AlchemyFormingState.Forming)
            {
                formingTicks--;
                if (formingTicks <= 0)
                {
                    formingTicks = 0;
                    state = AlchemyFormingState.Completed;
                    Notify_FormingCompleted();
                }
            }
        }

        public virtual void Notify_FormingCompleted()
        {
            // This will be called when the forming process completes
            Thing product = CreateAlchemyProducts();

            // Get the bill giver (the alchemy station)
            var alchemyStation = billStack?.billGiver as Building_AlchemyStationAutonomous;
            if (alchemyStation != null)
            {
                // Clear and add the product to the station's container
                alchemyStation.innerContainer.ClearAndDestroyContents();
                if (product != null)
                {
                    alchemyStation.innerContainer.TryAdd(product);
                }

                // Auto-eject the product
                alchemyStation.EjectContents();

                // Reset the bill
                Reset();
            }
        }

        public virtual Thing CreateAlchemyProducts()
        {
            if (recipe.products.Count == 0)
                return null;

            ThingDef productDef = recipe.products[0].thingDef;
            int productCount = recipe.products[0].count;

            Thing product;

            // Handle pawn creation (for creature summoning, etc.)
            if (productDef.race != null)
            {
                product = PawnGenerator.GeneratePawn(productDef.race.AnyPawnKind, Faction.OfPlayer);
            }
            else
            {
                // Handle regular items with potential stuff
                ThingDef stuffDef = null;
                if (productDef.MadeFromStuff)
                {
                    stuffDef = SelectStuffFromIngredients();
                }
                product = ThingMaker.MakeThing(productDef, stuffDef);
                product.stackCount = productCount;
            }

            // FIXED: Remove Potion reference - you can add this back if you create a Potion class
            // Apply alchemy-specific modifications here if needed

            return product;
        }

        private ThingDef SelectStuffFromIngredients()
        {
            foreach (var ingredient in ingredientCounts)
            {
                if (ingredient.Key.IsStuff && recipe.products[0].thingDef.stuffCategories != null &&
                    recipe.products[0].thingDef.stuffCategories.Any(sc => ingredient.Key.stuffProps.categories.Contains(sc)))
                {
                    return ingredient.Key;
                }
            }

            // Fallback to first ingredient that's stuff
            foreach (var ingredient in ingredientCounts)
            {
                if (ingredient.Key.IsStuff)
                {
                    return ingredient.Key;
                }
            }

            return ThingDefOf.Steel; // Final fallback
        }

        public virtual void Reset()
        {
            state = AlchemyFormingState.Waiting;
            providedIngredients.Clear();
            ingredientCounts.Clear();
            apparatus = null;
            formingTicks = recipe.workAmount;
        }

        public void AppendInspectionData(StringBuilder sb)
        {
            switch (state)
            {
                case AlchemyFormingState.Waiting:
                    sb.AppendLine($"Waiting for ingredients: {recipe.LabelCap}");
                    break;
                case AlchemyFormingState.Forming:
                    float progress = 1f - formingTicks / recipe.workAmount;
                    sb.AppendLine($"Brewing: {recipe.LabelCap} ({progress.ToStringPercent()})");
                    break;
                case AlchemyFormingState.Completed:
                    sb.AppendLine($"Completed: {recipe.LabelCap}");
                    break;
            }
        }

        public void StartForming()
        {
            if (state == AlchemyFormingState.Waiting && HasSufficientIngredients())
            {
                state = AlchemyFormingState.Forming;
                ConsumeIngredients();
            }
        }

        private bool HasSufficientIngredients()
        {
            // Check if we have all required ingredients in sufficient quantities
            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                bool hasEnough = false;
                foreach (var provided in ingredientCounts)
                {
                    if (ingredient.filter.Allows(provided.Key) && provided.Value >= ingredient.CountRequiredOfFor(provided.Key, recipe))
                    {
                        hasEnough = true;
                        break;
                    }
                }
                if (!hasEnough)
                    return false;
            }
            return true;
        }

        private void ConsumeIngredients()
        {
            // Consume the ingredients from the world
            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                foreach (var provided in ingredientCounts)
                {
                    if (ingredient.filter.Allows(provided.Key))
                    {
                        int required = ingredient.CountRequiredOfFor(provided.Key, recipe);
                        ConsumeFromWorld(provided.Key, required);
                        break;
                    }
                }
            }
        }

        private void ConsumeFromWorld(ThingDef thingDef, int amount)
        {
            int remaining = amount;

            // Consume from provided ingredients first
            foreach (var thing in providedIngredients.ToArray())
            {
                if (remaining <= 0) break;

                if (thing.def == thingDef)
                {
                    if (thing.stackCount <= remaining)
                    {
                        remaining -= thing.stackCount;
                        thing.Destroy();
                        providedIngredients.Remove(thing);
                    }
                    else
                    {
                        thing.SplitOff(remaining);
                        remaining = 0;
                    }
                }
            }

            // Consume from world if still needed
            if (remaining > 0)
            {
                List<Thing> thingsInWorld = Find.CurrentMap.listerThings.AllThings
                    .Where(t => t.def == thingDef && t.Spawned && t.IsInValidStorage())
                    .OrderBy(t => t.stackCount)
                    .ToList();

                foreach (Thing thing in thingsInWorld)
                {
                    if (remaining <= 0) break;

                    if (thing.stackCount <= remaining)
                    {
                        remaining -= thing.stackCount;
                        thing.Destroy();
                    }
                    else
                    {
                        Thing splitStack = thing.SplitOff(remaining);
                        if (splitStack != null)
                        {
                            splitStack.Destroy();
                        }
                        remaining = 0;
                    }
                }
            }
        }

        public void AddIngredient(Thing thing, int count)
        {
            providedIngredients.Add(thing);
            if (ingredientCounts.ContainsKey(thing.def))
            {
                ingredientCounts[thing.def] += count;
            }
            else
            {
                ingredientCounts[thing.def] = count;
            }
        }

        public void SetApparatus(Thing apparatusThing)
        {
            apparatus = apparatusThing;
        }

        protected override string StatusString
        {
            get
            {
                switch (state)
                {
                    case AlchemyFormingState.Waiting:
                        return "Waiting for ingredients";
                    case AlchemyFormingState.Forming:
                        return $"Brewing: {(1f - formingTicks / recipe.workAmount).ToStringPercent()}";
                    case AlchemyFormingState.Completed:
                        return "Ready to collect";
                    default:
                        return null;
                }
            }
        }

        public override Bill Clone()
        {
            Bill_AlchemyAutonomous obj = (Bill_AlchemyAutonomous)base.Clone();
            obj.state = state;
            obj.formingTicks = formingTicks;
            obj.providedIngredients = new List<Thing>(providedIngredients);
            obj.ingredientCounts = new Dictionary<ThingDef, int>(ingredientCounts);
            obj.apparatus = apparatus;
            return obj;
        }
    }

    public enum AlchemyFormingState
    {
        Waiting,
        Forming,
        Completed
    }


// Updated autonomous alchemy station
    public class Building_AlchemyStationAutonomous : Building_WorkTable, IThingHolder, INotifyHauledTo
    {
        public ThingOwner innerContainer;
        protected Bill_AlchemyAutonomous activeBill;

        public Bill_AlchemyAutonomous ActiveBill
        {
            get => activeBill;
            set
            {
                if (activeBill != value)
                {
                    activeBill = value;
                }
            }
        }

        public float CurrentBillFormingPercent
        {
            get
            {
                if (activeBill == null || activeBill.State != AlchemyFormingState.Forming)
                {
                    return 0f;
                }
                return 1f - activeBill.formingTicks / activeBill.recipe.workAmount;
            }
        }

        public Building_AlchemyStationAutonomous()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public virtual void Notify_StartForming()
        {
            activeBill?.StartForming();
        }

        public override void Notify_BillDeleted(Bill bill)
        {
            if (activeBill == bill)
            {
                EjectContents();
                activeBill = null;
            }
        }

        public virtual void Notify_HauledTo(Pawn hauler, Thing thing, int count)
        {
            // Auto-add hauled items to the current bill if they match requirements
            if (activeBill != null && activeBill.State == AlchemyFormingState.Waiting)
            {
                if (activeBill.IsFixedOrAllowedIngredient(thing))
                {
                    activeBill.AddIngredient(thing, count);

                    // Auto-start if we have enough ingredients
                    if (activeBill.State == AlchemyFormingState.Waiting)
                    {
                        Notify_StartForming();
                    }
                }
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            activeBill?.Reset();
            EjectContents();
            base.Destroy(mode);
        }

        public virtual void EjectContents()
        {
            innerContainer.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
        }

        public virtual bool CanWork()
        {
            return PowerComp?.TransmitsPowerNow != false && !Destroyed;
        }

        protected override void Tick()
        {
            base.Tick();
            if (activeBill != null && CanWork())
            {
                activeBill.BillTick();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_References.Look(ref activeBill, "activeBill");
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }

            if (activeBill != null)
            {
                activeBill.AppendInspectionData(stringBuilder);
            }
            else
            {
                stringBuilder.AppendLine("Ready for alchemy");
            }

            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (ActiveBill != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Cancel Alchemy",
                    defaultDesc = "Cancel the current alchemy process",
                    icon = TexCommand.ClearPrioritizedWork,
                    action = delegate
                    {
                        activeBill?.Reset();
                        EjectContents();
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }
                };
            }

            if (DebugSettings.ShowDevGizmos)
            {
                Bill_AlchemyAutonomous bill = ActiveBill;
                if (bill != null && bill.State == AlchemyFormingState.Forming)
                {
                    yield return new Command_Action
                    {
                        action = delegate
                        {
                            bill.formingTicks -= bill.recipe.workAmount * 0.25f;
                        },
                        defaultLabel = "DEV: Forming +25%"
                    };
                    yield return new Command_Action
                    {
                        action = delegate
                        {
                            bill.formingTicks = 0f;
                            bill.BillTick(); // Force completion
                        },
                        defaultLabel = "DEV: Complete brewing"
                    };
                }
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

        // Integration method for your ITab_Alchemy
        public void SetAlchemyBill(Bill_AlchemyAutonomous bill)
        {
            ActiveBill = bill;
            if (bill != null)
            {
                Notify_StartForming();
            }
        }
    }
}