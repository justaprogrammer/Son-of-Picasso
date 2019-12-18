using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using SonOfPicasso.UI.Extensions;
using SonOfPicasso.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EventExtensions = System.Windows.EventExtensions;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    ///     Interaction logic for ImageContainerListView.xaml
    /// </summary>
    public partial class ImageContainerListView : ReactiveUserControl<ICollectionView>
    {
        public delegate void ImageSelectionChangedEventHandler(object sender, ImageSelectionChangedEventArgs e);

        private readonly ILogger _logger;
        private readonly Dictionary<string, ImageViewModel> _selectedImageViewModels;

        private ImageContainerView _lastSelectedImageContainerView;

        public ImageContainerListView()
        {
            InitializeComponent();

            _logger = Log.ForContext<ImageContainerListView>();

            EventExtensions.Events(ScrollViewer)
                .PreviewMouseWheel
                .Subscribe(e =>
                {
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = MouseWheelEvent;
                    eventArg.Source = e.Source;
                    ScrollViewer.RaiseEvent(eventArg);

                    e.Handled = true;
                });

            _selectedImageViewModels = new Dictionary<string, ImageViewModel>();

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
                    .Select(verticalOffset => GetFirstVisibleImageContainerView())
                    .DistinctUntilChanged()
                    .Subscribe(containerKey =>
                    {
                        if (disposable1 != null)
                        {
                            disposable1.Dispose();
                            disposable1 = null;
                        }

                        observer.OnNext(containerKey);
                    });

                return new CompositeDisposable(disposable1, disposable2);
            }).BindTo(this, view => view.TopmostImageContainerKey);
        }

        public event ImageSelectionChangedEventHandler ImageSelectionChanged;

        private void ImageContainerView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IList<ImageViewModel> addedItems = new List<ImageViewModel>(e.AddedItems.Cast<ImageViewModel>());
            IList<ImageViewModel> removedItems = new List<ImageViewModel>(e.RemovedItems.Cast<ImageViewModel>());

            var selectedImageContainerView = (ImageContainerView) sender;
            if (_lastSelectedImageContainerView == null || _lastSelectedImageContainerView != selectedImageContainerView)
            {
                removedItems.AddRange(_selectedImageViewModels.Values);
                _selectedImageViewModels.Clear();

                _lastSelectedImageContainerView?.ClearSelection();
                _lastSelectedImageContainerView = selectedImageContainerView;
            }

            foreach (var removedItem in e.RemovedItems.Cast<ImageViewModel>())
                _selectedImageViewModels.Remove(removedItem.ImageRefKey);

            foreach (var addedItem in e.AddedItems.Cast<ImageViewModel>())
                _selectedImageViewModels.Add(addedItem.ImageRefKey, addedItem);

            ImageSelectionChanged?.Invoke(this, new ImageSelectionChangedEventArgs(addedItems, removedItems));
        }

        #region TopmostImageContainerKey

        public static readonly DependencyPropertyKey TopmostImageContainerKeyPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "TopmostImageContainerKey", typeof(string), typeof(ImageContainerListView),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty TopmostImageContainerKeyProperty =
            TopmostImageContainerKeyPropertyKey.DependencyProperty;

        public string TopmostImageContainerKey
        {
            get => (string) GetValue(TopmostImageContainerKeyProperty);
            private set => SetValue(TopmostImageContainerKeyPropertyKey, value);
        }

        #endregion

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

        public void ClearSelectedItems(IList<ImageViewModel> list)
        {
            _lastSelectedImageContainerView?.ClearSelection(list);
        }

        public void ScrollToIndex(int index)
        {
            var imageContainerView
                = ItemsControl.FindVisualChildren<ImageContainerView>()
                    .Skip(index)
                    .First();

            var point = imageContainerView
                .TransformToAncestor(ItemsControl)
                .Transform(new Point());

            ScrollViewer.ScrollToVerticalOffset(point.Y);
        }

        public string GetFirstVisibleImageContainerView()
        {
            var imageContainerViews = ItemsControl
                .FindVisualChildren<ImageContainerView>()
                .ToArray();

            ImageContainerView result = null;

            foreach (var imageContainerView in imageContainerViews)
            {
                var translatePoint = imageContainerView
                    .TranslatePoint(new Point(), ScrollViewer);

                var listViewItemBottom = translatePoint.Y + imageContainerView.ActualHeight;

                if (listViewItemBottom <= 0)
                {
                    continue;
                }

                result = imageContainerView;
                break;
            }

            var imageContainerViewModel = (ImageContainerViewModel) result?.DataContext;
            var containerKey = imageContainerViewModel?.ContainerKey;
            
            return containerKey;
        }
    }
}