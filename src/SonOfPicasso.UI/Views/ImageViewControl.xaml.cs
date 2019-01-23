using System;
using System.Reactive.Disposables;
using ReactiveUI;
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
        public ImageViewControl()
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                        vmProperty: model => model.Bitmap.Result,
                        viewProperty: window => window.ImageBitmap.Source,
                        selector: reference =>
                        {
                            if (reference.TryGetTarget(out var target))
                            {
                                return target;
                            }

                            return null;
                        })
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        model => model.Image.Path,
                        window => window.ImageLabel.Content)
                    .DisposeWith(disposable);
            });
        }
    }
}
