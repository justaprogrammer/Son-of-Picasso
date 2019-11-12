using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.UI.ViewModels.FolderRules
{
    public class FolderRuleViewModel : ReactiveObject, IFolderRuleInput, IDisposable
    {
        public enum ShouldShowChildReasonEnum
        {
            ShouldNot,
            IsParentOfRule
        }

        private readonly CompositeDisposable _disposables;
        private readonly ObservableAsPropertyHelper<bool> _shouldShowPropertyHelper;
        private FolderRuleActionEnum _folderRuleAction;
        private bool _isLoaded;
        private ShouldShowChildReasonEnum _shouldShowReason = ShouldShowChildReasonEnum.ShouldNot;

        public FolderRuleViewModel(IDirectoryInfo directoryInfo)
        {
            DirectoryInfo = directoryInfo;
            Children = new ObservableCollectionExtended<FolderRuleViewModel>();

            _disposables = new CompositeDisposable();

            _shouldShowPropertyHelper = this.WhenPropertyChanged(model => model.ShouldShowReason)
                .Select(propertyValue => propertyValue.Value != ShouldShowChildReasonEnum.ShouldNot)
                .ToProperty(this, model => model.ShouldShow);

            this
                .WhenPropertyChanged(model => model.FolderRuleAction, false)
                .Subscribe(propertyValue =>
                {
                    foreach (var customFolderRuleInput in Children)
                        customFolderRuleInput.FolderRuleAction = propertyValue.Value;
                })
                .DisposeWith(_disposables);
        }

        public IDirectoryInfo DirectoryInfo { get; }

        public bool ShouldShow => _shouldShowPropertyHelper.Value;

        public string Name => DirectoryInfo.Name;

        public bool IsLoaded
        {
            get => _isLoaded;
            set => this.RaiseAndSetIfChanged(ref _isLoaded, value);
        }

        public IObservableCollection<FolderRuleViewModel> Children { get; }

        public ShouldShowChildReasonEnum ShouldShowReason
        {
            get => _shouldShowReason;
            set => this.RaiseAndSetIfChanged(ref _shouldShowReason, value);
        }

        public void Dispose()
        {
            _shouldShowPropertyHelper?.Dispose();
            _disposables?.Dispose();
        }

        public string Path => DirectoryInfo.FullName;

        public FolderRuleActionEnum FolderRuleAction
        {
            get => _folderRuleAction;
            set => this.RaiseAndSetIfChanged(ref _folderRuleAction, value);
        }

        IList<IFolderRuleInput> IFolderRuleInput.Children => Children.Cast<IFolderRuleInput>().ToArray();
    }
}