using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DynamicData.Binding;
using Serilog;
using ReactiveUI;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.FolderRules;
using ListView = System.Windows.Controls.ListView;

namespace SonOfPicasso.UI.Windows.Dialogs
{
    /// <summary>
    ///     Interaction logic for AddAlbumWindow.xaml
    /// </summary>
    public partial class FolderManagementWindow : ReactiveWindow<ManageFolderRulesViewModel>
    {
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ILogger _logger;

        public IImageProvider ImageProvider { get; }

        public FolderManagementWindow(ISchedulerProvider schedulerProvider,
            IImageProvider imageProvider,
            ILogger logger)
        {
            _schedulerProvider = schedulerProvider;
            _logger = logger;
            ImageProvider = imageProvider;
            
            InitializeComponent();

            this.WhenActivated(d =>
            {
                FoldersListView.ItemsSource = ViewModel.Folders;
                WatchedPathsList.ItemsSource = ViewModel.WatchedPaths;

                FoldersListView.Events()
                    .SelectedItemChanged
                    .Select(args => (FolderRuleViewModel) args.NewValue)
                    .BindTo(ViewModel, model => model.SelectedItem)
                    .DisposeWith(d);

                ViewModel.WhenAnyValue(model => model.SelectedItem)
                    .Subscribe(folderRuleViewModel =>
                    {
                        var folderRule = (FolderRule)WatchedPathsList.SelectedItem;
                        if (folderRule != null)
                        {
                            if (!folderRule.Path.Equals(folderRuleViewModel.Path))
                            {
                                WatchedPathsList.SelectedItem = null;
                            }
                        }
                    })
                    .DisposeWith(d);

                SelectedItemAlwaysRadioButton.Events()
                    .Click
                    .Subscribe(args =>
                    {
                        if (ViewModel.SelectedItem != null)
                            ViewModel.SelectedItem.FolderRuleAction = FolderRuleActionEnum.Always;
                    })
                    .DisposeWith(d);

                SelectedItemOnceRadioButton.Events()
                    .Click
                    .Subscribe(args =>
                    {
                        if (ViewModel.SelectedItem != null)
                            ViewModel.SelectedItem.FolderRuleAction = FolderRuleActionEnum.Once;
                    })
                    .DisposeWith(d);

                SelectedItemNeverRadioButton.Events()
                    .Click
                    .Subscribe(args =>
                    {
                        if (ViewModel.SelectedItem != null)
                            ViewModel.SelectedItem.FolderRuleAction = FolderRuleActionEnum.Remove;
                    })
                    .DisposeWith(d);

                WatchedPathsList.Events()
                    .SelectionChanged
                    .Subscribe(args =>
                    {
                        var listView = (ListView)args.Source;
                        var folderRule = (FolderRule)listView.SelectedValue;
                        if (folderRule != null && ViewModel.FolderRuleViewModelDictionary.TryGetValue(folderRule.Path, out var folderRuleViewModel))
                        {
                            var treeViewItem = FindItem(FoldersListView.ItemContainerGenerator, folderRuleViewModel.Path);
                            if (treeViewItem != null)
                            {
                                treeViewItem.IsSelected = true;
                                treeViewItem.BringIntoView();

                                Dispatcher.BeginInvoke(new Action(() => FoldersListView.Focus()));
                            }
                        }
                    })
                    .DisposeWith(d);


                this.OneWayBind(ViewModel,
                    model => model.SelectedItem,
                    window => window.SelectedItemAlwaysRadioButton.IsEnabled,
                    selector: model => model != null);

                this.OneWayBind(ViewModel,
                    model => model.SelectedItem,
                    window => window.SelectedItemNeverRadioButton.IsEnabled,
                    selector: model => model != null);

                this.OneWayBind(ViewModel,
                    model => model.SelectedItem,
                    window => window.SelectedItemOnceRadioButton.IsEnabled,
                    selector: model => model != null);

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

        private void TreeViewItem_OnExpanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = (TreeViewItem)sender;
            var customFolderRuleInput = (FolderRuleViewModel) treeViewItem.DataContext;

            foreach (var folderRuleInput in customFolderRuleInput.Children)
            {
                ViewModel.PopulateFolderRuleInput(folderRuleInput)
                    .Subscribe();
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