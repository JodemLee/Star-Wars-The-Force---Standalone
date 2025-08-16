using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public static class GizmoUtils
    {
        public static IEnumerable<Gizmo> GetForceUserGizmos(CompClass_ForceUser comp)
        {
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Force.Debug.SetDarkSide".Translate(),
                    action = () => Find.WindowStack.Add(new Dialog_Slider(
                        "Force.Debug.DarkSideSlider".Translate(), 0, 100,
                        val => comp.Alignment.DarkSideAttunement = val
                    ))
                };

                yield return new Command_Action
                {
                    defaultLabel = "Force.Debug.SetLightSide".Translate(),
                    action = () => Find.WindowStack.Add(new Dialog_Slider(
                        "Force.Debug.LightSideSlider".Translate(), 0, 100,
                        val => comp.Alignment.LightSideAttunement = val
                    ))
                };
            }

            if (comp.forceLevel > 12 && comp.Limbs.HasReplaceableLimbs())
            {
                yield return new Command_Action
                {
                    defaultLabel = "Force.Limbs.ReplaceLabel".Translate(),
                    defaultDesc = "Force.Limbs.ReplaceDescription".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Abilities/Telekinesis/ForceLimb", true),
                    action = () => comp.Limbs.ReplaceMissingLimbs()
                };
            }
        }
    }

}