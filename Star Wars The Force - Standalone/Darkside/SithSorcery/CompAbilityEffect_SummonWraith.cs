using RimWorld;
using System;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheForce_Standalone.Darkside.SithSorcery
{
    internal class CompAbilityEffect_SummonWraith : CompAbilityEffect
    {
        public new CompProperties_SummonWraith Props => props as CompProperties_SummonWraith;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            try
            {
                if (!target.IsValid || parent.pawn.Map == null)
                {
                    Log.Warning("Summon Wraith: Invalid target or map");
                    return;
                }

                if (parent.pawn == null || parent.pawn.Map == null)
                {
                    Log.Warning("Summon Wraith: Parent pawn or map is null");
                    return;
                }

                Pawn wraith = PawnGenerator.GeneratePawn(ForceDefOf.Force_DarksideWraith, parent.pawn.Faction ?? Faction.OfPlayer);
                if (wraith == null)
                {
                    Log.Error("Failed to generate wraith pawn");
                    return;
                }

                var forceUser = parent.pawn.GetComp<CompClass_ForceUser>();
                if (forceUser != null)
                {
                    var animalComp = wraith.TryGetComp<Comp_RandomAnimalAppearance>();
                    if (animalComp != null)
                    {
                        if (!forceUser.wraithAnimalTexPath.NullOrEmpty())
                        {
                            // FIX: Add null check for wraithAnimalKind
                            if (forceUser.wraithAnimalKind != null)
                            {
                                animalComp.animalColor = forceUser.barColor;
                                animalComp.animalColor.a = 0.7f;
                                animalComp.SetAppearance(forceUser.wraithAnimalKind);
                            }
                            else
                            {
                                Log.Warning("Force user wraithAnimalKind is null, randomizing appearance");
                                animalComp.RandomizeAppearance();
                            }
                        }
                        else
                        {
                            animalComp.RandomizeAppearance();
                            Log.Warning("Force user has no wraith animal appearance set");
                        }
                    }
                }

                GenSpawn.Spawn(wraith, target.Cell, parent.pawn.Map);

                foreach (TrainableDef allDef in DefDatabase<TrainableDef>.AllDefs)
                {
                    if (wraith.training.GetWanted(allDef))
                    {
                        wraith.training.Train(allDef, parent.pawn, complete: true);
                    }
                }
                foreach (TrainableDef allDef2 in DefDatabase<TrainableDef>.AllDefs)
                {
                    if (wraith.training.CanAssignToTrain(allDef2).Accepted)
                    {
                        wraith.training.Train(allDef2, null, complete: true);
                    }
                }
                wraith.playerSettings.Master = parent.pawn;
                wraith.playerSettings.followDrafted = true;
                wraith.playerSettings.followFieldwork = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in SummonWraith.Apply: {ex}");
                throw;
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Cell == null)
            {
                if (throwMessages)
                {
                    Messages.Message("AbilityMustTargetValidLocation".Translate(), MessageTypeDefOf.RejectInput);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }

    public class CompProperties_SummonWraith : CompProperties_AbilityEffect
    {
        public CompProperties_SummonWraith()
        {
            compClass = typeof(CompAbilityEffect_SummonWraith);
        }
    }

    public class ThinkNode_Wraith : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn) =>
            pawn.def == ForceDefOf.Force_DarksideWraithRace;
    }

    public class DeathActionWorker_Wraith : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            if (corpse.Map != null)
            {
                corpse.Destroy();
            }
        }
    }

    public class CompSummonerDependency : ThingComp
    {
        public Pawn Summoner;
        public CompProperties_SummonerDependency Props => this.props as CompProperties_SummonerDependency;

        public override void CompTick()
        {
            base.CompTick();
            if (this.Summoner is { Dead: true } or { Destroyed: true } or null)
                this.parent.Kill();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref this.Summoner, "summoner");
        }
    }

    public class CompProperties_SummonerDependency : CompProperties
    {
        public CompProperties_SummonerDependency()
        {
            this.compClass = typeof(CompSummonerDependency);
        }
    }

}

