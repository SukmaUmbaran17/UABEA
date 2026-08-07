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

        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        layout.SetPadding(40, 40, 40, 40);

        var title = new TextView(this);
        title.Text = "UABEA Android";
        title.TextSize = 24;

        var button = new Button(this);
        button.Text = "Open Asset";

        var status = new TextView(this);
        status.Text = "Belum ada file dipilih.";

        button.Click += (s, e) =>
        {
            status.Text = "Tombol Open ditekan.";
        };

        layout.AddView(title);
        layout.AddView(button);
        layout.AddView(status);

        SetContentView(layout);
    }
}
