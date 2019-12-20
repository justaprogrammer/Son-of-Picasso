using System;
using System.Collections.Generic;
using System.Linq;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    public class ImageSelectionChangedEventArgs: EventArgs
    {
        public IList<ImageViewModel> RemovedItems { get; }
        public IList<ImageViewModel> AddedItems { get; }

        public ImageSelectionChangedEventArgs(IEnumerable<ImageViewModel> addedItems, IEnumerable<ImageViewModel> removedItems)
        {
            if (addedItems == null)
                throw new ArgumentNullException(nameof (addedItems));

            if (removedItems == null)
                throw new ArgumentNullException(nameof (removedItems));

            RemovedItems = removedItems.ToList();
            AddedItems = addedItems.ToList();
        }
    }
}