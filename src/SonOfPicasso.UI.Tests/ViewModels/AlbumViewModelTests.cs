using System;
using Autofac.Extras.NSubstitute;
using FluentAssertions;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Scheduling;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class AlbumViewModelTests : TestsBase
    {
        public AlbumViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanActivate()
        {
            using (var autoSub = new AutoSubstitute())
            {
                var testSchedulerProvider = new TestSchedulerProvider();
                autoSub.Provide<ISchedulerProvider>(testSchedulerProvider);

                var albumViewModel = autoSub.Resolve<AlbumViewModel>();
                albumViewModel.Activator.Activate();
            }
        }
    }
}
