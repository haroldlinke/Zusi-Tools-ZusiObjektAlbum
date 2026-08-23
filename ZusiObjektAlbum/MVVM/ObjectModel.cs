using log4net;
using SovomaLib;
using SovomaLib.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using ZusiKlassenLib;
using ZusiKlassenLib.Landscape;

namespace ZusiObjektAlbum.MVVM
{
    public sealed class ObjectModel : BaseTreeViewViewModel<ObjectModel, ILandscapeObject>
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string[] _folderToExclude = new string[]
        {
            "_Docu",
            "Test",
            "Husum",
            "Husum0",
            "Husum1",
            "Marschbahn"
        };

        private bool _erroneous;
        private bool _readonly = true;

        //---------------------------------------------------------------------
        public static ObservableCollection<ObjectModel> Create()
        {
            List<ILandscapeObject> objects;
            ObjectModel root = new("root", 0);
            DataPathType dtp;

            // RailwayObjects
            dtp = DataPathType.Official;
            string path = Zusi.GetAbsolutePathOf("RailwayObjects\\", ref dtp);
            if (Directory.Exists(path))
            {
                objects = LandscapeObjects.EnumerateLandscapeObjects_(path, true);
                ObjectModel railwayObjects = new("RailwayObjects", 0);
                GroupObjects(railwayObjects, objects, path, 1);
                root.Children.Add(railwayObjects);
            }
#if false
            path = Zusi.ZusiAlternateDataPath + "RailwayObjects\\";
            if (Directory.Exists(path))
            {
                objects = LandscapeObjects.EnumerateLandscapeObjects(path, true);
                ObjectModel railwayObjects = new ObjectModel("RailwayObjects (B)", 0);
                GroupObjects(railwayObjects, objects, path, 1);
                root.Children.Add(railwayObjects);
            }
#endif

            // Catenary
            dtp = DataPathType.Official;
            path = Zusi.GetAbsolutePathOf("Catenary\\", ref dtp);
            if (Directory.Exists(path))
            {
                objects = LandscapeObjects.EnumerateLandscapeObjects_(path, true);
                ObjectModel catenaryObjects = new("Catenary", 0);
                GroupObjects(catenaryObjects, objects, path, 1);
                root.Children.Add(catenaryObjects);
            }
#if false
            path = Zusi.ZusiAlternateDataPath + "Catenary\\";
            if (Directory.Exists(path))
            {
                objects = LandscapeObjects.EnumerateLandscapeObjects(path, true);
                ObjectModel catenaryObjects = new ObjectModel("Catenary (B)", 0);
                GroupObjects(catenaryObjects, objects, path, 1);
                root.Children.Add(catenaryObjects);
            }
#endif

            // Streckenobjekte
            ObjectModel routeObjects = new("Streckenobjekte", 1);
            List<ObjectModel> tmpObjects = new();
            bool alternatePath = false;
            foreach (string p in new string[] { Zusi.DataPath[DataPathType.Official] })
            {
                path = p + "Routes\\";
                if (Directory.Exists(p))
                {
                    foreach (string folder in Directory.EnumerateDirectories(path, "Objekt*", SearchOption.AllDirectories))
                    {
                        string f = folder.StripPrefix(path);
                        string[] ss = f.Split('\\');

                        bool skip = false;
                        foreach (string s in ss)
                        {
                            if (IsBlacklisted(s))
                            {
                                skip = true;
                                break;
                            }
                        }
                        if (skip) continue;

                        int ix = 0;
                        for (; ix < ss.Length && !ss[ix].StartsWith("Objekt"); ix++) ;
                        if (ix >= ss.Length) continue;

                        f = ss[ix];
                        int n = f.IndexOf('_');
                        if (n > -1)
                        {
                            f = f.Substring(n + 1);
                        }
                        else
                        {
                            if (f.StartsWith("Objekte") && f.Length > 7)
                            {
                                f = f.Substring(7);
                            }
                            else
                            {
                                string[] tmp = ss[ix - 1].Split('_');
                                int iix = 0;
                                for (; iix < tmp.Length; iix++)
                                {
                                    if (!char.IsDigit(tmp[iix][0]))
                                    {
                                        StringBuilder sb = new();
                                        for (int i = iix; i < tmp.Length; i++)
                                        {
                                            if (sb.Length > 0)
                                            {
                                                sb.Append(' ');
                                            }
                                            sb.Append(tmp[i]);
                                        }
                                        f = sb.ToString();
                                        break;
                                    }
                                }
                            }
                        }

                        List<ILandscapeObject> list = (from ls3 in Directory.EnumerateFiles(folder, "*.lod.ls3", SearchOption.AllDirectories)
                                                       select new LandscapeObject(ls3)).ToList<ILandscapeObject>();
                        string displayName = f.ToProper();
                        if (alternatePath)
                        {
                            displayName += " (B)";
                        }
                        ObjectModel om = new(displayName, 2);
                        GroupObjects(om, list, folder.EnsureTrailingBackslash(), 2);
                        if (om.Children.Count > 0)
                        {
                            //routeObjects.Children.Add(om);
                            tmpObjects.Add(om);
                        }
                    }
                }
                alternatePath = true;
            }

#if false
            if (routeObjects.Children.Count > 0)
            {
                root.Children.Add(routeObjects);
            }
#else
            if (tmpObjects.Count > 0)
            {
                tmpObjects.Sort((x, y) => string.Compare(x.DisplayName, y.DisplayName));
                tmpObjects.ForEach(o => routeObjects.Children.Add(o));
                root.Children.Add(routeObjects);
            }
#endif

            root.Initialize();

            return root.Children;
        }

        //---------------------------------------------------------------------
        public static ObjectModel FindMyObjects(ObjectModel om)
        {
            if (om != null)
            {
                if (IsMyObjects(om))
                {
                    return om;
                }
                else
                {
                    ObjectModel o = FindMyObjects(om.Parent);
                    if (o != null)
                    {
                        return o;
                    }
                }
            }
            return null;
        }

        //---------------------------------------------------------------------
        public static bool IsMyObjects(ObjectModel om)
        {
            return string.Compare(om.DisplayName, "Eigene Objekte") == 0;
        }

        //---------------------------------------------------------------------
        private static void GroupObjects(ObjectModel omParent, List<ILandscapeObject> objects, string prefixPath, int level)
        {
#if MP_SPECIAL
            Log.DebugFormat("begin grouping, prefix is '{0}'", prefixPath);
#endif

            try
            {
                foreach (var g in objects.GroupBy(ls =>
                    {
                        string s = string.Empty;
                        if (ls is Landschaft l)
                        {
                            s = l.GetDocument().Filename.StripPrefix(prefixPath);
                        }
                        else if (ls is LandscapeObject lo)
                        {
                            s = lo.Filename.StripPrefix(prefixPath);
                        }
                        if (s.Contains('\\'))
                        {
                            string[] ss = s.Split(new char[] { '\\' });
                            return ss[0];
                        }
                        else
                        {
                            return ".";
                        }
                    })
                    .Select(grp => new { GroupID = grp.Key, Members = grp.ToList() }))
                {
                    List<ObjectModel> children = new();
                    ObjectModel group = null;

                    if (g.GroupID == ".")
                    {
                        g.Members.ForEach(obj =>
                        {
                            children.Add(new ObjectModel(obj));
                        });
                    }
                    else
                    {
                        group = new ObjectModel(g.GroupID.ToProper(), level);
                        string prefix = prefixPath + g.GroupID.EnsureTrailingBackslash();
                        GroupObjects(group, g.Members, prefix, level + 1);
                    }

                    if (group != null)
                    {
                        omParent.Children.Add(group);
                    }
                    children.ForEach(c => omParent.Children.Add(c));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        //---------------------------------------------------------------------
        private static bool IsBlacklisted(string s)
        {
            return _folderToExclude.Contains(s);
        }

        //---------------------------------------------------------------------
        public bool IsErroneous
        {
            get => _erroneous;
            set
            {
                if (_erroneous != value)
                {
                    _erroneous = value;
                    OnPropertyChanged("IsErroneous");
                }
            }
        }

        //---------------------------------------------------------------------
        public bool IsReadOnly
        {
            get => _readonly;
            set
            {
                if (_readonly != value)
                {
                    _readonly = value;
                    OnPropertyChanged("IsReadOnly");
                }
            }
        }

        //---------------------------------------------------------------------
        public ObjectModel(string folder, int level)
            : base(null, level <= 1, false)
        {
            _displayName = folder;
            IsBold = true;
        }

        //---------------------------------------------------------------------
        public ObjectModel(ILandscapeObject obj)
            : base(null, false, false)
        {
            if (obj is LandscapeObject lo)
            {
                _displayName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(lo.Filename)).ToProper();
            }
            else if (obj is Landschaft l)
            {
                _displayName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(l.GetDocument().Filename)).ToProper();
            }
            _object = obj;
        }
    }
}
