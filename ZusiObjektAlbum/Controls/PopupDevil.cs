using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZusiObjektAlbum.Controls
{
    public class PopupDevil : Control
    {
        public static readonly DependencyProperty DetailsProperty = DependencyProperty.Register(
            "Details",
            typeof(object),
            typeof(PopupDevil));
        public object Details
        {
            get => GetValue(DetailsProperty);
            set => SetValue(DetailsProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly RoutedEvent PleaseOpenPopupEvent = EventManager.RegisterRoutedEvent(
            "PleaseOpenPopup",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PopupDevil));

        //---------------------------------------------------------------------
        public static readonly RoutedEvent PleaseClosePopupEvent = EventManager.RegisterRoutedEvent(
            "PleaseClosePopup",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PopupDevil));

        //---------------------------------------------------------------------
        static PopupDevil()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupDevil), new FrameworkPropertyMetadata(typeof(PopupDevil)));
        }

        //---------------------------------------------------------------------
        public PopupDevil()
        {
            MouseEnter += PopupDevil_MouseEnter;
            MouseLeave += PopupDevil_MouseLeave;
        }

        //---------------------------------------------------------------------
        private void PopupDevil_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(PleaseOpenPopupEvent));
        }

        //---------------------------------------------------------------------
        private void PopupDevil_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(PleaseClosePopupEvent));
        }
    }
}
