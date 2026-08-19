using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace UABEAvalonia.Android;

[Activity(
    Label = "UABEA Android Native",
    Theme = "@style/AvaloniaTheme",
    Icon = "@確定/icon", // Anda bisa menyesuaikan atau menghapus baris icon ini nanti
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont(); // Mengaktifkan font bawaan Avalonia agar teks tidak kotak-kotak
    }
}
