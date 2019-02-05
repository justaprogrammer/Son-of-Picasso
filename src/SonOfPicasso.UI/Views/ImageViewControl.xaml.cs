using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Nito.Mvvm;
using ReactiveUI;
using SonOfPicasso.Core.Extensions;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels;
using Splat;

namespace SonOfPicasso.UI.Views
{
    public class WeakReferenceBitmap : IBindingTypeConverter
    {
        public int GetAffinityForObjects(Type fromType, Type toType)
        {
            throw new NotImplementedException();
        }

        public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaction logic for ImageViewControl.xaml
    /// </summary>
    public partial class ImageViewControl : ReactiveUserControl<IImageViewModel>
    {
        public ImageViewControl(ISchedulerProvider schedulerProvider)
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                this.OneWayBind(ViewModel,
                        model => model.Image,
                        window => window.ImageLabel.Content,
                        image => image.Path)
                    .DisposeWith(disposable);

                this.ViewModel.GetImage()
                    .ObserveOn(schedulerProvider.MainThreadScheduler)
                    .Subscribe(bitmap =>
                    {
                        ImageBitmap.Source = bitmap.ToNative();
                    });
            });
        }
    }
}
