using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone.Apprenticeship
{
    internal class Hediff_Master : HediffWithComps
    {
        public HashSet<Pawn> apprentices = new HashSet<Pawn>();
        public int apprenticeCapacity = Force_ModSettings.apprenticeCapacity;
        public int graduatedApprenticesCount = 0;
        private Gizmo_Apprentice apprenticeGizmo;
        private bool hasBackstoryChanged = false;

        public override string Label
        {
            get
            {
                string label = base.Label + " of : ";
                if (apprentices != null && apprentices.Count > 0)
                {
                    label += string.Join(", ", apprentices.Select(apprentice => apprentice.LabelShort));
                }
                else
                {
                    label += "No apprentices";
                }

                return label;
            }
        }

        public float GetDarksideAlignment()
        {
            return this.pawn.GetStatValueForPawn(ForceDefOf.Force_Darkside_Attunement, this.pawn, true);
        }

        public float GetLightsideAlignment()
        {
            return this.pawn.GetStatValueForPawn(ForceDefOf.Force_Lightside_Attunement, this.pawn, true);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref apprentices, "apprentices", LookMode.Reference);
            Scribe_Values.Look(ref apprenticeCapacity, "apprenticeCapacity", Force_ModSettings.apprenticeCapacity);
            Scribe_Values.Look(ref hasBackstoryChanged, "hasBackstoryChanged", false);
        }

        public void CheckAndPromoteMasterBackstory()
        {
            if (graduatedApprenticesCount >= Force_ModSettings.requiredGraduatedApprentices &&
                pawn.GetComp<CompClass_ForceUser>().forceLevel >= Force_ModSettings.requiredforceLevel && Force_ModSettings.rankUpMaster)
            {
                if (!hasBackstoryChanged)
                {
                    if (GetDarksideAlignment() > GetLightsideAlignment())
                    {
                        pawn.story.Adulthood = ForceDefOf.Force_SithMaster;
                    }
                    else
                    {
                        pawn.story.Adulthood = ForceDefOf.Force_JediMaster;
                    }

                    hasBackstoryChanged = true;
                    Messages.Message("Force_MasterPromotion".Translate(pawn.Name.ToStringShort, pawn.story.Adulthood.title), MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            ClearApprentices();
        }

        public void ClearApprentices()
        {
            if (apprentices == null) return;

            foreach (var apprentice in apprentices.ToList())
            {
                if (apprentice?.health?.hediffSet == null) continue;

                var hediff = apprentice.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Apprentice);
                if (hediff != null)
                {
                    apprentice.health.RemoveHediff(hediff);
                }
            }
            apprentices.Clear();
        }


        public void ChangeApprenticeCapacitySetting(int newCapacity)
        {
            Force_ModSettings.apprenticeCapacity = newCapacity; // Update the setting
            var currentMap = Find.CurrentMap;
            if (currentMap == null) return;

            foreach (var pawn in currentMap.mapPawns.AllPawns)
            {
                if (pawn?.health?.hediffSet == null) continue;

                if (pawn.health.hediffSet.HasHediff(ForceDefOf.Force_Master))
                {
                    var masterHediff = pawn.health.hediffSet.GetFirstHediffOfDef(ForceDefOf.Force_Master) as Hediff_Master;
                    if (masterHediff != null)
                    {
                        masterHediff.UpdateApprenticeCapacity(newCapacity);
                    }
                }
            }
        }

        public void UpdateApprenticeCapacity(int newCapacity)
        {
            apprenticeCapacity = newCapacity; // Update the capacity
            apprenticeGizmo = null; // Invalidate the gizmo to force a refresh next time
        }
    }
}