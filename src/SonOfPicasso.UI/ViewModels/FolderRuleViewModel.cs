﻿using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class FolderRuleViewModel : ViewModelBase
    {
        private readonly ObservableCollectionExtended<FolderRuleViewModel> _manageFolderViewModels =
            new ObservableCollectionExtended<FolderRuleViewModel>();

        private IDirectoryInfo _directoryInfo;
        private FolderRuleActionEnum _manageFolderState;
        private IReadOnlyDictionary<string, FolderRuleActionEnum> _currentRules;

        public FolderRuleViewModel(
            ISchedulerProvider schedulerProvider,
            IDirectoryInfoPermissionsService directoryInfoPermissionsService,
            Func<FolderRuleViewModel> folderViewModelFactory,
            ViewModelActivator activator) : base(activator)
        {
            this.WhenActivated(disposable =>
            {
                Observable.Defer(() =>
                    {
                        return _directoryInfo
                            .EnumerateDirectories()
                            .ToObservable()
                            .Where(directoryInfoPermissionsService.IsReadable)
                            .Select(info =>
                            {
                                var folderViewModel = folderViewModelFactory();
                                folderViewModel.Initialize(info,_currentRules);
                                return folderViewModel;
                            });
                    })
                    .SubscribeOn(schedulerProvider.TaskPool)
                    .ObserveOn(schedulerProvider.MainThreadScheduler)
                    .Subscribe(folderViewModel =>
                    {
                        _manageFolderViewModels.Add(folderViewModel);
                    })
                    .DisposeWith(disposable);

                this.WhenAny(model => model.ManageFolderState, change => change.Value)
                    .ObserveOn(schedulerProvider.MainThreadScheduler)
                    .Subscribe(newState =>
                    {
                        foreach (var viewModel in Children)
                        {
                            viewModel.ManageFolderState = newState;
                        }
                    })
                    .DisposeWith(disposable);
            });
        }

        public FolderRuleActionEnum ManageFolderState
        {
            get => _manageFolderState;
            set => this.RaiseAndSetIfChanged(ref _manageFolderState, value);
        }

        public string FullName => _directoryInfo.FullName;

        public string Name => _directoryInfo.Name;

        public IObservableCollection<FolderRuleViewModel> Children => _manageFolderViewModels;

        public void Initialize(IDirectoryInfo directoryInfo,
            IReadOnlyDictionary<string, FolderRuleActionEnum> currentRules)
        {
            _directoryInfo = directoryInfo;

            if (currentRules.TryGetValue(_directoryInfo.FullName, out var currentFolderRule))
            {
                _manageFolderState = currentFolderRule;
            }
            else
            {
                _manageFolderState = FolderRuleActionEnum.Remove;
            }   
            
            _currentRules = currentRules;
        }
    }
}