using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Android.Content;
using Android.Util;
using A = Android;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Kyanite.Android;
using AndroidX.Core.App;
using Android;

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

            var loadedAssembly = new Dictionary<string, Assembly>();

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name).Name + ".dll";
                if (loadedAssembly.TryGetValue(name, out var asm))
                {
                    return asm;
                }
                
                var existing = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a =>
                    {
                        return a.FullName == args.Name;
                    });

                if (existing is not null)
                {
                    return existing;
                }


                if (assemblyFiles.TryGetValue(name, out var data))
                {
                    Log.Error("[Kyanite]", $"Loaded {name}");
                    var loadedASM = Assembly.Load(data);
                    loadedAssembly.Add(name, loadedASM);
                    return loadedASM;
                }

                Log.Error("[Kyanite]", $"Failed to load assembly name: {args.Name} from path: {name}");

                return null;
            };

            var assembly = Assembly.Load(nickelFile);
            StartNickel(Path.Combine(Uri.UnescapeDataString(directories[0].Path.AbsolutePath), "CobaltCore.exe"), assembly);
        }
        catch (Exception e)
        {
            Log.Error("[Kyanite]", e.ToString());
        }
    }

    private static void StartNickel(string gamePath, Assembly assembly)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        ActivityCompat.RequestPermissions(MainActivity.Instance, [Manifest.Permission.ManageExternalStorage], 0);
#pragma warning restore CA1416 // Validate platform compatibility

        if (gamePath.StartsWith("/tree/primary:"))
        {
            string relative = gamePath["/tree/primary:".Length..];
            string realPath = A.OS.Environment.ExternalStorageDirectory!.AbsolutePath 
                            + "/" + relative.Replace(':','/');
            Console.WriteLine(realPath);
            Services.AppServices.GamePath = realPath;
        }
        Services.AppServices.NickelAsm = assembly;

        var intent = new Intent(MainActivity.Instance, typeof(GameActivity));
        MainActivity.Instance.StartActivity(intent);
    }
}