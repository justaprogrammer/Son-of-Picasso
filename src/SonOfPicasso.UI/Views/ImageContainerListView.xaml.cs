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

            this.WhenAny(view => view.DefaultImageWidth, view => view.Zoom,
                    (change1, change2) => change1.Value * (change2.Value / 100))
                .Subscribe(d =>
                {
                    _logger.Verbose("Target Image Width {Value}", d);
                    ImageWidth = d;
                });

            this.WhenAny<ImageContainerListView, (double imageWidth, double containerWidth), double, double>(
                    view => view.ImageWidth,
                    view => view.ScrollViewer.ActualWidth,
                    (change1, change2) => (change1.Value, change2.Value))
                .Select(tuple => (int) (tuple.containerWidth / tuple.imageWidth))
                .Select(columns => Math.Max(1, columns))
                .Subscribe(columns => Columns = columns);
        }

        #region DefaultImageWidth

        public static readonly DependencyProperty DefaultImageWidthProperty = DependencyProperty.Register(
            "DefaultImageWidth", typeof(int), typeof(ImageContainerListView), new PropertyMetadata(default(int)));

        public int DefaultImageWidth
        {
            get => (int) GetValue(DefaultImageWidthProperty);
            set => SetValue(DefaultImageWidthProperty, value);
        }

        #endregion

        #region ImageWidth

        private static readonly DependencyPropertyKey ImageWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            "ImageWidth", typeof(double), typeof(ImageContainerListView), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty ImageWidthProperty = ImageWidthPropertyKey.DependencyProperty;

        public double ImageWidth
        {
            get => (double) GetValue(ImageWidthProperty);
            set => SetValue(ImageWidthPropertyKey, value);
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