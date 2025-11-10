using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone.Apprenticeship;
using TheForce_Standalone.Dialogs;
using Verse;

namespace TheForce_Standalone
{
    public class Force_ApprenticeshipSystem
    {
        private readonly CompClass_ForceUser parent;
        public Pawn Pawn => parent.Pawn;
        public HashSet<Pawn> apprentices = new HashSet<Pawn>();
        public int apprenticeCapacity = Force_ModSettings.apprenticeCapacity;
        public int graduatedApprenticesCount = 0;
        private Gizmo_Apprentice apprenticeGizmo;
        public bool hasBackstoryChanged = false;

        public Pawn master;
        public int ticksSinceLastXPGain;
        public int xpGainInterval;
        public const int ApplyBondCooldown = 60000; // 1 in-game day
        public int ticksSinceLastBondAttempt;

        public Force_ApprenticeshipSystem(CompClass_ForceUser parent)
        {
            this.parent = parent;
            this.xpGainInterval = Rand.Range(GenDate.TicksPerDay * 2, GenDate.TicksPerDay * 5);
        }

        public void Initialize()
        {
            // Initialization logic if needed
        }

        public void Tick()
        {
            if (Pawn == null) return;

            ticksSinceLastXPGain++;
            ticksSinceLastBondAttempt++;

            if (ticksSinceLastXPGain >= xpGainInterval)
            {
                GainExperience();
                ticksSinceLastXPGain = 0;
            }

            if (ticksSinceLastBondAttempt >= ApplyBondCooldown)
            {
                TryApplyForceBond();
                ticksSinceLastBondAttempt = 0;
            }
        }

        public void CompTickInterval(int delta)
        {
            // Handle interval-based ticking if needed
        }

        public IEnumerable<Gizmo> GetGizmos()
        {
            return null;
        }

        public float GetDarksideAlignment()
        {
            return Pawn.GetStatValueForPawn(ForceDefOf.Force_Darkside_Attunement, Pawn, true);
        }

        public float GetLightsideAlignment()
        {
            return Pawn.GetStatValueForPawn(ForceDefOf.Force_Lightside_Attunement, Pawn, true);
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref apprentices, "apprentices", LookMode.Reference);
            Scribe_Values.Look(ref apprenticeCapacity, "apprenticeCapacity", Force_ModSettings.apprenticeCapacity);
            Scribe_Values.Look(ref graduatedApprenticesCount, "graduatedApprenticesCount", 0);
            Scribe_Values.Look(ref hasBackstoryChanged, "hasBackstoryChanged", false);
            Scribe_References.Look(ref master, "master");
            Scribe_Values.Look(ref xpGainInterval, "xpGainInterval", 180000);
            Scribe_Values.Look(ref ticksSinceLastXPGain, "ticksSinceLastXPGain", 0);
            Scribe_Values.Look(ref ticksSinceLastBondAttempt, "ticksSinceLastBondAttempt", 0);
        }

        public void CheckAndPromoteMasterBackstory()
        {
            if (graduatedApprenticesCount >= Force_ModSettings.requiredGraduatedApprentices &&
                parent.forceLevel >= Force_ModSettings.requiredforceLevel && Force_ModSettings.rankUpMaster)
            {
                if (!hasBackstoryChanged)
                {
                    if (GetDarksideAlignment() > GetLightsideAlignment())
                    {
                        Pawn.story.Adulthood = ForceDefOf.Force_SithMaster;
                    }
                    else
                    {
                        Pawn.story.Adulthood = ForceDefOf.Force_JediMaster;
                    }

                    hasBackstoryChanged = true;
                    Messages.Message("Force_MasterPromotion".Translate(Pawn.Name.ToStringShort, Pawn.story.Adulthood.title), MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        public void ClearApprentices()
        {
            if (apprentices == null) return;

            foreach (var apprentice in apprentices.ToList())
            {
                if (apprentice?.GetComp<CompClass_ForceUser>() != null)
                {
                    var apprenticeComp = apprentice.GetComp<CompClass_ForceUser>();
                    apprenticeComp.Apprenticeship.master = null;

                    // Remove relations
                    if (Pawn != null)
                    {
                        apprentice.relations.RemoveDirectRelation(ForceDefOf.Force_ApprenticeRelation, Pawn);
                        Pawn.relations.RemoveDirectRelation(ForceDefOf.Force_MasterRelation, apprentice);
                    }
                }
            }
            apprentices.Clear();
        }

        private void EndApprenticeship()
        {
            if (master != null)
            {
                var masterComp = master.GetComp<CompClass_ForceUser>();
                if (masterComp?.Apprenticeship != null)
                {
                    masterComp.Apprenticeship.apprentices.Remove(Pawn);
                    masterComp.Apprenticeship.graduatedApprenticesCount++;
                    masterComp.Apprenticeship.CheckAndPromoteMasterBackstory();
                }

                // Remove relations
                Pawn.relations.RemoveDirectRelation(ForceDefOf.Force_MasterRelation, master);
                master.relations.RemoveDirectRelation(ForceDefOf.Force_ApprenticeRelation, Pawn);

                Messages.Message("Force.Apprentice_Graduated".Translate(Pawn.LabelShort, master.LabelShort),
                              Pawn,
                              MessageTypeDefOf.PositiveEvent);
            }

            master = null;
            Find.WindowStack.Add(new Dialog_SelectBackstory(Pawn));
        }

        public void ChangeApprenticeCapacitySetting(int newCapacity)
        {
            Force_ModSettings.apprenticeCapacity = newCapacity;
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
            apprenticeCapacity = newCapacity;
            apprenticeGizmo = null;
        }

        private void GainExperience()
        {
            if (master == null || Pawn == null) return;
            var masterComp = master.GetComp<CompClass_ForceUser>();

            int levelDifference = masterComp.forceLevel - parent.forceLevel;
            if (levelDifference > 0)
            {
                parent.Leveling.AddForceExperience(levelDifference * 10);
                Messages.Message("Force.Apprentice_XPGain".Translate(Pawn.LabelShort, levelDifference * 10),
                              Pawn,
                              MessageTypeDefOf.PositiveEvent);
            }

            if (parent.forceLevel >= masterComp.forceLevel)
            {
                Messages.Message("Force.Apprentice_GraduationReady".Translate(Pawn.LabelShort),
                              Pawn,
                              MessageTypeDefOf.PositiveEvent);
                EndApprenticeship();
            }
        }

        private void TryApplyForceBond()
        {
            if (Rand.Chance(0.1f) && master != null && Pawn != null)
            {
                if (!master.health.hediffSet.HasHediff(ForceDefOf.ForceBond_MasterApprentice))
                {
                    Hediff hediff = HediffMaker.MakeHediff(ForceDefOf.ForceBond_MasterApprentice, master);
                    master.health.AddHediff(hediff);
                    Pawn.health.AddHediff(hediff);
                    Messages.Message("Force.Apprentice_BondFormed".Translate(master.LabelShort, Pawn.LabelShort),
                                  Pawn,
                                  MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        public void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
        {
            if (victim == master && Pawn.GetStatValue(ForceDefOf.Force_Darkside_Attunement) > Pawn.GetStatValue(ForceDefOf.Force_Lightside_Attunement))
            {
                Messages.Message("Force.Apprentice_MasterKilled".Translate(Pawn.LabelShort, master.LabelShort),
                              Pawn,
                              MessageTypeDefOf.NegativeEvent);
                EndApprenticeship();
                Find.WindowStack.Add(new Dialog_SelectBackstory(Pawn));
            }
        }

        public void AssignApprentice(Pawn targetPawn)
        {
            var masterComp = this;
            var targetComp = targetPawn.GetComp<CompClass_ForceUser>();

            if (masterComp == null || targetComp == null) return;

            // Add to master's apprentice list
            if (!masterComp.apprentices.Contains(targetPawn))
            {
                masterComp.apprentices.Add(targetPawn);
            }

            // Set apprentice's master
            targetComp.Apprenticeship.master = Pawn;

            targetPawn.Notify_DisabledWorkTypesChanged();
            PawnComponentsUtility.AddAndRemoveDynamicComponents(targetPawn);
        }
    }
}