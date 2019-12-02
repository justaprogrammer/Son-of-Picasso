using System;
using System.Windows;
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

        #region ImageWidth

        public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register(
            "ImageWidth", typeof(double), typeof(ImageContainerView), new PropertyMetadata(default(double)));

        public double ImageWidth
        {
            get => (double) GetValue(ImageWidthProperty);
            set => SetValue(ImageWidthProperty, value);
        }

        #endregion
    }
}