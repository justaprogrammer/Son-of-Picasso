using System.Collections.Generic;
using AutoBogus;
using Bogus;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common.Extensions;

namespace SonOfPicasso.Testing.Common
{
    public static class FakerProfiles
    {
        public static readonly Faker<Directory> FakeNewDirectory
            = new AutoFaker<Directory>().RuleFor(directory1 => directory1.Id, 0)
                .RuleFor(directory1 => directory1.Images, (List<Image>)null)
                .RuleFor(directory1 => directory1.Path, faker => faker.System.DirectoryPathWindows());

        public static readonly Faker<Directory> FakeDirectory
            = new AutoFaker<Directory>()
                .RuleFor(directory1 => directory1.Images, (List<Image>)null)
                .RuleFor(directory1 => directory1.Path, faker => faker.System.DirectoryPathWindows());
    }
}