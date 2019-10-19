using System;
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
    public class ManageFolderRulesViewModel : ViewModelBase
    {
        private readonly ObservableCollectionExtended<ManageFolderRulesViewModel> _manageFolderViewModels =
            new ObservableCollectionExtended<ManageFolderRulesViewModel>();

        private IDirectoryInfo _directoryInfo;
        private FolderRuleActionEnum _manageFolderState;

        public ManageFolderRulesViewModel(
            ISchedulerProvider schedulerProvider,
            IDirectoryInfoPermissionsService directoryInfoPermissionsService,
            Func<ManageFolderRulesViewModel> folderViewModelFactory,
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
                                folderViewModel.Initialize(info, ManageFolderState);
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

        public IObservableCollection<ManageFolderRulesViewModel> Children => _manageFolderViewModels;

        public void Initialize(IDirectoryInfo directoryInfo, FolderRuleActionEnum manageFolderState)
        {
            _directoryInfo = directoryInfo;
            _manageFolderState = manageFolderState;
        }
    }
}