using Rhino;
using Rhino.Commands;
using Rhino.PlugIns;
using System;
using System.Runtime.InteropServices;

namespace ParasiticGrowth
{

    [Guid("D4C27EE9-E821-40BB-B432-1BBB5C5A6813")]
    public class ParasiticGrowthPlugIn : PlugIn
    {
        public ParasiticGrowthPlugIn()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static ParasiticGrowthPlugIn Instance { get; private set; }
    }
}