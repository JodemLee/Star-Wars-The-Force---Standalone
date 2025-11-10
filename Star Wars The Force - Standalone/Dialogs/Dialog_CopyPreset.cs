using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TheForce_Standalone.Dialogs
{
    public class Dialog_CopyPreset : Window
    {
        private CompClass_ForceUser forceUser;
        private string sourcePresetName;
        private string newPresetName = "";

        // Property to get the correct ability presets
        private Dictionary<string, AbilityPreset> AbilityPresets => forceUser.Abilities.abilityPresets;

        public Dialog_CopyPreset(CompClass_ForceUser forceUser, string sourcePresetName)
        {
            this.forceUser = forceUser;
            this.sourcePresetName = sourcePresetName;
            doCloseButton = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(300f, 150f);

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new(0f, 0f, inRect.width, 30f), $"Copy '{sourcePresetName}' to:");
            newPresetName = Widgets.TextField(new(0f, 40f, inRect.width, 30f), newPresetName);

            if (Widgets.ButtonText(new(0f, 80f, 100f, 30f), "Copy") && !string.IsNullOrEmpty(newPresetName))
            {
                if (!AbilityPresets.ContainsKey(newPresetName))
                {
                    if (AbilityPresets.TryGetValue(sourcePresetName, out var sourcePreset))
                    {
                        // Create a copy of the preset
                        var newPreset = new AbilityPreset
                        {
                            presetName = newPresetName,
                            activeAbilities = new HashSet<string>(sourcePreset.activeAbilities)
                        };
                        AbilityPresets[newPresetName] = newPreset;
                        Messages.Message($"Copied '{sourcePresetName}' to '{newPresetName}'", MessageTypeDefOf.PositiveEvent);
                        Close();
                    }
                }
                else
                {
                    Messages.Message("Preset name already exists!", MessageTypeDefOf.RejectInput);
                }
            }
        }
    }
}
