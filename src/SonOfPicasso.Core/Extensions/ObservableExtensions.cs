using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Nito.Mvvm;

namespace SonOfPicasso.Core.Extensions
{
    public static class ObservableExtensions
    {
        public static NotifyTask<T> ToNotifyTask<T>(this IObservable<T> observable)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();

            observable.Subscribe(taskCompletionSource.SetResult, 
                taskCompletionSource.SetException, 
                taskCompletionSource.SetCanceled);

            return NotifyTask.Create(taskCompletionSource.Task);
        }
    }
}
