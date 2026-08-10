using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;

using AssetsTools.NET;
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

        // =====================================================
        // AssetsTools.NET
        // =====================================================

        try
        {
            assetsManager = new AssetsManager();
        }
        catch (Exception ex)
        {
            Toast.MakeText(
                this,
                "Gagal memuat AssetsTools.NET",
                ToastLength.Long
            )?.Show();

            System.Diagnostics.Debug.WriteLine(
                "AssetsTools.NET error: " + ex
            );
        }


        // =====================================================
        // Layout
        // =====================================================

        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        layout.SetPadding(40, 40, 40, 40);


        // =====================================================
        // Judul
        // =====================================================

        var title = new TextView(this)
        {
            Text = "UABEA Android"
        };

        title.TextSize = 24;


        // =====================================================
        // Tombol
        // =====================================================

        var button = new Button(this)
        {
            Text = "OPEN UNITY3D"
        };

        button.Click += (sender, e) =>
        {
            OpenFilePicker();
        };


        // =====================================================
        // Status
        // =====================================================

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


    // =========================================================
    // FILE PICKER
    // =========================================================

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


    // =========================================================
    // HASIL FILE PICKER
    // =========================================================

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


    // =========================================================
    // PROSES FILE
    // =========================================================

    private void ProcessSelectedFile(
        Android.Net.Uri uri)
    {
        // =====================================================
        // Nama file
        // =====================================================

        string fileName = "temp.assets";

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

                if (
                    cursor.MoveToFirst() &&
                    nameIndex >= 0
                )
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


        // =====================================================
        // Bersihkan nama
        // =====================================================

        foreach (
            char invalidChar
            in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(
                invalidChar,
                '_'
            );
        }


        // =====================================================
        // Cache Android
        // =====================================================

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


        // =====================================================
        // Salin URI → cache
        // =====================================================

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


        // =====================================================
        // Pastikan AssetsManager tersedia
        // =====================================================

        if (assetsManager == null)
        {
            throw new Exception(
                "AssetsManager belum tersedia."
            );
        }


        // =====================================================
        // COBA BUKA SEBAGAI ASSET BUNDLE
        // =====================================================

        ShowStatus(
            "⏳ Membaca AssetBundle...\n\n" +
            $"Nama : {fileName}\n" +
            $"Ukuran : {fileSize:N0} bytes"
        );


        var bundleInst =
            assetsManager.LoadBundleFile(
                cachePath
            );


        // =====================================================
        // BACA INFORMASI BUNDLE
        // =====================================================

        var bundleFile =
            bundleInst.file;


        var directories =
            bundleFile.BlockAndDirInfo.DirectoryInfos;


        // =====================================================
        // TAMPILKAN HASIL
        // =====================================================

        StringBuilder result =
            new StringBuilder();


        result.AppendLine(
            "✅ ASSETBUNDLE BERHASIL DIBUKA!"
        );

        result.AppendLine();

        result.AppendLine(
            $"Nama : {fileName}"
        );

        result.AppendLine(
            $"Ukuran : {fileSize:N0} bytes"
        );

        result.AppendLine();

        result.AppendLine(
            $"Jumlah file : {directories.Count}"
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
                $"{index}. {dir.Name}"
            );

            index++;
        }


        ShowStatus(
            result.ToString()
        );
    }


    // =========================================================
    // STATUS
    // =========================================================

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
