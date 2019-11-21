using System;
using ReactiveUI;
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
            Func<TrayImageViewModel> trayImageViewModelFactory = () =>
                new TrayImageViewModel(new ViewModelActivator());

            AutoSubstitute.Provide(trayImageViewModelFactory);
        }
    }
}