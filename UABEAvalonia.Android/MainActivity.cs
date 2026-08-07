using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using System.IO;
using AssetsTools.NET.Extra;
using UABEA.Core.Assets;

namespace UABEAvalonia.Android;

[Activity(Label = "UABEA Android", MainLauncher = true)]
public class MainActivity : Activity
{
    private const int PickFileRequestCode = 1001;
    private TextView? status;

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

        status = new TextView(this);
        status.Text = "Belum ada file dipilih.";

        button.Click += (s, e) =>
        {
            Intent intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");

            StartActivityForResult(intent, PickFileRequestCode);
        };

        layout.AddView(title);
        layout.AddView(button);
        layout.AddView(status);

        SetContentView(layout);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
{
    base.OnActivityResult(requestCode, resultCode, data);

    if (requestCode == PickFileRequestCode &&
        resultCode == Result.Ok &&
        data?.Data != null)
    {
        try
        {
            var uri = data.Data;

            string fileName = "temp.assets";

            var cursor = ContentResolver.Query(uri, null, null, null, null);

            if (cursor != null)
            {
                int index = cursor.GetColumnIndex(Android.Provider.OpenableColumns.DisplayName);

                if (cursor.MoveToFirst() && index >= 0)
                    fileName = cursor.GetString(index);

                cursor.Close();
            }

            string cachePath = Path.Combine(CacheDir.AbsolutePath, fileName);

            using (var input = ContentResolver.OpenInputStream(uri))
            using (var output = File.Create(cachePath))
            {
                input!.CopyTo(output);
            }

            status!.Text =
                $"File berhasil disalin.\n\n{cachePath}";
        }
        catch (Exception ex)
        {
            status!.Text = ex.ToString();
        }
    }
    }
}
