﻿using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using ReactiveUI;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Windows.Dialogs
{
    /// <summary>
    ///     Interaction logic for AddAlbumWindow.xaml
    /// </summary>
    public partial class FolderManagementWindow : ReactiveWindow<ManageFolderRulesViewModel>
    {
        public IImageProvider ImageProvider { get; }

        public FolderManagementWindow(ISchedulerProvider schedulerProvider, IImageProvider imageProvider)
        {
            ImageProvider = imageProvider;
            
            InitializeComponent();

            this.WhenActivated(d =>
            {
                FoldersListView.ItemsSource = ViewModel.Folders;

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

        private void FoldersListView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ViewModel.SelectedItem = (FolderRuleViewModel) e.NewValue;
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
    }
}