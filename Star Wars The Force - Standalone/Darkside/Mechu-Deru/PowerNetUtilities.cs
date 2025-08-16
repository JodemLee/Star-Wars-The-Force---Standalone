using RimWorld;
using System.Collections.Generic;
using Verse;

namespace TheForce_Standalone.Darkside.Mechu_Deru
{
    public static class PowerNetUtility
    {
        public static HashSet<Building> GetConnectedBuildings(CompPower powerComp)
        {
            HashSet<Building> connectedBuildings = new HashSet<Building>();

            if (powerComp?.PowerNet == null)
            {
                return connectedBuildings;
            }

            foreach (var transmitter in powerComp.PowerNet.transmitters)
            {
                if (transmitter.parent is Building building)
                {
                    connectedBuildings.Add(building);
                }
            }

            foreach (var connector in powerComp.PowerNet.connectors)
            {
                if (connector.parent is Building building)
                {
                    connectedBuildings.Add(building);
                }
            }

            return connectedBuildings;
        }
    }
}
