using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWActorEdit.Source.Util
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ChunkName : Attribute
    {
        public string OriginalName; //Original name of chunk, ie: 2DMA or SCLS
        public string HumanName; //Human readable name, ie "Doors", etc.

        public ChunkName(string nintendoName, string readableName)
        {
            OriginalName = nintendoName;
            HumanName = readableName;
        }

    }


    [AttributeUsage(AttributeTargets.Field)]
    public class DisplayName : Attribute
    {
       
    }
}
