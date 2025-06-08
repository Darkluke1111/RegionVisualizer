using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace RegionVisualizer.MapLayer
{
    internal class OceanRegionMapLayer : RegionMapLayer
    {
        public OceanRegionMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink) { }

        public override string Title => "Ocean";

        public override string LayerGroupCode => "ocean";

        public override int RegionMapPixelSize => 16;

        public override int RGBAfromInt(int value)
        {
            return ColorUtil.ColorFromRgba(value, value, value, 128);
        }

        public override string HoverInfoFromInt(int value)
        {
            return value.ToString();
        }

        public override RegionData fetchRegionData(Vec3i regionPos, IMapRegion region)
        {
            return new RegionData
            {
                name = "ocean",
                rX = regionPos.X,
                rY = regionPos.Z,
                dataMap = region.OceanMap,
            };
        }
    }
}
