namespace SonOfPicasso.Testing.Common.Extensions
{
    public static class FakerSystemExtensions
    {
        public static string DirectoryPathWindows(this Bogus.DataSets.System system)
            => $@"c:\{system.Random.Word()}\{system.Random.Word()}";
    }
}