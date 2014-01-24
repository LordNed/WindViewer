using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWActorEdit.Source.Util
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ChunkName : Attribute
    {
        private string _originalName; //Original name of chunk, ie: 2DMA or SCLS
        private string _humanName; //Human readable name, ie "Doors", etc.

        public ChunkName(string nintendoName, string readableName)
        {
            _originalName = nintendoName;
            _humanName = readableName;
        }

        public string OriginalName
        {
            get { return _originalName; }
            set { _originalName = value; }
        }

        public string HumanName
        {
            get { return _humanName; }
        }
    }


    [AttributeUsage(AttributeTargets.Field)]
    public class DisplayName : Attribute
    {
       
    }
}
