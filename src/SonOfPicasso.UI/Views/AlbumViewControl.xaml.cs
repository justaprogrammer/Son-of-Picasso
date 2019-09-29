using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for AlbumViewControl.xaml
    /// </summary>
    public partial class AlbumViewControl : ReactiveUserControl<AlbumViewModel>
    {
        public AlbumViewControl()
        {
            InitializeComponent();
        }
    }
}
