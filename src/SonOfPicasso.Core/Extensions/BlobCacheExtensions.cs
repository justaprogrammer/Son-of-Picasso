using System;
using System.Reactive.Linq;
using Akavache;

namespace SonOfPicasso.Core.Extensions
{
    public static class BlobCacheExtensions
    {
        // https://github.com/reactiveui/Akavache/issues/186#issuecomment-59683874

        public static IObservable<(bool, byte[])> TryGet(this IBlobCache blobCache, string key)
        {
            return Observable.Create<(bool, byte[])>(observer =>
            {
                return blobCache.Get(key).Subscribe(
                    x => observer.OnNext((true, x)),
                    ex => observer.OnNext((false, default)),
                    observer.OnCompleted);
            });
        }

        public static IObservable<(bool, T)> TryGetObject<T>(this IBlobCache blobCache, string key)
        {
            return Observable.Create<(bool, T)>(observer =>
            {
                return blobCache.GetObject<T>(key).Subscribe(
                    x =>
                    {
                        observer.OnNext((true, x));
                        observer.OnCompleted();
                    },
                    ex =>
                    {
                        observer.OnNext((false, default));
                        observer.OnCompleted();
                    });
            });
        }
    }
}