using System;
using System.Reactive.Disposables;

namespace SonOfPicasso.Core.Extensions
{
    internal static class DisposableExtensions
    {
        public static T DisposeWith<T>(this T item, CompositeDisposable compositeDisposable)
            where T : IDisposable
        {
            if (compositeDisposable == null)
            {
                throw new ArgumentNullException(nameof(compositeDisposable));
            }

            compositeDisposable.Add(item);
            return item;
        }
    }
}