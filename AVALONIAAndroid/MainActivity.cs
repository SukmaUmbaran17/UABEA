using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using UABEAvalonia; // Menghubungkan ke logika utama UABEA

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
