using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace WorkerDronesMod
{
    public class ThoughtWorker_BlindingSunlamp : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // Activate the thought if the pawn is in sun lamp light
            return SolarUtil.InSunLampLight(pawn) ? ThoughtState.ActiveDefault : ThoughtState.Inactive;
        }
    }
}
