using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheForce_Standalone
{
    public class ForceMapComponent : MapComponent
    {
        private Dictionary<Thing, List<Pawn>> linkedObjects = new Dictionary<Thing, List<Pawn>>();

        public ForceMapComponent(Map map) : base(map) { }

        public void RegisterLinkedObject(Thing thing, Pawn pawn)
        {
            if (!linkedObjects.ContainsKey(thing))
            {
                linkedObjects.Add(thing, new List<Pawn>());
            }
            if (!linkedObjects[thing].Contains(pawn))
            {
                linkedObjects[thing].Add(pawn);
            }
        }

        public bool IsObjectLinked(Thing thing)
        {
            return linkedObjects.ContainsKey(thing) && linkedObjects[thing].Count > 0;
        }

        public IEnumerable<Pawn> GetPawnsLinkedTo(Thing thing)
        {
            if (linkedObjects.TryGetValue(thing, out var pawns))
            {
                return pawns.Where(p => p != null && !p.Destroyed);
            }
            return Enumerable.Empty<Pawn>();
        }

        public bool TryGetLinkedObject(Pawn pawn, out Thing linkedObject)
        {
            foreach (var kvp in linkedObjects)
            {
                if (kvp.Value.Contains(pawn))
                {
                    linkedObject = kvp.Key;
                    return true;
                }
            }
            linkedObject = null;
            return false;
        }

        public void CleanupInvalidLinks()
        {
            // Remove destroyed pawns from all links
            foreach (var kvp in linkedObjects.ToList())
            {
                kvp.Value.RemoveAll(p => p == null || p.Destroyed);
                if (kvp.Value.Count == 0)
                {
                    linkedObjects.Remove(kvp.Key);
                }
            }

            var destroyedObjects = linkedObjects.Keys.Where(t => t == null || t.Destroyed).ToList();
            foreach (var obj in destroyedObjects)
            {
                linkedObjects.Remove(obj);
            }
        }

        public IEnumerable<Thing> GetAllLinkedObjects()
        {
            return linkedObjects.Keys.Where(thing => !thing.Destroyed);
        }

        public void UnregisterLinkedObject(Thing thing, Pawn pawn)
        {
            if (linkedObjects.TryGetValue(thing, out var pawns))
            {
                pawns.Remove(pawn);
                if (pawns.Count == 0)
                {
                    linkedObjects.Remove(thing);
                }
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                CleanupInvalidLinks();
                var destroyedObjects = linkedObjects.Keys.Where(t => t.Destroyed).ToList();
                foreach (var obj in destroyedObjects)
                {
                    foreach (var pawn in linkedObjects[obj].Where(p => p != null && !p.Destroyed))
                    {
                        var forceUser = pawn.GetComp<CompClass_ForceUser>();
                        forceUser?.ReceiveCompSignal("LinkedObjectDestroyed");
                    }
                    linkedObjects.Remove(obj);
                }
            }
        }
    }
}
