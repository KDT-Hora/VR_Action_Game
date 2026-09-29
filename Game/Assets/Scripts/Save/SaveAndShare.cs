using System.IO;
using VrAction.Core.Model;
using VrAction.Core.Serialization;

namespace VrAction.Game.Save
{
    /// <summary>Stores spatial data and progress locally so play can resume without rescanning (FR-019).</summary>
    public sealed class SaveStore
    {
        readonly string _path;
        public SaveStore(string path) { _path = path; }

        public void Save(SaveFile file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            File.WriteAllText(_path, SaveFileJson.Serialize(file));
        }

        /// <returns>null when nothing is saved.</returns>
        public SaveFile Load() => File.Exists(_path) ? SaveFileJson.Deserialize(File.ReadAllText(_path)) : null;

        public void Delete() { if (File.Exists(_path)) File.Delete(_path); }
    }

    /// <summary>Lets the player throw away the saved scan and scan again (FR-028).</summary>
    public sealed class RescanFlow
    {
        readonly SaveStore _store;
        public RescanFlow(SaveStore store) { _store = store; }
        public bool NeedsScan => _store.Load() == null;
        public void DiscardSavedScan() => _store.Delete();
    }

    /// <summary>Exports/imports generation conditions. Export includes room geometry, so it requires explicit consent (FR-025).</summary>
    public sealed class ShareService
    {
        readonly string _dir;
        public ShareService(string directory) { _dir = directory; }

        public string Export(GenerationRequest request, SpatialData space, bool consent)
        {
            string json = ShareFileJson.Export(request, space, consent); // throws without consent
            Directory.CreateDirectory(_dir);
            string path = Path.Combine(_dir, "share_" + request.Seed + "_" + request.StageType + ".json");
            File.WriteAllText(path, json);
            return path;
        }

        public ShareFile Import(string path) => ShareFileJson.Import(File.ReadAllText(path));
    }
}
