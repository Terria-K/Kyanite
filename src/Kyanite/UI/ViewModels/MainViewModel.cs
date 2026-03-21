using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Kyanite.Services;
using ReactiveUI;

namespace Kyanite.ViewModels;

public class MainViewModel : ViewModelBase
{
    public int Count
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public MainViewModel()
    {
    }
}
