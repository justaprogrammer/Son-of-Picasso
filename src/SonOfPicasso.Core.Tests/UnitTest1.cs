using System;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests
{
    public class UnitTest1: TestsBase<UnitTest1>
    {
        public UnitTest1(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void Test1()
        {

        }
    }
}
