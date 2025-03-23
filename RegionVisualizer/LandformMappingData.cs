using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionVisualizer
{

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class LandformMappingData
    {
        public int[] colorMapping;
        public string[] nameMapping;
    }
}
