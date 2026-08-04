using Android.App;
using Android.OS;
using Android.Widget;

namespace UABEAvalonia.Android;

[Activity(Label = "UABEA Android", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var text = new TextView(this);
        text.Text = "UABEA Android berhasil berjalan!";
        SetContentView(text);
    }
}
