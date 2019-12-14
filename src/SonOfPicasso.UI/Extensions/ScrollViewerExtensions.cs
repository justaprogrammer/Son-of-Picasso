using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SonOfPicasso.UI.Extensions
{
    public static class ScrollViewerExtensions
    {
        public static ListViewItem GetFirstVisibleListViewItem<TDataContextType>(this ScrollViewer scrollViewer)
        {
            var listViewItems = scrollViewer
                .FindVisualChildren<ListViewItem>()
                .ToArray();

            ListViewItem lastListViewItem = null;

            Point lastViewViewItemPoint;
            foreach (var listViewItem in listViewItems)
            {
                if (listViewItem.DataContext.GetType() != typeof(TDataContextType))
                    continue;

                var translatePoint = listViewItem.TranslatePoint(new Point(), scrollViewer);

                if (translatePoint.Y <= 0)
                {
                    if (lastListViewItem == null || lastViewViewItemPoint.Y != translatePoint.Y)
                    {
                        lastListViewItem = listViewItem;
                        lastViewViewItemPoint = translatePoint;
                    }

                    continue;
                }

                if (lastListViewItem == null) lastListViewItem = listViewItem;

                break;
            }

            return lastListViewItem;
        }
    }
}