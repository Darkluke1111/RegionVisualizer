using Cairo;
using RegionVisualizer.MapLayer;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace RegionVisualizer
{
    public class RegionVisualizer : ModSystem
    {
        ICoreServerAPI sapi;
        ICoreClientAPI capi;
        WorldMapManager worldMapManager;

        public override void Start(ICoreAPI api)
        {
            api.Network
                .RegisterChannel("landformmapping")
                .RegisterMessageType(typeof(LandformMappingData));

            worldMapManager = api.ModLoader.GetModSystem<WorldMapManager>();
            worldMapManager.RegisterMapLayer<OceanRegionMapLayer>("ocean", -1);
            worldMapManager.RegisterMapLayer<LandformRegionMapLayer>("landform", -2);
            worldMapManager.RegisterMapLayer<AltitudeRegionMapLayer>("altitude", -3);

        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
        }

        private TextCommandResult OnRequestRegionData(TextCommandCallingArgs args)
        {
            IAsset asset = sapi.Assets.Get("game:worldgen/landforms.json");
            LandformsWorldProperty landforms = asset.ToObject<LandformsWorldProperty>();

            int quantityMutations = 0;

            for (int i = 0; i < landforms.Variants.Length; i++)
            {
                LandformVariant variant = landforms.Variants[i];
                variant.index = i;
                variant.Init(sapi.WorldManager, i);

                if (variant.Mutations != null)
                {
                    quantityMutations += variant.Mutations.Length;
                }
            }

            landforms.LandFormsByIndex = new LandformVariant[quantityMutations + landforms.Variants.Length];

            // Mutations get indices after the parent ones
            for (int i = 0; i < landforms.Variants.Length; i++)
            {
                landforms.LandFormsByIndex[i] = landforms.Variants[i];
            }

            int nextIndex = landforms.Variants.Length;
            for (int i = 0; i < landforms.Variants.Length; i++)
            {
                LandformVariant variant = landforms.Variants[i];
                if (variant.Mutations != null)
                {
                    for (int j = 0; j < variant.Mutations.Length; j++)
                    {
                        LandformVariant variantMut = variant.Mutations[j];

                        landforms.LandFormsByIndex[nextIndex] = variantMut;
                        nextIndex++;
                    }
                }
            }

            int[] colorMapping = new int[landforms.LandFormsByIndex.Length];
            string[] nameMapping = new string[landforms.LandFormsByIndex.Length];

            for (int i = 0; i < colorMapping.Length; i++)
            {
                colorMapping[i] = landforms.LandFormsByIndex[i].ColorInt;
                nameMapping[i] = landforms.LandFormsByIndex[i].Code;
            }

            sapi.Network.GetChannel("landformmapping").SendPacket(new LandformMappingData()
            {
                colorMapping = colorMapping, nameMapping = nameMapping
            }, (IServerPlayer)args.Caller.Player);
            

            return TextCommandResult.Success();
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            //api.Event.MapRegionGeneration(OnMapRegionGen, "standard");

            var parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create("getRegion").WithDescription("Requests Region Data for the current position").RequiresPlayer().RequiresPrivilege(Privilege.chat)
                .HandleWith(OnRequestRegionData);
            api.ChatCommands.Create("worldInfo").WithDescription("Desplays several world setting constants").RequiresPlayer().RequiresPrivilege(Privilege.chat).HandleWith(OnRequestWorldInfo);
        }

        private TextCommandResult OnRequestWorldInfo(TextCommandCallingArgs args)
        {
            StringBuilder sb = new StringBuilder();
            var chunksPerRegion = sapi.WorldManager.RegionSize / sapi.WorldManager.ChunkSize;
            sb.AppendLine("Chunk Size: " + sapi.WorldManager.ChunkSize + " blocks");
            sb.AppendLine("Region Size: " + sapi.WorldManager.RegionSize + " blocks  (" + chunksPerRegion + " chunks)");
            sb.AppendLine("World Size: " + sapi.WorldManager.MapSizeX + " blocks * " + sapi.WorldManager.MapSizeX + " blocks");
            sb.AppendLine("World Height: " + sapi.WorldManager.MapSizeY + " blocks");
            sb.AppendLine("World Seed: " + sapi.WorldManager.Seed);
            sb.AppendLine("Default Spawn: " + sapi.World.DefaultSpawnPosition.ToString());
            sb.AppendLine("Beach Map Scale: " + TerraGenConfig.beachMapScale + " pixels (" + TerraGenConfig.beachMapScale / chunksPerRegion + " pixels per chunk)");
            sb.AppendLine("Ocean Map Scale: " + TerraGenConfig.oceanMapScale + " pixels (" + TerraGenConfig.oceanMapScale / chunksPerRegion + " pixels per chunk)");
            sb.AppendLine("Forest Map Scale: " + TerraGenConfig.forestMapScale + " pixels (" + TerraGenConfig.forestMapScale / chunksPerRegion + " pixels per chunk)");
            sb.AppendLine("Landform Map Scale: " + TerraGenConfig.landformMapScale + " pixels (" + TerraGenConfig.landformMapScale / chunksPerRegion + " pixels per chunk)");
            sb.AppendLine("Ore Map Scale: " + TerraGenConfig.oreMapScale + " pixels (" + TerraGenConfig.oreMapScale / chunksPerRegion + " pixels per chunk)");
            sb.AppendLine("Upheaval Map Scale: " + TerraGenConfig.geoUpheavelMapScale + " pixels (" + TerraGenConfig.geoUpheavelMapScale / chunksPerRegion + " pixels per chunk)");
            return TextCommandResult.Success(sb.ToString());
        }


        public override double ExecuteOrder()
        {
            return 100;
        }

/*        private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams)
        {
            sapi.Network.GetChannel("regiondata").BroadcastPacket(new RegionData()
            {
                rX = regionX,
                rY = regionZ,
                data = mapRegion.OceanMap.Data,
            });
        }*/


    }
}