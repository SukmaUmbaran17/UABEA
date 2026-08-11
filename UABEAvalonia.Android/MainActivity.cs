using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;

using AssetsTools.NET.Extra;

using System;
using System.IO;
using System.Linq;
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
        // AssetsManager
        // =====================================================

        try
        {
            assetsManager = new AssetsManager();
        }
        catch (Exception ex)
        {
            assetsManager = null;

            System.Diagnostics.Debug.WriteLine(
                ex.ToString()
            );
        }


        // =====================================================
        // Layout
        // =====================================================

        LinearLayout layout = new LinearLayout(this);

        layout.Orientation = Orientation.Vertical;

        layout.SetPadding(
            40,
            40,
            40,
            40
        );


        // =====================================================
        // Judul
        // =====================================================

        TextView title = new TextView(this);

        title.Text = "UABEA Android";

        title.TextSize = 24;


        // =====================================================
        // Tombol
        // =====================================================

        Button button = new Button(this);

        button.Text = "OPEN UNITY3D";


        button.Click += delegate
        {
            OpenFilePicker();
        };


        // =====================================================
        // Status
        // =====================================================

        status = new TextView(this);

        if (assetsManager != null)
        {
            status.Text =
                "AssetsTools.NET berhasil dimuat.\n\n" +
                "Pilih file Unity3D.";
        }
        else
        {
            status.Text =
                "AssetsTools.NET gagal dimuat.";
        }

        status.TextSize = 16;


        // =====================================================
        // Tambahkan view
        // =====================================================

        layout.AddView(title);

        layout.AddView(button);

        layout.AddView(status);


        // =====================================================
        // Tampilkan
        // =====================================================

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
                "Gagal membuka File Picker\n\n" +
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
        {
            return;
        }


        if (resultCode != Result.Ok)
        {
            ShowStatus(
                "Pemilihan file dibatalkan."
            );

            return;
        }


        if (data == null)
        {
            ShowStatus(
                "File tidak ditemukan."
            );

            return;
        }


        global::Android.Net.Uri? uri =
            data.Data;


        if (uri == null)
        {
            ShowStatus(
                "URI file tidak ditemukan."
            );

            return;
        }


        try
        {
            ProcessSelectedFile(uri);
        }
        catch (Exception ex)
        {
            ShowStatus(
                "Gagal memproses file:\n\n" +
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
        global::Android.Net.Uri uri)
    {
        // =====================================================
        // Pastikan AssetsManager tersedia
        // =====================================================

        if (assetsManager == null)
        {
            throw new Exception(
                "AssetsManager belum tersedia."
            );
        }


        AssetsManager manager =
            assetsManager;


        // =====================================================
        // Nama file
        // =====================================================

        string fileName =
            "temp.unity3d";


        using (
            var cursor =
                ContentResolver.Query(
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
                    nameIndex >= 0)
                {
                    string detectedName =
                        cursor.GetString(
                            nameIndex
                        );


                    if (
                        !string.IsNullOrWhiteSpace(
                            detectedName))
                    {
                        fileName =
                            detectedName;
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
            fileName =
                fileName.Replace(
                    invalidChar,
                    '_'
                );
        }


        // =====================================================
        // Cache
        // =====================================================

        if (CacheDir == null)
        {
            throw new Exception(
                "CacheDir Android tidak tersedia."
            );
        }


        string cacheDirectory =
            CacheDir.AbsolutePath;


        string cachePath =
            Path.Combine(
                cacheDirectory,
                fileName
            );


        // =====================================================
        // Copy URI ke cache
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
                FileStream output =
                    File.Create(cachePath))
            {
                input.CopyTo(output);
            }
        }


        // =====================================================
        // Ukuran
        // =====================================================

        FileInfo info =
            new FileInfo(cachePath);


        long fileSize =
            info.Length;


        ShowStatus(
            "Membaca AssetBundle...\n\n" +
            "Nama : " +
            fileName +
            "\n" +
            "Ukuran : " +
            fileSize.ToString("N0") +
            " bytes"
        );


        // =====================================================
        // Buka AssetBundle
        // =====================================================

        BundleFileInstance bundleInst =
            manager.LoadBundleFile(
                cachePath
            );


        // =====================================================
        // Directory list
        // =====================================================

        var directories =
            bundleInst
                .file
                .BlockAndDirInfo
                .DirectoryInfos;


        // =====================================================
        // Hasil
        // =====================================================

        StringBuilder result =
            new StringBuilder();


        result.AppendLine(
            "ASSETBUNDLE BERHASIL DIBUKA!"
        );


        result.AppendLine();


        result.AppendLine(
            "Nama : " +
            fileName
        );


        result.AppendLine(
            "Ukuran : " +
            fileSize.ToString("N0") +
            " bytes"
        );


        result.AppendLine();


        result.AppendLine(
            "Jumlah file : " +
            directories.Count()
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


    // =========================================================
    // STATUS
    // =========================================================

    private void ShowStatus(
        string message)
    {
        if (status == null)
        {
            return;
        }


        RunOnUiThread(
            delegate
            {
                if (status != null)
                {
                    status.Text = message;
                }
            }
        );
    }
}
