using System;
using System.Collections.ObjectModel;

namespace Shared.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static ObservableCollection<T> InsertBefore<T>(this ObservableCollection<T> observable, Func<T, bool> match, T item)
        {
            // start at the end and work forwards
            var index = 0;
            for (; index < observable.Count && !match(observable[index]); index++);

            // insert it at this position
            observable.Insert(index, item);

            return observable;
        }
    }
}
