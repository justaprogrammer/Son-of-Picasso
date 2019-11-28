using System;
using System.Collections.Generic;
using FluentAssertions;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Data.Tests.Repository
{
    public class UnitOfWorkTests : DataTestsBase
    {
        public UnitOfWorkTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanSaveAndGetById()
        {
            var directory = new Folder
            {
                Images = null,
                Path = Faker.System.DirectoryPathWindows()
            };

            using (var unitOfWork = CreateUnitOfWork())
            {
                unitOfWork.FolderRepository.Insert(directory);
                unitOfWork.Save();
            }

            using (var unitOfWork = CreateUnitOfWork())
            {
                var dircopy = unitOfWork.FolderRepository.GetById(directory.Id);
            }
        }
    }
}