using Android.App;
using Android.Content.PM;
using Android;
using Avalonia;
using Avalonia.Android;
using UABEAvalonia;

// 1. Menyuntikkan Izin Penyimpanan Memori HP
[assembly: UsesPermission(Manifest.Permission.ReadExternalStorage)]
[assembly: UsesPermission(Manifest.Permission.WriteExternalStorage)]
[assembly: UsesPermission(Manifest.Permission.ManageExternalStorage)]

// 2. Menyuntikkan Atribut Aplikasi Utama agar .NET Membuatkan Manifes Otomatis yang Bersih
[assembly: Application(
    Label = "UABEA Android Native",
    AllowBackup = true,
    RequestLegacyExternalStorage = true)]

namespace AVALONIAAndroid;

[Activity(
    Label = "UABEA Android Native",
    Theme = "@style/AvaloniaTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder);
    }
}
