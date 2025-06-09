using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace RegionVisualizer.MapLayer
{
    internal class AltitudeRegionMapLayer : RegionMapLayer
    {
        public AltitudeRegionMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink) { }

        public override string Title => "Altitude";

        public override string LayerGroupCode => "altitude";

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
            IntDataMap2D data;
            region.ModMaps.TryGetValue("AltitudeMap", out data);

            return new RegionData
            {
                name = "altitude",
                rX = regionPos.X,
                rY = regionPos.Z,
                dataMap = data,
            };
        }
    }
}
