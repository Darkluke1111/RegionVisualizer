using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;

namespace RegionVisualizer
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class RegionData
    {
        public IntDataMap2D dataMap;
        public string name;
        public int rX;
        public int rY;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class RegionDataRequest
    {
        public int rX = 1000;
        public int rY = 1000;
    }
}
