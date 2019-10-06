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

            Func<ImageRowViewModel> imageRowViewModelFactory =
                () => new ImageRowViewModel(imageViewModelFactory, new ViewModelActivator());

            Func<ImageContainerViewModel> imageContainerViewModelFactory = () =>
                new ImageContainerViewModel(imageRowViewModelFactory, new ViewModelActivator(), TestSchedulerProvider);

            AutoSubstitute.Provide(imageViewModelFactory);
            AutoSubstitute.Provide(imageRowViewModelFactory);
            AutoSubstitute.Provide(imageContainerViewModelFactory);
        }

        protected void ActivateContainerViewModel(int rowCount, params ImageContainerViewModel[] imageContainerViewModels)
        {
            foreach (var imageContainerViewModel in imageContainerViewModels)
                imageContainerViewModel.Activator.Activate();

            TestSchedulerProvider.MainThreadScheduler.AdvanceBy(rowCount);

            foreach (var imageContainerViewModel in imageContainerViewModels)
            foreach (var imageRowViewModel in imageContainerViewModel.ImageRowViewModels)
            {
                imageRowViewModel.Activator.Activate();

                foreach (var imageViewModel in imageRowViewModel.ImageViewModels)
                    imageViewModel.Activator.Activate();
            }
        }
    }
}