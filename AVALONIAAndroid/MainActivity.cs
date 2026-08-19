using Android.App;
using Android.Content.PM;
using Android;
using Avalonia;
using Avalonia.Android;
using UABEAvalonia;

// Menyuntikkan izin penyimpanan langsung via kode C# tanpa membutuhkan AndroidManifest.xml
[assembly: UsesPermission(Manifest.Permission.ReadExternalStorage)]
[assembly: UsesPermission(Manifest.Permission.WriteExternalStorage)]
[assembly: UsesPermission(Manifest.Permission.ManageExternalStorage)]

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
