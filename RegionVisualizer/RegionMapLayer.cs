using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RegionVisualizer
{
    public abstract class RegionMapLayer : RGBMapLayer
    {
        ICoreClientAPI capi;

        public ConcurrentDictionary<Vec2i, RegionMapComponent> loadedMapData = new ConcurrentDictionary<Vec2i, RegionMapComponent>();
        IWorldMapManager mapSink;

        public RegionMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
        {
            this.mapSink = mapSink;
            if (api.Side == EnumAppSide.Client) { 
                capi = (ICoreClientAPI)api;
                capi.Network
                    .GetChannel(RegionMap())
                    .SetMessageHandler<RegionData>(OnRegionData);
            }
        }

        public abstract string RegionMap();
        public abstract int RegionMapPixelSize();
        public abstract int RGBAfromInt(int value);
        public abstract string HoverInfoFromInt(int value);

        private void OnRegionData(RegionData packet)
        {
            Vec2i pos = new Vec2i(packet.rX, packet.rY);
            RegionMapComponent regionMapComponent;
            if (!loadedMapData.TryGetValue(pos, out regionMapComponent)) { 
                regionMapComponent = new RegionMapComponent(capi, pos, this);
            }
             
            regionMapComponent.setRegion(packet.dataMap);
            loadedMapData[pos] = regionMapComponent;

        }

        public override MapLegendItem[] LegendItems => throw new NotImplementedException();
        public override EnumMinMagFilter MinFilter => EnumMinMagFilter.Linear;
        public override EnumMinMagFilter MagFilter => EnumMinMagFilter.Nearest;
        public override EnumMapAppSide DataSide => EnumMapAppSide.Client;




        public override void Render(GuiElementMap mapElem, float dt)
        {
            if (!Active) return;

            foreach (var val in loadedMapData)
            {
                val.Value.Render(mapElem, dt);
            }
        }

        public override void OnShutDown()
        {
            MultiChunkMapComponent.tmpTexture?.Dispose();
            //mapdb?.Dispose();
        }

        public override void Dispose()
        {
            if (loadedMapData != null)
            {
                foreach (RegionMapComponent cmp in loadedMapData.Values)
                {
                    cmp?.ActuallyDispose();
                }
            }

            MultiChunkMapComponent.DisposeStatic();

            base.Dispose();
        }

        public int getMapDataAt(Vec3d worldPos)
        {
            int chunkSize = GlobalConstants.ChunkSize;
            int chunksPerRegion = 16;
            int x = (int) worldPos.X;
            int z = (int)worldPos.Z;
            int rX = x / (chunkSize * chunksPerRegion);
            int rZ = z / (chunkSize * chunksPerRegion);
            int dX = x % (chunkSize * chunksPerRegion) * RegionMapPixelSize() / (chunkSize * chunksPerRegion);
            int dZ = z % (chunkSize * chunksPerRegion) * RegionMapPixelSize() / (chunkSize * chunksPerRegion);

            if (!loadedMapData.TryGetValue(new Vec2i(rX, rZ), out var value)) return -1;

            return value.mapData.GetUnpaddedInt(dX, dZ);
        }

        public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {
            Vec3d worldPos = new Vec3d();
            double x = args.X - mapElem.Bounds.absX;
            double y = args.Y - mapElem.Bounds.absY;
            mapElem.TranslateViewPosToWorldPos(new Vec2f((float)x,(float) y), ref worldPos);
            
            //mapElem.Api.Logger.Debug("Getting info from " + worldPos.Clone().Sub(capi.World.DefaultSpawnPosition.AsBlockPos));
            hoverText.AppendLine(Title + ": " + HoverInfoFromInt(getMapDataAt(worldPos)));
            base.OnMouseMoveClient(args, mapElem, hoverText);
        }
    }


}
