using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;
using Serilog;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    ///     Interaction logic for ImageContainerListView.xaml
    /// </summary>
    public partial class ImageContainerListView : ReactiveUserControl<ICollectionView>
    {
        private readonly ILogger _logger;

        public ImageContainerListView()
        {
            InitializeComponent();

            _logger = Log.ForContext<ImageContainerListView>();

            this.WhenAny(view => view.ViewModel, change => change.Value)
                .Subscribe(collectionView => { ItemsControl.ItemsSource = collectionView; });

            this.WhenAny(view => view.DefaultImageSize, view => view.Zoom,
                    (change1, change2) => change1.Value * (change2.Value / 100))
                .Subscribe(d =>
                {
                    _logger.Verbose("Target Image Size {Value}", d);
                    ImageSize = d;
                });

            this.WhenAny<ImageContainerListView, (double imageWidth, double containerWidth), double, double>(
                    view => view.ImageSize,
                    view => view.ScrollViewer.ActualWidth,
                    (change1, change2) => (change1.Value, change2.Value))
                .Select(tuple => (int) (tuple.containerWidth / tuple.imageWidth))
                .Select(columns => Math.Max(1, columns))
                .Subscribe(columns => Columns = columns);
        }

        #region DefaultImageSize

        public static readonly DependencyProperty DefaultImageSizeProperty = DependencyProperty.Register(
            "DefaultImageSize", typeof(int), typeof(ImageContainerListView), new PropertyMetadata(default(int)));

        public int DefaultImageSize
        {
            get => (int) GetValue(DefaultImageSizeProperty);
            set => SetValue(DefaultImageSizeProperty, value);
        }

        #endregion

        #region ImageSize

        private static readonly DependencyPropertyKey ImageSizePropertyKey = DependencyProperty.RegisterReadOnly(
            "ImageSize", typeof(double), typeof(ImageContainerListView), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty ImageSizeProperty = ImageSizePropertyKey.DependencyProperty;

        public double ImageSize
        {
            get => (double) GetValue(ImageSizeProperty);
            set => SetValue(ImageSizePropertyKey, value);
        }

        #endregion

        #region Columns

        private static readonly DependencyPropertyKey ImageContainerColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "Columns", typeof(int), typeof(ImageContainerListView),
                new PropertyMetadata(default(int)));

        public static readonly DependencyProperty ColumnsProperty
            = ImageContainerColumnsPropertyKey.DependencyProperty;

        public int Columns
        {
            get => (int) GetValue(ColumnsProperty);
            protected set => SetValue(ImageContainerColumnsPropertyKey, value);
        }

        #endregion

        #region Zoom

        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
            "Zoom", typeof(double), typeof(ImageContainerListView), new PropertyMetadata(default(double)));

        public double Zoom
        {
            get => (double) GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        #endregion
    }
}