using System;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.ViewModels;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class ViewModelTestsBase : UnitTestsBase
    {
        public ViewModelTestsBase(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            Func<ImageViewModel> imageViewModelFactory = () =>
                new ImageViewModel(AutoSubstitute.Resolve<IImageLoadingService>(), TestSchedulerProvider,
                    new ViewModelActivator());

            Func<ImageContainerViewModel> imageContainerViewModelFactory = () =>
                new ImageContainerViewModel(new ViewModelActivator(), TestSchedulerProvider);

            Func<TrayImageViewModel> trayImageViewModelFactory = () =>
                new TrayImageViewModel(new ViewModelActivator());

            AutoSubstitute.Provide(imageViewModelFactory);
            AutoSubstitute.Provide(imageContainerViewModelFactory);
            AutoSubstitute.Provide(trayImageViewModelFactory);
        }

        protected void ActivateContainerViewModel(int rowCount, params ImageContainerViewModel[] imageContainerViewModels)
        {
            foreach (var imageContainerViewModel in imageContainerViewModels)
                imageContainerViewModel.Activator.Activate();
        }
    }
}