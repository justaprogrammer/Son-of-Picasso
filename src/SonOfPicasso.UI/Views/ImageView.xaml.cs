using System;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;
using SonOfPicasso.UI.Windows;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    ///     Interaction logic for ImageView.xaml
    /// </summary>
    public partial class ImageView : ReactiveUserControl<ImageViewModel>
    {
        public ImageView()
        {
            InitializeComponent();

            this.WhenAny(view => view.ViewModel, change => change.Value)
                .Skip(1)
                .SelectMany(model => model.GetBitmapSource())
                .ObserveOnDispatcher()
                .Subscribe(source => ImageControl.Source = source);
        }

        #region ImageSize

        public static readonly DependencyProperty ImageSizeProperty = DependencyProperty.Register(
            "ImageSize", typeof(double), typeof(ImageView), new PropertyMetadata(default(double)));

        public double ImageSize
        {
            get => (double) GetValue(ImageSizeProperty);
            set => SetValue(ImageSizeProperty, value);
        }

        #endregion
    }
}   