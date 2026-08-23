using log4net;
using SovomaLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;

namespace ZusiObjektAlbum.Miscellaneous
{
    [Serializable]
    public class KeepInMindItem : NotifyPropertyChangedBase
    {
        string _item;
        string _section;

        public string Item
        {
            get => _item;
            set
            {
                if (string.Compare(_item, value) != 0)
                {
                    _item = value;
                    OnPropertyChanged("Item");
                }
            }
        }

        public string Section
        {
            get => _section;
            set
            {
                if (string.Compare(_section, value) != 0)
                {
                    _section = value;
                    OnPropertyChanged("Item");
                }
            }
        }

        public override int GetHashCode()
        {
            return Item.ToLower().GetHashCode();
        }
    }

    [Serializable]
    class KeepInMind : ISerializable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(KeepInMind));
        private static KeepInMind _instance = null;

        private string _exportFile;
        private readonly Dictionary<int, KeepInMindItem> _files;
        private readonly Dictionary<int, KeepInMindItem> _folders;
        private readonly Dictionary<string, string> _maps;
        private bool _dirty;

        private class KeepInMindData
        {
            public Dictionary<int, KeepInMindItem> Files { get; set; }
            public Dictionary<int, KeepInMindItem> Folders { get; set; }
            public Dictionary<string, string> Maps { get; set; }
            public string ExportFile { get; set; }
        }

        //---------------------------------------------------------------------
        private static KeepInMind Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Birth();
                }
                return _instance;
            }
        }

        //---------------------------------------------------------------------
        public static string ExportFile
        {
            get => Instance._exportFile;
            set
            {
                Instance._exportFile = value;
                Instance._dirty = true;
            }
        }
        public static List<KeepInMindItem> Files => Instance._files.Values.ToList();
        public static List<KeepInMindItem> Folders => Instance._folders.Values.ToList();

        //---------------------------------------------------------------------
        private KeepInMind()
        {
            _files = new Dictionary<int, KeepInMindItem>();
            _folders = new Dictionary<int, KeepInMindItem>();
            _maps = new Dictionary<string, string>();
        }

        //---------------------------------------------------------------------
        protected KeepInMind(SerializationInfo info, StreamingContext context)
        {
            _files = (Dictionary<int, KeepInMindItem>)info.GetValue("Files", typeof(Dictionary<int, KeepInMindItem>));
            _folders = (Dictionary<int, KeepInMindItem>)info.GetValue("Folders", typeof(Dictionary<int, KeepInMindItem>));
            try
            {
                _maps = (Dictionary<string, string>)info.GetValue("Maps", typeof(Dictionary<string, string>));
            }
            catch
            {
                _maps = new Dictionary<string, string>();
            }
            try
            {
                _exportFile = info.GetString("ExportFile");
            }
            catch { }
        }

        //---------------------------------------------------------------------
        private static string GetFilePath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).EnsureTrailingBackslash() + @"ZusiObjektAlbum\KeepInMind.bin";
        }

        //---------------------------------------------------------------------
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Files", _files);
            info.AddValue("Folders", _folders);
            info.AddValue("Maps", _maps);
            info.AddValue("ExportFile", _exportFile);
        }

        //---------------------------------------------------------------------
        private void AddFile_impl(string file, string section)
        {
            int hash = file.ToLower().GetHashCode();
            if (!_files.ContainsKey(hash))
            {
                _files[hash] = new KeepInMindItem { Item = file, Section = section };
                _dirty = true;
            }
        }

        //---------------------------------------------------------------------
        private void AddFolder_impl(string folder, string section)
        {
            int hash = folder.ToLower().GetHashCode();
            if (!_folders.ContainsKey(hash))
            {
                _folders[hash] = new KeepInMindItem { Item = folder, Section = section };
                _dirty = true;
            }
        }

        //---------------------------------------------------------------------
        private void AddMapping_impl(string oldValue, string newValue)
        {
            string key = oldValue.ToLower();
            _maps[key] = newValue;
            _dirty = true;
        }

        //---------------------------------------------------------------------
        private string GetMappedValue_impl(string value)
        {
            string key = value.ToLower();
            return _maps.ContainsKey(key) ? _maps[key] : value;
        }

        //---------------------------------------------------------------------
        private void RemoveFile_impl(string file)
        {
            int hash = file.ToLower().GetHashCode();
            if (_files.ContainsKey(hash))
            {
                _files.Remove(hash);
                _dirty = true;
            }
        }

        //---------------------------------------------------------------------
        private void RemoveFolder_impl(string folder)
        {
            int hash = folder.ToLower().GetHashCode();
            if (_folders.ContainsKey(hash))
            {
                _folders.Remove(hash);
                _dirty = true;
            }
        }

        //---------------------------------------------------------------------
        private void Save_impl()
        {
            if (_dirty)
            {
                string filePath = GetFilePath();
                string folder = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(folder);

                try
                {
                    var data = new KeepInMindData { Files = _files, Folders = _folders, Maps = _maps, ExportFile = _exportFile };
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    string json = JsonSerializer.Serialize(data, options);
                    File.WriteAllText(filePath, json);
                    _dirty = false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }

        //---------------------------------------------------------------------
        private void SyncFolders_impl(IEnumerable<KeepInMindItem> collection)
        {
            _folders.Clear();
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    AddFolder_impl(item.Item, item.Section);
                }
            }
        }

        //---------------------------------------------------------------------
        public static void AddFile(string file, string section)
        {
            Instance.AddFile_impl(file, section);
        }

        //---------------------------------------------------------------------
        public static void AddFolder(string folder, string section)
        {
            Instance.AddFolder_impl(folder, section);
        }

        //---------------------------------------------------------------------
        public static void AddMapping(string oldValue, string newValue)
        {
            Instance.AddMapping_impl(oldValue, newValue);
        }

        //---------------------------------------------------------------------
        public static KeepInMind Birth()
        {
            string filePath = GetFilePath();
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var data = JsonSerializer.Deserialize<KeepInMindData>(json, options);
                    if (data != null)
                    {
                        var k = new KeepInMind();
                        if (data.Files != null)
                        {
                            foreach (var kv in data.Files)
                                k._files[kv.Key] = kv.Value;
                        }
                        if (data.Folders != null)
                        {
                            foreach (var kv in data.Folders)
                                k._folders[kv.Key] = kv.Value;
                        }
                        if (data.Maps != null)
                        {
                            foreach (var kv in data.Maps)
                                k._maps[kv.Key] = kv.Value;
                        }
                        k._exportFile = data.ExportFile;
                        k._dirty = false;
                        return k;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }

            return new KeepInMind();
        }

        //---------------------------------------------------------------------
        public static string GetMappedValue(string value)
        {
            return Instance.GetMappedValue_impl(value);
        }

        //---------------------------------------------------------------------
        public static void RemoveFile(string file)
        {
            Instance.RemoveFile_impl(file);
        }

        //---------------------------------------------------------------------
        public static void RemoveFolder(string folder)
        {
            Instance.RemoveFolder_impl(folder);
        }

        //---------------------------------------------------------------------
        public static void Save()
        {
            Instance.Save_impl();
        }

        //---------------------------------------------------------------------
        public static void SyncFolders(IEnumerable<KeepInMindItem> collection)
        {
            Instance.SyncFolders_impl(collection);
        }
    }
}
