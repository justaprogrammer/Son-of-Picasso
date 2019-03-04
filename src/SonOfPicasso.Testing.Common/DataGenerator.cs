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
            ImageFolderFaker = new AutoFaker<ImageFolderModel>()
                .RuleFor(folder => folder.Path, 
                    faker => faker.System.DirectoryPathWindows());
        }

        public static Faker<ImageFolderModel> ImageFolderFaker { get; }
    }
}