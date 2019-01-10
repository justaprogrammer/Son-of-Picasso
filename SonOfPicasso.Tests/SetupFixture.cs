﻿using System;
using NUnit.Framework;

namespace SonOfPicasso.Tests
{
    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
        }
    }
}