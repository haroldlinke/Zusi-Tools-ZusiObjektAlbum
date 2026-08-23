using log4net;
using SovomaLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ZusiKlassenLib;
using ZusiKlassenLib.Common;
using ZusiKlassenLib.Landscape;
using ZusiObjektAlbum.Miscellaneous;

namespace ZusiObjektAlbum.MVVM
{
    public sealed class Zusi3DModel : DependencyObject
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ZusiDocumentBase _document;
        private readonly Rect3D _modelBounds;
        private readonly List<CameraData> _cameras = new();

        //---------------------------------------------------------------------
        private static readonly DependencyPropertyKey _hasAuthorsKey = DependencyProperty.RegisterReadOnly(
            "HasAuthors",
            typeof(bool),
            typeof(Zusi3DModel),
            new PropertyMetadata(false));
        public static readonly DependencyProperty HasAuthorsProperty = _hasAuthorsKey.DependencyProperty;
        public bool HasAuthors
        {
            get => (bool)GetValue(HasAuthorsProperty);
            private set => SetValue(_hasAuthorsKey, value);
        }

        //---------------------------------------------------------------------
        private static readonly DependencyPropertyKey _heightKey = DependencyProperty.RegisterReadOnly(
            "Height",
            typeof(double),
            typeof(Zusi3DModel),
            new PropertyMetadata(0.0));
        public static readonly DependencyProperty HeightProperty = _heightKey.DependencyProperty;
        public double Height
        {
            get => (double)GetValue(HeightProperty);
            private set => SetValue(_heightKey, value);
        }

        //---------------------------------------------------------------------
        private static readonly DependencyPropertyKey _lengthKey = DependencyProperty.RegisterReadOnly(
            "Length",
            typeof(double),
            typeof(Zusi3DModel),
            new PropertyMetadata(0.0));
        public static readonly DependencyProperty LengthProperty = _lengthKey.DependencyProperty;
        public double Length
        {
            get => (double)GetValue(LengthProperty);
            private set => SetValue(_lengthKey, value);
        }

        //---------------------------------------------------------------------
        private static readonly DependencyPropertyKey _modelKey = DependencyProperty.RegisterReadOnly(
            "Model",
            typeof(Model3D),
            typeof(Zusi3DModel),
            new PropertyMetadata(null));
        public static readonly DependencyProperty ModelProperty = _modelKey.DependencyProperty;
        public Model3D Model
        {
            get => (Model3D)GetValue(ModelProperty);
            private set => SetValue(_modelKey, value);
        }

        //---------------------------------------------------------------------
        private static readonly DependencyPropertyKey _nameKey = DependencyProperty.RegisterReadOnly(
            "Name",
            typeof(string),
            typeof(Zusi3DModel),
            new PropertyMetadata(null));
        public static readonly DependencyProperty NameProperty = _nameKey.DependencyProperty;
        public string Name
        {
            get => (string)GetValue(NameProperty);
            private set => SetValue(_nameKey, value);
        }

        //---------------------------------------------------------------------
        private static readonly DependencyPropertyKey _objectModelKey = DependencyProperty.RegisterReadOnly(
            "ObjectModel",
            typeof(ObjectModel),
            typeof(Zusi3DModel),
            new PropertyMetadata(null));
        public static readonly DependencyProperty ObjectModelProperty = _objectModelKey.DependencyProperty;
        public ObjectModel ObjectModel
        {
            get => (ObjectModel)GetValue(ObjectModelProperty);
            private set => SetValue(_objectModelKey, value);
        }

        //---------------------------------------------------------------------
        private static readonly DependencyPropertyKey _transformKey = DependencyProperty.RegisterReadOnly(
            "Transform",
            typeof(Transform3D),
            typeof(Zusi3DModel),
            new PropertyMetadata(null));
        public static readonly DependencyProperty TransformProperty = _transformKey.DependencyProperty;
        public Transform3D Transform
        {
            get => (Transform3D)GetValue(TransformProperty);
            private set => SetValue(_transformKey, value);
        }

        //---------------------------------------------------------------------
        private static readonly DependencyPropertyKey _warningLevelKey = DependencyProperty.RegisterReadOnly(
            "WarningLevel",
            typeof(int),
            typeof(Zusi3DModel),
            new PropertyMetadata(0));
        public static readonly DependencyProperty WarningLevelProperty = _warningLevelKey.DependencyProperty;
        public int WarningLevel
        {
            get => (int)GetValue(WarningLevelProperty);
            private set => SetValue(_warningLevelKey, value);
        }

        //---------------------------------------------------------------------
        private static readonly DependencyPropertyKey _widthKey = DependencyProperty.RegisterReadOnly(
            "Width",
            typeof(double),
            typeof(Zusi3DModel),
            new PropertyMetadata(0.0));
        public static readonly DependencyProperty WidthProperty = _widthKey.DependencyProperty;
        public double Width
        {
            get => (double)GetValue(WidthProperty);
            private set => SetValue(_widthKey, value);
        }

        //---------------------------------------------------------------------
        public List<AutorEintrag> Authors { get => _document.Info.AutorEintraege; }

        //---------------------------------------------------------------------
        public List<CameraData> Cameras { get => _cameras; }

        //---------------------------------------------------------------------
        public LoDInfo LoD0 { get; private set; }

        //---------------------------------------------------------------------
        public LoDInfo LoD1 { get; private set; }

        //---------------------------------------------------------------------
        public LoDInfo LoD2 { get; private set; }

        //---------------------------------------------------------------------
        public LoDInfo LoD3 { get; private set; }

        //---------------------------------------------------------------------
        public Zusi3DModel(ObjectModel objectModel)
        {
            System.Diagnostics.Debug.Assert(objectModel.Object is Landschaft, $"Wrong object type: {objectModel.Object.GetType()}");

            ObjectModel = objectModel;
            Name = objectModel.DisplayName;

            Landschaft ls = objectModel.Object as Landschaft;
            _document = ls.GetDocument();
            HasAuthors = _document.Info.AutorEintraege.Count > 0;

            ObjectInfo objectInfo = ls.GetObjectInfo();
            LoD0 = objectInfo[0];
            LoD1 = objectInfo[1];
            LoD2 = objectInfo[2];
            LoD3 = objectInfo[3];

            if (LoD3 == null)
            {
                if (LoD2 == null)
                {
                    WarningLevel = 2;
                }
                else
                {
                    WarningLevel = 1;
                }
            }
            else
            {
                WarningLevel = 0;
            }

            try
            {
                Model3D model = ls.CreateModel();
                _modelBounds = model.Bounds;
                Length = _modelBounds.SizeX;
                Width = _modelBounds.SizeY;
                Height = _modelBounds.SizeZ;

                double mx = _modelBounds.MidX();
                double my = _modelBounds.MidY();
                double mz = _modelBounds.MidZ();

                double f = 0.71 / Math.Tan(Math2D.Radians(46));

                double distanceZ = _modelBounds.SizeZ * f;

                // Kamera linke/rechte Seite
                double distance1 = _modelBounds.SizeX * f + _modelBounds.SizeY * 0.5;
                double distance2 = distanceZ + _modelBounds.SizeY * 0.5;
                double distanceLR = Math.Max(distance1, distance2);
                _cameras.Add(new CameraData(CameraPosition.Left, distanceLR));
                _cameras.Add(new CameraData(CameraPosition.Right, distanceLR));

                // Kamera Vorder-/Rückseite
                distance1 = _modelBounds.SizeY * f + _modelBounds.SizeX * 0.5;
                distance2 = distanceZ + _modelBounds.SizeX * 0.5;
                double distanceFB = Math.Max(distance1, distance2);
                _cameras.Add(new CameraData(CameraPosition.Back, distanceFB));
                _cameras.Add(new CameraData(CameraPosition.Front, distanceFB));

                // Rundum
                _cameras.Add(new CameraData(CameraPosition.Front, Math.Max(distanceLR, distanceFB)));

                // Transform
                Transform = new TranslateTransform3D(-mx, -my, -mz);

                // Model
                Model3DGroup m3dg = new();
                m3dg.Children.Add(model);
                if (DataManager.Instance.HasGroundplate)
                {
                    m3dg.Children.Add(BuildGroundplate(_modelBounds));
                }
                Model = m3dg;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                objectModel.IsErroneous = true;
            }
        }

        //---------------------------------------------------------------------
        public void AddGroundplate()
        {
            if (Model is Model3DGroup m3dg)
            {
                if (m3dg.Children.Count < 2)
                {
                    m3dg.Children.Add(BuildGroundplate(_modelBounds));
                }
            }
        }

        //---------------------------------------------------------------------
        public void RemoveGroundplate()
        {
            if (Model is Model3DGroup m3dg)
            {
                if (m3dg.Children.Count == 2)
                {
                    m3dg.Children.RemoveAt(1);
                }
            }
        }

        //---------------------------------------------------------------------
        private Model3D BuildGroundplate(Rect3D bounds)
        {
            double x = Math.Ceiling(bounds.SizeX) * 5;
            double y = Math.Ceiling(bounds.SizeY) * 5;

            MeshGeometry3D mg3d = new();

            mg3d.Positions.Add(new System.Windows.Media.Media3D.Point3D(x, y, -0.02));
            mg3d.Positions.Add(new System.Windows.Media.Media3D.Point3D(x, -y, -0.02));
            mg3d.Positions.Add(new System.Windows.Media.Media3D.Point3D(-x, -y, -0.02));
            mg3d.Positions.Add(new System.Windows.Media.Media3D.Point3D(-x, y, -0.02));

            mg3d.TriangleIndices.Add(0);
            mg3d.TriangleIndices.Add(3);
            mg3d.TriangleIndices.Add(1);

            mg3d.TriangleIndices.Add(3);
            mg3d.TriangleIndices.Add(2);
            mg3d.TriangleIndices.Add(1);

            Material m = new DiffuseMaterial(Brushes.ForestGreen);

            return new GeometryModel3D(mg3d, m);
        }
    }
}
