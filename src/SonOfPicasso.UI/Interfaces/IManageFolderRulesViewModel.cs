﻿using System;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;

namespace SonOfPicasso.UI.WPF.Interfaces
{
    public interface IManageFolderRulesViewModel : IReactiveObject
    {
        IList<IFolderRuleInput> Folders { get; }
        IObservable<Unit> Initialize();
        bool HideUnselected { get; }
    }
}