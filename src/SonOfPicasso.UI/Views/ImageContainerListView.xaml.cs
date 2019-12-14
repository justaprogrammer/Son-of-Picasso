using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using SonOfPicasso.UI.Extensions;
using SonOfPicasso.UI.ViewModels;

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

            var columns = ScrollViewer
                .WhenAny(
                    scrollViewer => scrollViewer.ViewportWidth,
                    change => change.Value)
                .Select(value => Math.Max(1, (int) (value / 310)));

            columns.Subscribe(i =>
            {
                _logger.Verbose("Columns {Columns}", columns);
            });

            Observable.Create<string>(observer =>
            {
                var disposable1 = this.WhenAny(view => view.ViewModel, change => change.Value)
                    .Skip(1)
                    .SelectMany(collectionView =>
                    {
                        return collectionView
                            .ObserveCollectionChanges()
                            .Select(pattern => (ICollectionView) pattern.Sender)
                            .Select(view => view.Cast<ImageContainerViewModel>().FirstOrDefault())
                            .Where(model => model != null)
                            .Select(model => model.ContainerKey)
                            .DistinctUntilChanged();
                    })
                    .Subscribe(observer.OnNext);

                var disposable2 = ScrollViewer
                    .WhenAny(
                        scrollViewer => scrollViewer.VerticalOffset,
                        observedChange1 => observedChange1.Value)
                    .Skip(1)
                    .DistinctUntilChanged(verticalOffset => (int) (verticalOffset / 30))
                    .Select(verticalOffset =>
                    {
                        var listViewItem = ScrollViewer.GetFirstVisibleListViewItem<ImageViewModel>();
                        var imageViewModel1 = (ImageViewModel) listViewItem?.DataContext;
                        return imageViewModel1?.ImageContainerViewModel;
                    })
                    .DistinctUntilChanged()
                    .Subscribe(model =>
                    {
                        if (disposable1 != null)
                        {
                            disposable1.Dispose();
                            disposable1 = null;
                        }

                        observer.OnNext(model?.ContainerKey);
                    });

                return new CompositeDisposable(disposable1, disposable2);
            }).BindTo(this, view => view.TopmostImageContainerKey);
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
            private set => SetValue(ImageSizePropertyKey, value);
        }

        #endregion

        public static readonly DependencyPropertyKey TopmostImageContainerKeyPropertyKey = DependencyProperty.RegisterReadOnly(
            "TopmostImageContainerKey", typeof(string), typeof(ImageContainerListView), new PropertyMetadata(default(string)));
        
        public static readonly DependencyProperty TopmostImageContainerKeyProperty = TopmostImageContainerKeyPropertyKey.DependencyProperty;

        public string TopmostImageContainerKey
        {
            get { return (string) GetValue(TopmostImageContainerKeyProperty); }
            private set { SetValue(TopmostImageContainerKeyPropertyKey, value); }
        }

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