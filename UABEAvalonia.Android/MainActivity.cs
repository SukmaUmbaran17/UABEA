using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using System;
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
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");

            StartActivityForResult(
                intent,
                PickFileRequestCode
            );
        };

        layout.AddView(title);
        layout.AddView(button);
        layout.AddView(status);

        SetContentView(layout);
    }

    protected override void OnActivityResult(
        int requestCode,
        Result resultCode,
        Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode != PickFileRequestCode ||
            resultCode != Result.Ok ||
            data?.Data == null)
        {
            return;
        }

        try
        {
            var uri = data.Data;

            string fileName = "temp.assets";

            string cachePath = Path.Combine(
                CacheDir!.AbsolutePath,
                fileName
            );

            using (var input = ContentResolver!.OpenInputStream(uri))
            using (var output = File.Create(cachePath))
            {
                if (input == null)
                    throw new Exception(
                        "Tidak dapat membuka file."
                    );

                input.CopyTo(output);
            }

            status!.Text =
                "⏳ Membuka file Unity...";

            AssetsManager am = new AssetsManager();

            AssetsFileInstance fileInst =
                am.LoadAssetsFile(
                    cachePath,
                    true
                );

            AssetWorkspace workspace =
                new AssetWorkspace(
                    am,
                    false
                );

            workspace.LoadAssetsFile(
                fileInst,
                true
            );

            status!.Text =
                $"✅ Asset berhasil dibuka!\n\n" +
                $"File: {fileName}\n" +
                $"Assets: {workspace.LoadedAssets.Count}";
        }
        catch (Exception ex)
        {
            status!.Text =
                $"❌ Gagal membuka file Unity\n\n" +
                $"{ex.Message}";
        }
    }
}
