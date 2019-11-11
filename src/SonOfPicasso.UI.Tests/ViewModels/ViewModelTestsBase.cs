using System;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
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

            Func<FolderRuleViewModel> folderRuleViewModelFactory = () => new FolderRuleViewModel(AutoSubstitute.Resolve<ISchedulerProvider>(),
                AutoSubstitute.Resolve<Func<FolderRuleViewModel>>(),
                new ViewModelActivator());

            AutoSubstitute.Provide(imageViewModelFactory);
            AutoSubstitute.Provide(imageContainerViewModelFactory);
            AutoSubstitute.Provide(trayImageViewModelFactory);
            AutoSubstitute.Provide(folderRuleViewModelFactory);
        }
    }
}