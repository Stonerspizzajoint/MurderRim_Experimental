using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WorkerDronesMod
{
    public class Hediff_NeutroOilCraving : HediffWithComps
    {
        public override string SeverityLabel
        {
            get
            {
                if (this.Severity == 0f)
                {
                    return null;
                }
                return this.Severity.ToStringPercent();
            }
        }
    }
}
