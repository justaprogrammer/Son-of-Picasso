using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using ReactiveUI;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels;
using SonOfPicasso.UI.ViewModels.FolderRules;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;

namespace SonOfPicasso.UI.Windows.Dialogs
{
    /// <summary>
    ///     Interaction logic for AddAlbumWindow.xaml
    /// </summary>
    public partial class FolderManagementWindow : ReactiveWindow<ManageFolderRulesViewModel>
    {
        private readonly ISchedulerProvider _schedulerProvider;
        public IImageProvider ImageProvider { get; }

        public FolderManagementWindow(ISchedulerProvider schedulerProvider, IImageProvider imageProvider)
        {
            _schedulerProvider = schedulerProvider;
            ImageProvider = imageProvider;
            
            InitializeComponent();

            this.WhenActivated(d =>
            {
                FoldersListView.ItemsSource = ViewModel.Folders;
                WatchedPathsList.ItemsSource = ViewModel.WatchedPaths;

                FoldersListView.Events()
                    .SelectedItemChanged
                    .Select(args => (FolderRuleViewModel) args.NewValue)
                    .BindTo(ViewModel, model => model.SelectedItem);

                this.BindCommand(ViewModel,
                        model => model.Continue,
                        window => window.OkButton)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                        model => model.Cancel,
                        window => window.CancelButton)
                    .DisposeWith(d);

                this.Bind(ViewModel,
                    model => model.HideUnselected,
                    window => window.DisplaySelectedItemsCheckbox.IsChecked);

                this.OneWayBind(ViewModel,
                    model => model.SelectedItem.FolderRuleAction,
                    window => window.SelectedItemAlwaysRadioButton.IsChecked,
                    manageFolderStateEnum => manageFolderStateEnum == FolderRuleActionEnum.Always);

                this.OneWayBind(ViewModel,
                    model => model.SelectedItem.FolderRuleAction,
                    window => window.SelectedItemNeverRadioButton.IsChecked,
                    manageFolderStateEnum => manageFolderStateEnum == FolderRuleActionEnum.Remove);

                this.OneWayBind(ViewModel,
                    model => model.SelectedItem.FolderRuleAction,
                    window => window.SelectedItemOnceRadioButton.IsChecked,
                    manageFolderStateEnum => manageFolderStateEnum == FolderRuleActionEnum.Once);

                ViewModel.CancelInteraction.RegisterHandler(context =>
                    {
                        return Observable.Defer(() =>
                        {
                            context.SetOutput(Unit.Default);
                            DialogResult = false;
                            Close();
                            return Observable.Return(Unit.Default);
                        }).SubscribeOn(schedulerProvider.MainThreadScheduler);
                    })
                    .DisposeWith(d);

                ViewModel.ContinueInteraction.RegisterHandler(context =>
                    {
                        return Observable.Defer(() =>
                        {
                            context.SetOutput(Unit.Default);
                            DialogResult = true;
                            Close();
                            return Observable.Return(Unit.Default);
                        }).SubscribeOn(schedulerProvider.MainThreadScheduler);
                    })
                    .DisposeWith(d);
            });
        }

        private void SelectedItemNeverRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedItem.FolderRuleAction = FolderRuleActionEnum.Remove;
        }

        private void SelectedItemOnceRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedItem.FolderRuleAction = FolderRuleActionEnum.Once;
        }

        private void SelectedItemAlwaysRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedItem.FolderRuleAction = FolderRuleActionEnum.Always;
        }

        private void TreeViewItem_OnExpanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = (TreeViewItem)sender;
            var customFolderRuleInput = (FolderRuleViewModel) treeViewItem.DataContext;
            foreach (var folderRuleInput in customFolderRuleInput.Children)
            {
                ViewModel.PopulateFolderRuleInput(folderRuleInput);
            }
        }

        private void WatchedPathsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = (ListView)sender;
            var folderRule = (FolderRule)listView.SelectedValue;
            if (ViewModel.FolderRuleViewModelDictionary.TryGetValue(folderRule.Path, out var folderRuleViewModel))
            {
                var treeViewItem = FindItem(FoldersListView.ItemContainerGenerator, folderRuleViewModel.Path);
                if (treeViewItem != null)
                {
                    treeViewItem.IsSelected = true;
                    treeViewItem.BringIntoView();

                    Dispatcher.BeginInvoke(new Action(() => FoldersListView.Focus()));
                }
            }
        }

        private static TreeViewItem FindItem(ItemContainerGenerator itemContainerGenerator, string path)
        {
            var folderRuleViewModel = itemContainerGenerator
                .Items
                .Cast<FolderRuleViewModel>()
                .Select<FolderRuleViewModel, (FolderRuleViewModel model, bool isPath, bool startWithPath)>(model => (
                    model,
                    path == model.DirectoryInfo.FullName,
                    path.StartsWith(model.DirectoryInfo.FullName)))
                .FirstOrDefault(tuple => tuple.Item2 || tuple.Item3);

            if (folderRuleViewModel != default)
            {
                var containerFromItem = (TreeViewItem) itemContainerGenerator.ContainerFromItem(folderRuleViewModel.model);
                if (folderRuleViewModel.isPath)
                {
                    return containerFromItem;
                }

                return FindItem(containerFromItem.ItemContainerGenerator, path);
            }

            return null;
        }
    }
}