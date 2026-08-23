using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace ZusiObjektAlbum.MVVM
{
    public enum CameraPosition
    {
        Left,
        Right,
        Back,
        Front
    }

    public sealed class CameraData : DependencyObject
    {
        private static readonly DependencyPropertyKey _positionKey = DependencyProperty.RegisterReadOnly(
            "Position",
            typeof(Point3D),
            typeof(CameraData),
            new PropertyMetadata(new Point3D()));
        public static readonly DependencyProperty PositionProperty = _positionKey.DependencyProperty;
        public Point3D Position
        {
            get => (Point3D)GetValue(PositionProperty);
            private set => SetValue(_positionKey, value);
        }

        private static readonly DependencyPropertyKey _lookDirectionKey = DependencyProperty.RegisterReadOnly(
            "LookDirection",
            typeof(Vector3D),
            typeof(CameraData),
            new PropertyMetadata(new Vector3D()));
        public static readonly DependencyProperty LookDirectionProperty = _lookDirectionKey.DependencyProperty;
        public Vector3D LookDirection
        {
            get => (Vector3D)GetValue(LookDirectionProperty);
            private set => SetValue(_lookDirectionKey, value);
        }

        private static readonly DependencyPropertyKey _upDirectionKey = DependencyProperty.RegisterReadOnly(
            "UpDirection",
            typeof(Vector3D),
            typeof(CameraData),
            new PropertyMetadata(new Vector3D(0, 0, 1)));
        public static readonly DependencyProperty UpDirectionProperty = _upDirectionKey.DependencyProperty;
        public Vector3D UpDirection
        {
            get => (Vector3D)GetValue(UpDirectionProperty);
            private set => SetValue(_upDirectionKey, value);
        }

#if false
        public CameraData(Point3D position, double distance)
        {
            Position = position;
            LookDirection = new Vector3D(-position.X, -position.Y, -position.Z);
        }
#endif

        public CameraData(CameraPosition pos, double distance)
        {
            Point3D p = new Point3D();

            switch (pos)
            {
                case CameraPosition.Back:
                    p = new Point3D(-distance, 0, 0);
                    break;
                case CameraPosition.Front:
                    p = new Point3D(distance, 0, 0);
                    break;
                case CameraPosition.Left:
                    p = new Point3D(0, -distance, 0);
                    break;
                case CameraPosition.Right:
                    p = new Point3D(0, distance, 0);
                    break;
            }
            Position = p;
            LookDirection = new Vector3D(-p.X, -p.Y, -p.Z);
        }
    }
}
