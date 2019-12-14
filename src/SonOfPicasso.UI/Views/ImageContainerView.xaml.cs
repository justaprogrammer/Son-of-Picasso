using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    ///     Interaction logic for ImageContainerView.xaml
    /// </summary>
    public partial class ImageContainerView : ReactiveUserControl<ImageContainerViewModel>
    {
        public ImageContainerView()
        {
            InitializeComponent();

            ListView.MouseWheel += (sender, args) =>
            {
            };
        }

        #region Columns

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
            "Columns", typeof(int), typeof(ImageContainerView), new PropertyMetadata(default(int)));

        public int Columns
        {
            get => (int) GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        #endregion

        #region ImageSize

        public static readonly DependencyProperty ImageSizeProperty = DependencyProperty.Register(
            "ImageSize", typeof(double), typeof(ImageContainerView), new PropertyMetadata(default(double)));

        public double ImageSize
        {
            get => (double) GetValue(ImageSizeProperty);
            set => SetValue(ImageSizeProperty, value);
        }

        #endregion

        private void UniformG5rid_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                this.ListView.RaiseEvent(eventArg);
            }
        }
    }
}