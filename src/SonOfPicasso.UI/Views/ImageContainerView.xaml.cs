using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        public event SelectionChangedEventHandler SelectionChanged;
        
        public ImageContainerView()
        {
            InitializeComponent();

            ListView.Events()
                .SelectionChanged
                .Subscribe(args => { SelectionChanged?.Invoke(this, args); });
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

        public void ClearSelection()
        {
            ListView.SelectedItems.Clear();
        }

        public void ClearSelection(IList<ImageViewModel> list)
        {
            foreach (var item in list) 
                ListView.SelectedItems.Remove(item);
        }
    }
}