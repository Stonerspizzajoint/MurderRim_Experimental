using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WorkerDronesMod
{
    // ModExtension class for configurable properties
    public class GeneExtension_Learning : DefModExtension
    {
        // Standard observation
        public float observationRadius = 10f;
        public int checkIntervalTicks = 250;
        public float xpPerObservation = 50f;

        // Television learning
        public float xpPerTVObservation = 30f;
        public int tvMaxSkillLevel = 10;
    }
}
