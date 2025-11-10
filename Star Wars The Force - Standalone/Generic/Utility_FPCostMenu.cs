using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Generic
{
    public static class Utility_FPCostMenu
    {
        public struct ForceMenuOption
        {
            public string labelKey;
            public float percentage;
            public Action<LocalTargetInfo, float> action; // Changed to include parameters
            public bool useCurrentFP;

            public ForceMenuOption(string labelKey, float percentage, Action<LocalTargetInfo, float> action, bool useCurrentFP = false)
            {
                this.labelKey = labelKey;
                this.percentage = percentage;
                this.action = action;
                this.useCurrentFP = useCurrentFP;
            }
        }

        public static void ShowForcePercentageMenu(Pawn pawn, LocalTargetInfo target, List<ForceMenuOption> menuOptions, string notEnoughFPTooltipKey = null)
        {
            var forceUser = pawn.GetComp<CompClass_ForceUser>();
            if (forceUser == null) return;

            var options = new List<FloatMenuOption>();

            foreach (var menuOption in menuOptions)
            {
                // Create the action with the correct percentage
                Action action = () => menuOption.action?.Invoke(target, menuOption.percentage);

                var option = new FloatMenuOption(
                    menuOption.labelKey.Translate(), // This should work if you have the translation keys
                    action
                );

                // FP validation
                if (menuOption.useCurrentFP)
                {
                    if (forceUser.currentFP <= 0)
                    {
                        option.Disabled = true;
                        if (notEnoughFPTooltipKey != null)
                            option.tooltip = notEnoughFPTooltipKey.Translate(pawn.LabelShort, 0);
                    }
                }
                else
                {
                    float fpCost = forceUser.MaxFP * menuOption.percentage;
                    if (forceUser.currentFP < fpCost)
                    {
                        option.Disabled = true;
                        if (notEnoughFPTooltipKey != null)
                            option.tooltip = notEnoughFPTooltipKey.Translate(pawn.LabelShort, fpCost);
                    }
                }

                options.Add(option);
            }

            if (options.Any())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public static List<ForceMenuOption> CreateStandardPercentages(Action<LocalTargetInfo, float> action)
        {
            return new List<ForceMenuOption>
            {
                new ForceMenuOption("Force.Standard_25", 0.25f, action),
                new ForceMenuOption("Force.Standard_50", 0.50f, action),
                new ForceMenuOption("Force.Standard_75", 0.75f, action),
                new ForceMenuOption("Force.Standard_100", 1.00f, action, true)
            };
        }

        public static List<ForceMenuOption> CreateDetailedPercentages(Action<LocalTargetInfo, float> action)
        {
            return new List<ForceMenuOption>
            {
                new ForceMenuOption("Force.Detailed_25", 0.25f, action),
                new ForceMenuOption("Force.Detailed_33", 0.33f, action),
                new ForceMenuOption("Force.Detailed_50", 0.50f, action),
                new ForceMenuOption("Force.Detailed_66", 0.66f, action),
                new ForceMenuOption("Force.Detailed_100", 1.00f, action, true)
            };
        }
    }
}