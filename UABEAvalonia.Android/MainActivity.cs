using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;

using AssetsTools.NET;
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


    // =========================================================
    // ON CREATE
    // =========================================================

    protected override void OnCreate(
        Bundle? savedInstanceState)
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
            assetsManager = null;

            System.Diagnostics.Debug.WriteLine(
                ex.ToString()
            );
        }


        // =====================================================
        // Layout
        // =====================================================

        LinearLayout layout =
            new LinearLayout(this);

        layout.Orientation =
            Orientation.Vertical;

        layout.SetPadding(
            40,
            40,
            40,
            40
        );


        // =====================================================
        // Judul
        // =====================================================

        TextView title =
            new TextView(this);

        title.Text =
            "UABEA Android";

        title.TextSize =
            24;


        // =====================================================
        // Tombol
        // =====================================================

        Button button =
            new Button(this);

        button.Text =
            "OPEN UNITY3D";


        button.Click += delegate
        {
            OpenFilePicker();
        };


        // =====================================================
        // Status
        // =====================================================

        status =
            new TextView(this);

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

        status.TextSize =
            16;


        // =====================================================
        // Tambahkan View
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
            Intent intent =
                new Intent(
                    Intent.ActionOpenDocument
                );

            intent.AddCategory(
                Intent.CategoryOpenable
            );

            intent.SetType(
                "*/*"
            );


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
        // Bersihkan nama file
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
        // Cache Android
        // =====================================================

        if (CacheDir == null)
        {
            throw new Exception(
                "CacheDir Android tidak tersedia."
            );
        }


        string cachePath =
            Path.Combine(
                CacheDir.AbsolutePath,
                fileName
            );


        // =====================================================
        // Copy URI -> Cache
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
            new FileInfo(
                cachePath
            );


        long fileSize =
            info.Length;


        ShowStatus(
            "⏳ Membaca AssetBundle...\n\n" +
            "Nama : " +
            fileName +
            "\n" +
            "Ukuran : " +
            fileSize.ToString("N0") +
            " bytes"
        );


        // =====================================================
        // LOAD ASSET BUNDLE
        // =====================================================

        BundleFileInstance bundleInst =
            manager.LoadBundleFile(
                cachePath
            );


        if (bundleInst == null)
        {
            throw new Exception(
                "AssetBundle tidak dapat dibuka."
            );
        }


        // =====================================================
        // Ambil daftar file dalam bundle
        // =====================================================

        var directories =
            bundleInst
                .file
                .BlockAndDirInfo
                .DirectoryInfos;


        int directoryCount =
            directories.Count();


        // =====================================================
        // Mulai hasil
        // =====================================================

        StringBuilder result =
            new StringBuilder();


        result.AppendLine(
            "✅ ASSETBUNDLE BERHASIL DIBUKA!"
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
            "File dalam Bundle : " +
            directoryCount
        );


        result.AppendLine();


        result.AppendLine(
            "=== SERIALIZED FILE ==="
        );


        result.AppendLine();


        int serializedCount =
            0;


        // =====================================================
        // Baca setiap file CAB
        // =====================================================

        for (
            int i = 0;
            i < directoryCount;
            i++)
        {
            var directory =
                directories[i];


            string directoryName =
                directory.Name;


            // Flags 0x04 = serialized file
            bool isSerialized =
                (directory.Flags & 0x04) != 0;


            if (!isSerialized)
            {
                continue;
            }


            serializedCount++;


            result.AppendLine(
                (serializedCount) +
                ". " +
                directoryName
            );


            try
            {
                // =================================================
                // Load SerializedFile dari Bundle
                // =================================================

                AssetsFileInstance assetsInst =
                    manager.LoadAssetsFileFromBundle(
                        bundleInst,
                        i,
                        false
                    );


                if (assetsInst == null)
                {
                    result.AppendLine(
                        "   Gagal memuat SerializedFile."
                    );

                    result.AppendLine();

                    continue;
                }


                // =================================================
                // Informasi SerializedFile
                // =================================================

                int assetCount =
                    assetsInst
                        .file
                        .Metadata
                        .AssetInfos
                        .Count;


                result.AppendLine(
                    "   Unity Version : " +
                    assetsInst.file.Metadata.UnityVersion
                );


                result.AppendLine(
                    "   Asset Count : " +
                    assetCount
                );


                result.AppendLine();
            }
            catch (Exception ex)
            {
                result.AppendLine(
                    "   Error : " +
                    ex.Message
                );


                result.AppendLine();
            }
        }


        // =====================================================
        // Jika tidak ada SerializedFile
        // =====================================================

        if (serializedCount == 0)
        {
            result.AppendLine(
                "Tidak ada SerializedFile."
            );
        }


        // =====================================================
        // Tampilkan
        // =====================================================

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
                    status.Text =
                        message;
                }
            }
        );
    }
}
