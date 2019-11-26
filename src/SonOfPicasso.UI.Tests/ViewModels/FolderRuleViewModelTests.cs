using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.ViewModels
{
    public class FolderRuleViewModelTests : ViewModelTestsBase
    {
        public FolderRuleViewModelTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }
    }
}