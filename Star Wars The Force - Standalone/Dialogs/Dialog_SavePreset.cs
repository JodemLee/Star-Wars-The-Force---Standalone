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
    public class Dialog_SavePreset : Window
    {
        private CompClass_ForceUser forceUser;
        private string presetName = "";

        public Dialog_SavePreset(CompClass_ForceUser forceUser)
        {
            this.forceUser = forceUser;
            doCloseButton = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(300f, 150f);

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new(0f, 0f, inRect.width, 30f), "Save Preset As:");
            presetName = Widgets.TextField(new(0f, 40f, inRect.width, 30f), presetName);

            if (Widgets.ButtonText(new(0f, 80f, 100f, 30f), "Save") && !string.IsNullOrEmpty(presetName))
            {
                if (!forceUser.Abilities.abilityPresets.ContainsKey(presetName))
                {
                    forceUser.Abilities.SaveCurrentAsPreset(presetName);
                    Messages.Message($"Saved as: {presetName}", MessageTypeDefOf.PositiveEvent);
                    Close();
                }
                else
                {
                    Messages.Message("Name already exists!", MessageTypeDefOf.RejectInput);
                }
            }
        }
    }
}
