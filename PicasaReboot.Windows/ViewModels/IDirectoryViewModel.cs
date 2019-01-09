namespace SonOfPicasso.Windows.ViewModels
{
    public interface IDirectoryViewModel
    {
        string Name { get; }
        ReactiveList<IImageViewModel> Images { get; }
    }
}