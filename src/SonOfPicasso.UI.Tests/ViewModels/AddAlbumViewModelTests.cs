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
    public class AddAlbumViewModelTests : TestsBase
    {
        public AddAlbumViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            Logger.Debug("CanInitialize");
            using (var autoSub = new AutoSubstitute())
            {
                var testSchedulerProvider = new TestSchedulerProvider();
                autoSub.Provide<ISchedulerProvider>(testSchedulerProvider);

                var addAlbumViewModel = autoSub.Resolve<AddAlbumViewModel>();
                addAlbumViewModel.AlbumNameRule.IsValid.Should().BeFalse();

                addAlbumViewModel.AlbumNameRule.ValidationChanged.Subscribe(validationState =>
                {
                    AutoResetEvent.Set();
                });

                addAlbumViewModel.AlbumName = "Hello";
                WaitOne();

                addAlbumViewModel.AlbumName = string.Empty;
                WaitOne();
            }
        }
    }
}
