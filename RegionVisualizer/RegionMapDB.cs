using Microsoft.Data.Sqlite;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace RegionVisualizer
{
    [ProtoContract]
    public class RegionMapPieceDB
    {
        [ProtoMember(1)]
        public int[] Pixels;
    }

    public class RegionMapDB : SQLiteDBConnection
    {

        public override string DBTypeCode => "regionmap database";

        public RegionMapDB(ILogger logger) : base(logger)
        {
        }

        SqliteCommand setMapPieceCmd;
        SqliteCommand getMapPieceCmd;


        public override void OnOpened()
        {
            base.OnOpened();

            setMapPieceCmd = sqliteConn.CreateCommand();
            setMapPieceCmd.CommandText = "INSERT OR REPLACE INTO mappiece (position, data) VALUES (@pos, @data)";
            setMapPieceCmd.Parameters.Add("@pos", SqliteType.Integer, 1);
            setMapPieceCmd.Parameters.Add("@data", SqliteType.Blob);
            setMapPieceCmd.Prepare();

            getMapPieceCmd = sqliteConn.CreateCommand();
            getMapPieceCmd.CommandText = "SELECT data FROM mappiece WHERE position=@pos";
            getMapPieceCmd.Parameters.Add("@pos", SqliteType.Integer, 1);
            getMapPieceCmd.Prepare();
        }

        protected override void CreateTablesIfNotExists(SqliteConnection sqliteConn)
        {
            using (var sqlite_cmd = sqliteConn.CreateCommand())
            {
                sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS mappiece (position integer PRIMARY KEY, data BLOB);";
                sqlite_cmd.ExecuteNonQuery();
            }
        }

        public void Purge()
        {
            using var cmd = sqliteConn.CreateCommand();
            cmd.CommandText = "delete FROM mappiece";
            cmd.ExecuteNonQuery();
        }

        public RegionMapPieceDB[] GetMapPieces(List<Vec2i> chunkCoords)
        {
            var pieces = new RegionMapPieceDB[chunkCoords.Count];
            for (var i = 0; i < chunkCoords.Count; i++)
            {
                getMapPieceCmd.Parameters["@pos"].Value = chunkCoords[i].ToChunkIndex();
                using var sqlite_datareader = getMapPieceCmd.ExecuteReader();
                while (sqlite_datareader.Read())
                {
                    var data = sqlite_datareader["data"];
                    if (data == null) return null;

                    pieces[i] = SerializerUtil.Deserialize<RegionMapPieceDB>(data as byte[]);
                }
            }

            return pieces;
        }

        public RegionMapPieceDB GetMapPiece(Vec2i chunkCoord)
        {
            getMapPieceCmd.Parameters["@pos"].Value = chunkCoord.ToChunkIndex();

            using var sqlite_datareader = getMapPieceCmd.ExecuteReader();
            while (sqlite_datareader.Read())
            {
                object data = sqlite_datareader["data"];
                if (data == null) return null;

                return SerializerUtil.Deserialize<RegionMapPieceDB>(data as byte[]);
            }

            return null;
        }

        public void SetMapPieces(Dictionary<Vec2i, RegionMapPieceDB> pieces)
        {
            using var transaction = sqliteConn.BeginTransaction();
            setMapPieceCmd.Transaction = transaction;
            foreach (var val in pieces)
            {
                setMapPieceCmd.Parameters["@pos"].Value = val.Key.ToChunkIndex();
                setMapPieceCmd.Parameters["@data"].Value = SerializerUtil.Serialize(val.Value);
                setMapPieceCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }



        public override void Close()
        {
            setMapPieceCmd?.Dispose();
            getMapPieceCmd?.Dispose();

            base.Close();
        }


        public override void Dispose()
        {
            setMapPieceCmd?.Dispose();
            getMapPieceCmd?.Dispose();

            base.Dispose();
        }
    }
}
