namespace SonOfPicasso.Testing.Common.Extensions
{
    public static class FakerSystemExtensions
    {
        public static string DirectoryPathWindows(this Bogus.DataSets.System system, int depth = 2, string drive = "c")
            => $@"{drive}:\{string.Join(@"\", system.Random.WordsArray(depth))}";
    }
}