using RegionVisualizer.MapLayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RegionVisualizer
{
    public class RegionMapComponent : MapComponent
    {
        public int regionSize = 16 * 16;
        public float renderZ = 50;
        public Vec2i regionCoord;
        public LoadedTexture texture;
        Vec3d worldPos;
        Vec2f viewPos = new Vec2f();
        int chunkSize = GlobalConstants.ChunkSize;
        int chunksPerRegion = 16; //Should always be 16 (I think?)
        int regionMapSize;
        public IntDataMap2D mapData;
        RegionMapLayer layer;

        public RegionMapComponent(ICoreClientAPI capi, Vec2i regionCoord, RegionMapLayer mapLayer) : base(capi)
        {
            this.regionCoord = regionCoord;
            this.layer = mapLayer;
            
            worldPos = new Vec3d(regionCoord.X * chunkSize * chunksPerRegion, 0, regionCoord.Y * chunkSize * chunksPerRegion);
        }

        public void setRegion(IntDataMap2D dataMap)
        {
            mapData = dataMap;
            regionMapSize = dataMap.InnerSize;
            if (texture == null || texture.Disposed)
            {
                texture = new LoadedTexture(capi, 0, dataMap.InnerSize, dataMap.InnerSize);
            }
            int[] stripped =  new int[dataMap.InnerSize * dataMap.InnerSize];
            for (int z = 0; z < dataMap.InnerSize; z++)
            {
                for (int x = 0; x < dataMap.InnerSize; x++)
                {
                    int regionMapValue = dataMap.GetUnpaddedInt(x, z);
                    stripped[z * dataMap.InnerSize + x] = layer.RGBAfromInt(regionMapValue);
                }
            };
            capi.Render.LoadOrUpdateTextureFromRgba(stripped, false, 0, ref texture);

            capi.Render.BindTexture2d(texture.TextureId);
            capi.Render.GlGenerateTex2DMipmaps();
        }

        public override void Render(GuiElementMap map, float dt)
        {
            if (worldPos == null)
            {
                throw new ArgumentNullException("worldPos is null");
            }

            map.TranslateWorldPosToViewPos(worldPos, ref viewPos);

            capi.Render.Render2DTexture(
                texture.TextureId,
                (int)(map.Bounds.renderX + viewPos.X),
                (int)(map.Bounds.renderY + viewPos.Y),
                ((int)(texture.Width * map.ZoomLevel)) * (chunkSize * chunksPerRegion)/regionMapSize,
                ((int)(texture.Height * map.ZoomLevel)) * (chunkSize * chunksPerRegion)/regionMapSize,
                renderZ
            );
        }

        public override void Dispose()
        {
            base.Dispose();

        }

        public void ActuallyDispose()
        {
            texture.Dispose();
        }
    }
}
