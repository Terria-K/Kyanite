using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Android.Util;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Kyanite.Android;

namespace Kyanite.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    public async void OnClick(object? sender, RoutedEventArgs args)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var directories = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Open Folder",
            AllowMultiple = false,
        });

        if (directories.Count == 0)
        {
            return;
        }

        // var assets = MainActivity.Instance.Application?.ApplicationContext?.Assets;
        // var bcl = await assets!.ListAsync("dotnet_bcl");

        // foreach (var l in bcl!)
        // {
        //     using var fs = assets.Open(Path.Combine("dotnet_bcl", l));
        //     var targetFile = await directories[0].CreateFileAsync(l);

        //     using var targetStream = await targetFile!.OpenWriteAsync();

        //     await fs.CopyToAsync(targetStream);
        // }

        try
        {
            var nickel = await directories[0].GetFolderAsync("Nickel");
            if (nickel is null)
            {
                return;
            }

            var assemblyFiles = new Dictionary<string, byte[]>();

            var baseGameDll = await directories[0].GetFileAsync("CobaltCore.dll");
            if (baseGameDll is not null)
            {
                using var ms = new MemoryStream();
                using var stream = await baseGameDll.OpenReadAsync();
                await stream.CopyToAsync(ms);
                assemblyFiles[baseGameDll.Name] = ms.ToArray();   
            }

            await foreach (var item in nickel.GetItemsAsync())
            {
                if (item is IStorageFile file && file.Name.EndsWith(".dll"))
                {
                    using var ms = new MemoryStream();
                    using var stream = await file.OpenReadAsync();
                    await stream.CopyToAsync(ms);

                    assemblyFiles[file.Name] = ms.ToArray();
                }
            }

            if (!assemblyFiles.TryGetValue("Nickel.dll", out var nickelFile))
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name).Name + ".dll";
                if (assemblyFiles.TryGetValue(name, out var data))
                {
                    Log.Info("[Kyanite]", $"Loaded {name}");
                    return Assembly.Load(data);
                }

                Log.Error("[Kyanite]", $"Failed to load assembly name: {args.Name} from path: {name}");

                return null;
            };

            // preloading the dependencies
            Assembly.Load(assemblyFiles["PluginManager.dll"]);
            Assembly.Load(assemblyFiles["NickelCommon.dll"]);

            var assembly = Assembly.Load(nickelFile);
            StartNickel(assembly);
        }
        catch (Exception e)
        {
            Log.Error("[Kyanite]", e.ToString());
        }
    }

    private static void StartNickel(Assembly assembly)
    {
        var Nickel = assembly.EntryPoint?.DeclaringType;
        if (Nickel is null)
        {
            return;
        }

        var newEntry = Nickel.GetMethod("CreateAndStartInstance", BindingFlags.Static | BindingFlags.NonPublic);
        var launchArguments = new LaunchArguments()
        {
            InitSteam = false
        };

        var nickelLaunchArguments = Nickel.Assembly.GetType("Nickel.LaunchArguments");
        var passable = MapStructFields(launchArguments, nickelLaunchArguments!);

        // FIXME: Investigate Method not found: !!0 Nickel.Common.SettingsUtilities.ReadSettings<!0>
        newEntry!.Invoke(null, [passable, Stopwatch.StartNew()]);
    }

    public static object MapStructFields(object sourceStruct, Type targetType)
    {
        var sourceType = sourceStruct.GetType();
        var targetInstance = Activator.CreateInstance(targetType)!;

        foreach (var field in targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var sourceField = sourceType.GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (sourceField != null)
            {
                var value = sourceField.GetValue(sourceStruct);
                field.SetValue(targetInstance, value);
            }
        }

        return targetInstance;
    }
}

internal readonly struct LaunchArguments
{
	public bool Vanilla { get; init; }
	public bool? Debug { get; init; }
	public bool? SaveInDebug { get; init; }
	public bool? InitSteam { get; init; }
	public FileInfo? GamePath { get; init; }
	public DirectoryInfo? ModsPath { get; init; }
	public DirectoryInfo? InternalModsPath { get; init; }
	public DirectoryInfo? ModStoragePath { get; init; }
	public DirectoryInfo? PrivateModStoragePath { get; init; }
	public DirectoryInfo? SavePath { get; init; }
	public DirectoryInfo? LogPath { get; init; }
	public DirectoryInfo? AssemblyCachePath { get; init; }
	public string? AttachDebuggerBeforeMod { get; init; }
	public string? AttachDebuggerAfterMod { get; init; }
	public string? AttachDebuggerBeforeModLoadPhase { get; init; }
	public string? AttachDebuggerAfterModLoadPhase { get; init; }
	public bool? TimestampedLogFiles { get; init; }
	public string? LogPipeName { get; init; }
	public IReadOnlyList<string> UnmatchedArguments { get; init; }
}