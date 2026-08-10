using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;

using AssetsTools.NET.Extra;

using System;
using System.IO;
using System.Text;

namespace UABEAvalonia.Android;

[Activity(
    Label = "UABEA Android",
    MainLauncher = true
)]
public class MainActivity : Activity
{
    private const int PickFileRequestCode = 1001;

    private TextView? status;
    private AssetsManager? assetsManager;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        try
        {
            assetsManager = new AssetsManager();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "AssetsTools.NET error: " + ex
            );
        }

        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        layout.SetPadding(40, 40, 40, 40);

        var title = new TextView(this)
        {
            Text = "UABEA Android"
        };

        title.TextSize = 24;

        var button = new Button(this)
        {
            Text = "OPEN UNITY3D"
        };

        button.Click += (sender, e) =>
        {
            OpenFilePicker();
        };

        status = new TextView(this)
        {
            Text = assetsManager != null
                ? "✅ AssetsTools.NET berhasil dimuat.\n\nPilih file .unity3d."
                : "❌ AssetsTools.NET gagal dimuat."
        };

        status.TextSize = 16;

        layout.AddView(title);
        layout.AddView(button);
        layout.AddView(status);

        SetContentView(layout);
    }

    private void OpenFilePicker()
    {
        try
        {
            Intent intent = new Intent(
                Intent.ActionOpenDocument
            );

            intent.AddCategory(
                Intent.CategoryOpenable
            );

            intent.SetType("*/*");

            StartActivityForResult(
                intent,
                PickFileRequestCode
            );
        }
        catch (Exception ex)
        {
            ShowStatus(
                "❌ Gagal membuka File Picker\n\n" +
                ex.Message
            );
        }
    }

    protected override void OnActivityResult(
        int requestCode,
        Result resultCode,
        Intent? data)
    {
        base.OnActivityResult(
            requestCode,
            resultCode,
            data
        );

        if (requestCode != PickFileRequestCode)
            return;

        if (resultCode != Result.Ok)
        {
            ShowStatus(
                "Pemilihan file dibatalkan."
            );

            return;
        }

        if (data?.Data == null)
        {
            ShowStatus(
                "❌ File tidak ditemukan."
            );

            return;
        }

        try
        {
            ProcessSelectedFile(data.Data);
        }
        catch (Exception ex)
        {
            ShowStatus(
                "❌ Gagal memproses file\n\n" +
                ex.Message
            );

            System.Diagnostics.Debug.WriteLine(
                ex.ToString()
            );
        }
    }

    private void ProcessSelectedFile(
        global::Android.Net.Uri uri)
    {
        if (assetsManager == null)
        {
            throw new Exception(
                "AssetsManager belum tersedia."
            );
        }

        string fileName = "temp.unity3d";

        using (var cursor = ContentResolver.Query(
            uri,
            null,
            null,
            null,
            null))
        {
            if (cursor != null)
            {
                int nameIndex =
                    cursor.GetColumnIndex(
                        OpenableColumns.DisplayName
                    );

                if (cursor.MoveToFirst() &&
                    nameIndex >= 0)
                {
                    string? detectedName =
                        cursor.GetString(nameIndex);

                    if (!string.IsNullOrWhiteSpace(
                        detectedName))
                    {
                        fileName = detectedName;
                    }
                }
            }
        }

        foreach (
            char invalidChar
            in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(
                invalidChar,
                '_'
            );
        }

        string cacheDirectory =
            CacheDir?.AbsolutePath
            ?? throw new Exception(
                "Cache directory Android tidak tersedia."
            );

        string cachePath =
            Path.Combine(
                cacheDirectory,
                fileName
            );

        using (
            var input =
                ContentResolver.OpenInputStream(uri))
        {
            if (input == null)
            {
                throw new Exception(
                    "Tidak dapat membaca file."
                );
            }

            using (
                var output =
                    File.Create(cachePath))
            {
                input.CopyTo(output);
            }
        }

        long fileSize =
            new FileInfo(cachePath).Length;

        ShowStatus(
            "⏳ Membaca AssetBundle...\n\n" +
            "Nama : " + fileName + "\n" +
            "Ukuran : " + fileSize.ToString("N0") +
            " bytes"
        );

        var bundleInst =
            assetsManager.LoadBundleFile(
                cachePath
            );

        var directories =
            bundleInst.file.BlockAndDirInfo.DirectoryInfos;

        StringBuilder result =
            new StringBuilder();

        result.AppendLine(
            "✅ ASSETBUNDLE BERHASIL DIBUKA!"
        );

        result.AppendLine();

        result.AppendLine(
            "Nama : " + fileName
        );

        result.AppendLine(
            "Ukuran : " + fileSize.ToString("N0") +
            " bytes"
        );

        result.AppendLine();

        result.AppendLine(
            "Jumlah file : " +
            directories.Count
        );

        result.AppendLine();

        result.AppendLine(
            "=== ISI BUNDLE ==="
        );

        result.AppendLine();

        int index = 1;

        foreach (var dir in directories)
        {
            result.AppendLine(
                index.ToString() +
                ". " +
                dir.Name
            );

            index++;
        }

        ShowStatus(
            result.ToString()
        );
    }

    private void ShowStatus(
        string message)
    {
        RunOnUiThread(() =>
        {
            if (status != null)
            {
                status.Text = message;
            }
        });
    }
}
