//#define VARIABLE_GOAROUNDTIME

using SovomaLib.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZusiObjektAlbum.Miscellaneous;
using ZusiObjektAlbum.MVVM;

namespace ZusiObjektAlbum.Controls
{
    public class ObjectViewControl : Control
    {
#if VARIABLE_GOAROUNDTIME
        private Slider _slider;
        private DoubleAnimation _currentRotationAnimation;
        private Viewport3D _viewport;
#else
        private DoubleAnimation _animation;
#endif
        private readonly DispatcherTimer _longClickTimer = new DispatcherTimer();
        private Point _startPoint;
        private Cursor _defaultCursor = Cursors.None;

        //---------------------------------------------------------------------
        public static readonly DependencyProperty AmbientLightProperty = DependencyProperty.Register(
            "AmbientLight",
            typeof(Color),
            typeof(ObjectViewControl),
            new PropertyMetadata(Colors.AliceBlue));
        public Color AmbientLight
        {
            get => (Color)GetValue(AmbientLightProperty);
            set => SetValue(AmbientLightProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty AnimationTimeProperty = DependencyProperty.Register(
            "AnimationTime",
            typeof(double),
            typeof(ObjectViewControl),
            new PropertyMetadata(10.0));
        public double AnimationTime
        {
            get => (double)GetValue(AnimationTimeProperty);
            set => SetValue(AnimationTimeProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty CameraProperty = DependencyProperty.Register(
            "Camera",
            typeof(CameraData),
            typeof(ObjectViewControl),
            new PropertyMetadata(null));
        public CameraData Camera
        {
            get => (CameraData)GetValue(CameraProperty);
            set => SetValue(CameraProperty, value);
        }

#if VARIABLE_GOAROUNDTIME
        public static readonly DependencyProperty GoAroundTimeProperty = DependencyProperty.Register(
            "GoAroundTime",
            typeof(double),
            typeof(ObjectViewControl),
            new PropertyMetadata(10.0, OnGoAroundTimeChanged));
        public double GoAroundTime
        {
            get => (double)GetValue(GoAroundTimeProperty);
            set => SetValue(GoAroundTimeProperty, value);
        }
#endif

        //---------------------------------------------------------------------
        public static readonly DependencyProperty IsAnimatedProperty = DependencyProperty.Register(
            "IsAnimated",
            typeof(bool),
            typeof(ObjectViewControl),
            new PropertyMetadata(false, OnIsAnimatedChanged));
        public bool IsAnimated
        {
            get => (bool)GetValue(IsAnimatedProperty);
            set => SetValue(IsAnimatedProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty IsAnimationActiveProperty = DependencyProperty.Register(
            "IsAnimationActive",
            typeof(bool),
            typeof(ObjectViewControl),
            new PropertyMetadata(false, OnIsAnimationActiveChanged, OnCoerceIsAnimationActive));
        public bool IsAnimationActive
        {
            get => (bool)GetValue(IsAnimationActiveProperty);
            set => SetValue(IsAnimationActiveProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
            "Model",
            typeof(Model3D),
            typeof(ObjectViewControl),
            new PropertyMetadata(null, OnModelChanged));
        public Model3D Model
        {
            get => (Model3D)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty RotationAngleYProperty = DependencyProperty.Register(
            "RotationAngleY",
            typeof(double),
            typeof(ObjectViewControl),
            new PropertyMetadata(0.0));
        public double RotationAngleY
        {
            get => (double)GetValue(RotationAngleYProperty);
            set => SetValue(RotationAngleYProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty RotationAngleZProperty = DependencyProperty.Register(
            "RotationAngleZ",
            typeof(double),
            typeof(ObjectViewControl),
            new PropertyMetadata(0.0));
        public double RotationAngleZ
        {
            get => (double)GetValue(RotationAngleZProperty);
            set => SetValue(RotationAngleZProperty, value);
        }

#if VARIABLE_GOAROUNDTIME
        public static readonly DependencyProperty ShowVelocitySliderProperty = DependencyProperty.Register(
            "ShowVelocitySlider",
            typeof(bool),
            typeof(ObjectViewControl),
            new PropertyMetadata(false));
        public bool ShowVelocitySlider
        {
            get => (bool)GetValue(ShowVelocitySliderProperty);
            set => SetValue(ShowVelocitySliderProperty, value);
        }
#endif

        //---------------------------------------------------------------------
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title",
            typeof(string),
            typeof(ObjectViewControl),
            new PropertyMetadata(null));
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty TitleBackgroundProperty = DependencyProperty.Register(
            "TitleBackground",
            typeof(Brush),
            typeof(ObjectViewControl),
            new PropertyMetadata(Brushes.Brown));
        public Brush TitleBackground
        {
            get => (Brush)GetValue(TitleBackgroundProperty);
            set => SetValue(TitleBackgroundProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty TransformProperty = DependencyProperty.Register(
            "Transform",
            typeof(Transform3D),
            typeof(ObjectViewControl),
            new PropertyMetadata(null));
        public Transform3D Transform
        {
            get => (Transform3D)GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        //---------------------------------------------------------------------
        static ObjectViewControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectViewControl), new FrameworkPropertyMetadata(typeof(ObjectViewControl)));
        }

        //---------------------------------------------------------------------
        public ObjectViewControl()
        {
            _longClickTimer.Interval = TimeSpan.FromMilliseconds(SysInfo.DoubleClickTime * 2);
            _longClickTimer.Tick += LongClickHappen;

            Loaded += (s, e) => BeginAnimation();
            MouseLeftButtonDown += ObjectViewControl_MouseLeftButtonDown;
            MouseLeftButtonUp += ObjectViewControl_MouseLeftButtonUp;
            MouseDoubleClick += ObjectViewControl_MouseDoubleClick;
            MouseMove += ObjectViewControl_MouseMove;
        }

        //---------------------------------------------------------------------
        public void EndAnimation()
        {
            BeginAnimation(RotationAngleZProperty, null);
            _animation = null;
        }

        //---------------------------------------------------------------------
        private void ObjectViewControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (IsAnimated && !IsAnimationActive)
            {
                RotationAngleZ = 0;
                RotationAngleY = 0;
            }
        }

        //---------------------------------------------------------------------
        private void ObjectViewControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsAnimated && !IsAnimationActive && e.LeftButton == MouseButtonState.Pressed)
            {
                Point pos = e.GetPosition(null);
                Vector v = _startPoint - pos;
                RotationAngleZ += v.X;
                RotationAngleY += v.Y;

                _startPoint = pos;
            }
        }

        //---------------------------------------------------------------------
        private void ObjectViewControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsAnimated)
            {
                _startPoint = e.GetPosition(null);
                if (IsAnimationActive)
                {
                    _longClickTimer.Start();
                }
                else
                {
                    SetHandCursor();
                }
            }
        }

        //---------------------------------------------------------------------
        private void ObjectViewControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsAnimated)
            {
                _longClickTimer.Stop();
                ResetCursor();
            }
        }

        //---------------------------------------------------------------------
        private void LongClickHappen(object sender, EventArgs e)
        {
            _longClickTimer.Stop();
            IsAnimationActive = false;
            SetHandCursor();
        }

#if VARIABLE_GOAROUNDTIME
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _slider = Template?.FindName("PART_Slider", this) as Slider;
            _slider.MouseDoubleClick += (s, e) => _slider.Value = 1;

            _viewport = Template?.FindName("PART_Viewport", this) as Viewport3D;
            System.Diagnostics.Debug.Assert(_viewport != null, "Mising PART_Viewport");
        }
#endif

        //---------------------------------------------------------------------
        private void SetHandCursor()
        {
            if (_defaultCursor == Cursors.None)
            {
                _defaultCursor = Cursor;
            }
            Cursor = Cursors.Hand;
        }

        //---------------------------------------------------------------------
        private void ResetCursor()
        {
            if (_defaultCursor != Cursors.None)
            {
                Cursor = _defaultCursor;
            }
        }

        //---------------------------------------------------------------------
        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ObjectViewControl)?.OnModelChanged((Model3D)e.NewValue);
        }

        //---------------------------------------------------------------------
        private void OnModelChanged(Model3D value)
        {
            EndAnimation();
            if (value != null)
            {
                BeginAnimation();
            }
        }

#if VARIABLE_GOAROUNDTIME
        private static void OnGoAroundTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ObjectViewControl)?.OnGoAroundTimeChanged((double)e.NewValue);
        }

        private void OnGoAroundTimeChanged(double value)
        {
            //StopAnimation();
            StartAnimation();
        }
#endif

        //---------------------------------------------------------------------
        private static void OnIsAnimatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ObjectViewControl)?.OnIsAnimatedChanged((bool)e.NewValue);
        }

        //---------------------------------------------------------------------
        private void OnIsAnimatedChanged(bool value)
        {
            IsAnimationActive = value;
        }

        //---------------------------------------------------------------------
        private static object OnCoerceIsAnimationActive(DependencyObject d, object baseValue)
        {
            return (d as ObjectViewControl)?.OnCoerceIsAnimationActive((bool)baseValue);
        }

        //---------------------------------------------------------------------
        private bool OnCoerceIsAnimationActive(bool baseValue)
        {
            return IsAnimated && baseValue;
        }

        //---------------------------------------------------------------------
        private static void OnIsAnimationActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ObjectViewControl)?.OnIsAnimationActiveChanged((bool)e.NewValue);
        }

        //---------------------------------------------------------------------
        private void OnIsAnimationActiveChanged(bool value)
        {
            if (value)
            {
                BeginAnimation();
            }
            else
            {
                EndAnimation();
            }
        }

#if VARIABLE_GOAROUNDTIME
#else
        //---------------------------------------------------------------------
        private void BeginAnimation()
        {
            if (IsAnimated && Model != null)
            {
                RotationAngleY = 0;

                if (_animation == null)
                {
                    _animation = new DoubleAnimation(360, new Duration(TimeSpan.FromSeconds(AnimationTime)))
                    {
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    BeginAnimation(RotationAngleZProperty, _animation);
                }
            }
        }
#endif
    }
}
