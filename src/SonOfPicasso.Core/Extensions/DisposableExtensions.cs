using System;
using System.Reactive.Disposables;

namespace SonOfPicasso.Core.Extensions
{
    internal static class DisposableExtensions
    {
        public static void DisposeWith(this IDisposable disposable, CompositeDisposable compositeDisposable)
        {
            if (compositeDisposable == null) throw new ArgumentNullException(nameof(compositeDisposable));
            compositeDisposable.Add(disposable);
        }
    }
}