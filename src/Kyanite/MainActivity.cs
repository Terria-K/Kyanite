using System;
using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Avalonia;
using Avalonia.Android;
using Kyanite.Services;

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

[Activity(Label = "Game Activity", ConfigurationChanges = ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape, Exported = true)]
public class GameActivity : Activity
{
    protected override async void OnStart()
    {
        base.OnStart();

        // since Nickelite makes Nickel a library, there's no longer an entry point
        var nickelType = AppServices.NickelAsm.GetType("Nickel.Nickel");
        if (nickelType is null)
        {
            return;
        }

        var entryPoint = nickelType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);

        try
        {
            Log.Error("Kyanite", "THE PATH IS: " + AppServices.GamePath);
            string[] args = [AppServices.GamePath];
            var code = entryPoint?.Invoke(null, [args]);
            if (code is not null)
            {
                int c = (int)code;
                Log.Error("Kyanite", "Status Code: " + c);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Kyanite", ex.ToString());
        }

    }
}