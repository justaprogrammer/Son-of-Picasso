using AutoBogus;
using Bogus;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Testing.Common.Extensions;

namespace SonOfPicasso.Testing.Common
{
    public static class DataGenerator
    {
        static DataGenerator()
        {
            ImageFolderFaker = new AutoFaker<ImageFolder>()
                .RuleFor(folder => folder.Path, 
                    faker => faker.System.DirectoryPathWindows());
        }

        public static Faker<ImageFolder> ImageFolderFaker { get; }
    }
}