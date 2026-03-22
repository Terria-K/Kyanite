using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Android.Util;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

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
                var existing = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => a.FullName == args.Name);

                if (existing is not null)
                {
                    return existing;
                }

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
        // since Nickelite makes Nickel a library, there's no longer an entry point
        var nickelType = assembly.GetType("Nickel.Nickel");
        if (nickelType is null)
        {
            return;
        }

        var entryPoint = nickelType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
        var code = entryPoint?.Invoke(null, [Array.Empty<string>()]);
        if (code is not null)
        {
            int c = (int)code;
            Log.Error("Kyanite", "Status Code: " + c);
        }
    }
}