using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using VrAction.Core.Model;

namespace VrAction.Core.Serialization
{
    public static class SpatialDataJson
    {
        public static string Serialize(SpatialData d)
        {
            var kinds = new StringBuilder();
            foreach (var k in d.KindsCopy()) kinds.Append((int)k);
            var hz = new StringBuilder();
            foreach (var h in d.HazardsCopy()) hz.Append(h ? '1' : '0');
            var hs = new StringBuilder();
            var heights = d.HeightsCopy();
            for (int i = 0; i < heights.Length; i++)
            {
                if (i > 0) hs.Append(',');
                hs.Append(heights[i].ToString(CultureInfo.InvariantCulture));
            }
            return "{\"mode\":" + MiniJson.Quote(d.Mode.ToString())
                + ",\"cellSizeMm\":" + d.CellSizeMm
                + ",\"width\":" + d.Width
                + ",\"depth\":" + d.Depth
                + ",\"originXMm\":" + d.OriginXMm
                + ",\"originZMm\":" + d.OriginZMm
                + ",\"kinds\":\"" + kinds + "\""
                + ",\"heightsMm\":[" + hs + "]"
                + ",\"hazards\":\"" + hz + "\"}";
        }

        public static SpatialData Deserialize(string json) => FromObject(MiniJson.Obj(MiniJson.Parse(json)));

        public static SpatialData FromObject(Dictionary<string, object> o)
        {
            var mode = (PlayMode)Enum.Parse(typeof(PlayMode), MiniJson.Str(MiniJson.Get(o, "mode")));
            int cell = (int)MiniJson.Long(MiniJson.Get(o, "cellSizeMm"));
            int w = (int)MiniJson.Long(MiniJson.Get(o, "width"));
            int dp = (int)MiniJson.Long(MiniJson.Get(o, "depth"));
            int ox = (int)MiniJson.Long(MiniJson.Get(o, "originXMm"));
            int oz = (int)MiniJson.Long(MiniJson.Get(o, "originZMm"));
            string ks = MiniJson.Str(MiniJson.Get(o, "kinds"));
            string hz = MiniJson.Str(MiniJson.Get(o, "hazards"));
            var hl = MiniJson.Arr(MiniJson.Get(o, "heightsMm"));
            var kinds = new CellKind[ks.Length];
            for (int i = 0; i < ks.Length; i++) kinds[i] = (CellKind)(ks[i] - '0');
            var heights = new int[hl.Count];
            for (int i = 0; i < hl.Count; i++) heights[i] = (int)MiniJson.Long(hl[i]);
            var hazards = new bool[hz.Length];
            for (int i = 0; i < hz.Length; i++) hazards[i] = hz[i] == '1';
            return new SpatialData(mode, cell, w, dp, ox, oz, kinds, heights, hazards);
        }

        public static string Hash(SpatialData d)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(Serialize(d)));
                var sb = new StringBuilder("sha256:");
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }

    public sealed class ClearedStage
    {
        public ulong Seed { get; }
        public StageType StageType { get; }
        public ClearedStage(ulong seed, StageType type) { Seed = seed; StageType = type; }
    }

    public sealed class SaveFile
    {
        public SpatialData SpatialData { get; }
        public IReadOnlyList<ClearedStage> ClearedStages { get; }
        public int CharacterHeightCm { get; }
        public DifficultyMode Difficulty { get; }

        public SaveFile(SpatialData spatialData, IEnumerable<ClearedStage> cleared, int characterHeightCm, DifficultyMode difficulty)
        {
            SpatialData = spatialData;
            ClearedStages = new List<ClearedStage>(cleared).AsReadOnly();
            CharacterHeightCm = characterHeightCm;
            Difficulty = difficulty;
        }
    }

    public static class SaveFileJson
    {
        public const int Version = 1;

        public static string Serialize(SaveFile f)
        {
            var sb = new StringBuilder();
            sb.Append("{\"version\":").Append(Version);
            sb.Append(",\"spatialData\":").Append(SpatialDataJson.Serialize(f.SpatialData));
            sb.Append(",\"clearedStages\":[");
            for (int i = 0; i < f.ClearedStages.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"seed\":").Append(f.ClearedStages[i].Seed)
                  .Append(",\"stageType\":").Append(MiniJson.Quote(f.ClearedStages[i].StageType.ToString())).Append('}');
            }
            sb.Append("],\"settings\":{\"characterScaleCm\":").Append(f.CharacterHeightCm)
              .Append(",\"difficulty\":").Append(MiniJson.Quote(f.Difficulty.ToString())).Append("}}");
            return sb.ToString();
        }

        public static SaveFile Deserialize(string json)
        {
            var o = MiniJson.Obj(MiniJson.Parse(json));
            long v = MiniJson.Long(MiniJson.Get(o, "version"));
            if (v != Version) throw new FormatException("unsupported save version " + v);
            var sd = SpatialDataJson.FromObject(MiniJson.Obj(MiniJson.Get(o, "spatialData")));
            var cleared = new List<ClearedStage>();
            foreach (var item in MiniJson.Arr(MiniJson.Get(o, "clearedStages")))
            {
                var c = MiniJson.Obj(item);
                cleared.Add(new ClearedStage(MiniJson.ULong(MiniJson.Get(c, "seed")),
                    (StageType)Enum.Parse(typeof(StageType), MiniJson.Str(MiniJson.Get(c, "stageType")))));
            }
            var s = MiniJson.Obj(MiniJson.Get(o, "settings"));
            return new SaveFile(sd, cleared, (int)MiniJson.Long(MiniJson.Get(s, "characterScaleCm")),
                (DifficultyMode)Enum.Parse(typeof(DifficultyMode), MiniJson.Str(MiniJson.Get(s, "difficulty"))));
        }
    }

    public sealed class ShareFile
    {
        public GenerationRequest Request { get; }
        public SpatialData SpatialData { get; }
        public ShareFile(GenerationRequest request, SpatialData spatialData) { Request = request; SpatialData = spatialData; }
    }

    public static class ShareFileJson
    {
        public const int Version = 1;

        /// <summary>The share file contains room geometry, so the caller must confirm explicit consent first (FR-025).</summary>
        public static string Export(GenerationRequest req, SpatialData data, bool consent)
        {
            if (!consent) throw new InvalidOperationException("sharing requires explicit user consent");
            return "{\"version\":" + Version
                + ",\"seed\":" + req.Seed
                + ",\"stageType\":" + MiniJson.Quote(req.StageType.ToString())
                + ",\"difficulty\":" + MiniJson.Quote(req.Difficulty.ToString())
                + ",\"characterScaleCm\":" + req.CharacterHeightCm
                + ",\"spatialData\":" + SpatialDataJson.Serialize(data)
                + ",\"spatialDataHash\":" + MiniJson.Quote(SpatialDataJson.Hash(data)) + "}";
        }

        public static ShareFile Import(string json)
        {
            var o = MiniJson.Obj(MiniJson.Parse(json));
            long v = MiniJson.Long(MiniJson.Get(o, "version"));
            if (v != Version) throw new FormatException("unsupported share version " + v);
            var sd = SpatialDataJson.FromObject(MiniJson.Obj(MiniJson.Get(o, "spatialData")));
            if (SpatialDataJson.Hash(sd) != MiniJson.Str(MiniJson.Get(o, "spatialDataHash")))
                throw new FormatException("spatial data hash mismatch");
            var req = new GenerationRequest(
                MiniJson.ULong(MiniJson.Get(o, "seed")),
                (StageType)Enum.Parse(typeof(StageType), MiniJson.Str(MiniJson.Get(o, "stageType"))),
                (DifficultyMode)Enum.Parse(typeof(DifficultyMode), MiniJson.Str(MiniJson.Get(o, "difficulty"))),
                (int)MiniJson.Long(MiniJson.Get(o, "characterScaleCm")));
            return new ShareFile(req, sd);
        }
    }
}
