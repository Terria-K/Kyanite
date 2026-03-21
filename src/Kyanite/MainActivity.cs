using System;
using System.IO;
using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;

namespace Kyanite.Android;

[Activity(
    Label = "Kyanite",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    public MainActivity()
    {
        Instance = this;
    }

    public static MainActivity Instance { get; private set; } = null!;

    public override void OnCreate(Bundle? savedInstanceState, PersistableBundle? persistentState)
    {
        base.OnCreate(savedInstanceState, persistentState);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}