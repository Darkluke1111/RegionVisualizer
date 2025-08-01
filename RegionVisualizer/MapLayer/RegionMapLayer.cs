﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RegionVisualizer.MapLayer
{
    public abstract class RegionMapLayer : RGBMapLayer
    {
        ICoreClientAPI capi;

        public ConcurrentDictionary<Vec2i, RegionMapComponent> loadedMapData = new ConcurrentDictionary<Vec2i, RegionMapComponent>();
        ICoreServerAPI sapi;

        public RegionMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
        {
            this.mapSink = mapSink;

            if(api.Side == EnumAppSide.Server)
            {
                ICoreServerAPI sapi = api as ICoreServerAPI;
                this.sapi = sapi;

                sapi.ChatCommands.Create("getRegion" + Title).WithDescription("Requests Region Data for the current position").RequiresPlayer().RequiresPrivilege(Privilege.chat)
                .HandleWith(OnGetRegionCommand);

                sapi.Event.MapRegionLoaded += OnRegionLoaded;
            } else
            {
                ICoreClientAPI capi = api as ICoreClientAPI;
                this.capi = capi;
            }
        }

        public override void OnMapOpenedServer(IServerPlayer fromPlayer)
        {
            SendRegionData(fromPlayer, sapi.WorldManager.AllLoadedMapRegions);
        }

        private void OnRegionLoaded(Vec2i mapCoord, IMapRegion region)
        {
            Vec3d worldPos = new Vec3d(mapCoord.X, 0, mapCoord.Y) * sapi.WorldManager.RegionSize;
            var closePlayers = sapi.World.AllOnlinePlayers;
            var data = new Dictionary<long, IMapRegion>();
            data.Add(sapi.WorldManager.MapRegionIndex2D(mapCoord.X, mapCoord.Y), region);
            foreach( var player in closePlayers)
            {
                SendRegionData(player as IServerPlayer, data);
            }
            
        }

        public TextCommandResult OnGetRegionCommand(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player as IServerPlayer;
            SendRegionData(player, sapi.WorldManager.AllLoadedMapRegions);
            return TextCommandResult.Success();
        }

        public void SendRegionData(IServerPlayer toPlayer, Dictionary<long,IMapRegion> regions)
        {
            List<RegionData> dataList = new List<RegionData>();
            foreach (var region in regions)
            {
                var regionPos = sapi.WorldManager.MapRegionPosFromIndex2D(region.Key);
                var regionData = fetchRegionData(regionPos, region.Value);
                if (regionData != null && regionData.dataMap != null)
                {
                    dataList.Add(regionData);
                }

            }
            mapSink.SendMapDataToClient(this, toPlayer, SerializerUtil.Serialize(dataList));
        }

        public override MapLegendItem[] LegendItems => throw new NotImplementedException();
        public override EnumMinMagFilter MinFilter => EnumMinMagFilter.Linear;
        public override EnumMinMagFilter MagFilter => EnumMinMagFilter.Nearest;
        public override EnumMapAppSide DataSide => EnumMapAppSide.Server;

        public override bool RequireChunkLoaded => true;

        public override void OnDataFromServer(byte[] data)
        {
            var dataList = SerializerUtil.Deserialize<List<RegionData>>(data);
            foreach (var packet in dataList)
            {
                Vec2i pos = new Vec2i(packet.rX, packet.rY);
                RegionMapComponent regionMapComponent;
                if (!loadedMapData.TryGetValue(pos, out regionMapComponent))
                {
                    regionMapComponent = new RegionMapComponent(capi, pos, this);
                }

                regionMapComponent.setRegion(packet.dataMap);
                loadedMapData[pos] = regionMapComponent;
            }

        }

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
            int dX = x % (chunkSize * chunksPerRegion) * RegionMapPixelSize / (chunkSize * chunksPerRegion);
            int dZ = z % (chunkSize * chunksPerRegion) * RegionMapPixelSize / (chunkSize * chunksPerRegion);

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


        public abstract RegionData fetchRegionData(Vec3i regionPos, IMapRegion region);
        public abstract int RegionMapPixelSize { get; }
        public abstract int RGBAfromInt(int value);
        public abstract string HoverInfoFromInt(int value);
    }


}
