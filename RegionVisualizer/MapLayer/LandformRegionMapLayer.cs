using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RegionVisualizer.MapLayer
{
    internal class LandformRegionMapLayer : RegionMapLayer
    {
        int[] colorMapping;
        string[] nameMapping;

        public LandformRegionMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
        {
            if (api.Side == EnumAppSide.Client)
            {
                ((ICoreClientAPI)api).Network.GetChannel("landformmapping").SetMessageHandler<LandformMappingData>(OnMappingMessage);
            }

        }

        private void OnMappingMessage(LandformMappingData packet)
        {
            colorMapping = packet.colorMapping;
            nameMapping = packet.nameMapping;
        }

        public override string Title => "Landform";

        public override string LayerGroupCode => "landform";

        public override int RegionMapPixelSize => 32;

        public override int RGBAfromInt(int value)
        {
            if (colorMapping != null && value >= 0 && value < colorMapping.Length)
            {
                return colorMapping[value] & ~(128<<24);
            }
            return ColorUtil.BlackArgb;
        }

        public override string HoverInfoFromInt(int value)
        {
            string name = "-";
            string hexColor = "-";
            if (nameMapping != null && value >= 0 && value < nameMapping.Length)
            {
                name = nameMapping[value];
            }

            if (colorMapping != null && value >= 0 && value < colorMapping.Length)
            {
                hexColor = ColorUtil.Int2Hex(colorMapping[value]);
            }
            return name + "(" + value + ") [" + hexColor + "]";
        }

        public override RegionData fetchRegionData(Vec3i regionPos, IMapRegion region)
        {
            return new RegionData
            {
                name = "landform",
                rX = regionPos.X,
                rY = regionPos.Z,
                dataMap = region.LandformMap,
            };
        }
    }
}
