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

using AndroidUri = global::Android.Net.Uri;

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


    protected override void OnCreate(
        Bundle? savedInstanceState)
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

        LinearLayout layout =
            new LinearLayout(this);

        layout.Orientation =
            Orientation.Vertical;

        layout.SetPadding(
            24,
            24,
            24,
            24
        );


        // =====================================================
        // Title
        // =====================================================

        TextView title =
            new TextView(this);

        title.Text =
            "UABEA Android";

        title.TextSize =
            24;


        // =====================================================
        // Button
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

        status.TextSize =
            18;


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


        // =====================================================
        // Scroll
        // =====================================================

        ScrollView scroll =
            new ScrollView(this);

        scroll.AddView(status);


        // =====================================================
        // Layout
        // =====================================================

        layout.AddView(title);

        layout.AddView(button);

        LinearLayout.LayoutParams scrollParams =
            new LinearLayout.LayoutParams(
                -1,
                0
            );

        scrollParams.Weight = 1;

        layout.AddView(
            scroll,
            scrollParams
        );


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
            ShowError(
                "Gagal membuka File Picker",
                ex
            );
        }
    }


    // =========================================================
    // FILE PICKER RESULT
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
            SetStatus(
                "Pemilihan file dibatalkan."
            );

            return;
        }


        if (data == null)
        {
            SetStatus(
                "Data file tidak ditemukan."
            );

            return;
        }


        AndroidUri? uri =
            data.Data;


        if (uri == null)
        {
            SetStatus(
                "URI file tidak ditemukan."
            );

            return;
        }


        try
        {
            ProcessFile(uri);
        }
        catch (Exception ex)
        {
            ShowError(
                "Gagal memproses file",
                ex
            );
        }
    }


    // =========================================================
    // GET FILE NAME
    // =========================================================

    private string GetFileName(
        AndroidUri uri)
    {
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
                    string? detectedName =
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


        return fileName;
    }


    // =========================================================
    // COPY FILE
    // =========================================================

    private string CopyToCache(
        AndroidUri uri,
        string fileName)
    {
        if (CacheDir == null)
        {
            throw new Exception(
                "Cache directory tidak tersedia."
            );
        }


        string cachePath =
            Path.Combine(
                CacheDir.AbsolutePath,
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
                FileStream output =
                    File.Create(cachePath))
            {
                input.CopyTo(output);
            }
        }


        return cachePath;
    }


    // =========================================================
    // PROCESS FILE
    // =========================================================

    private void ProcessFile(
        AndroidUri uri)
    {
        if (assetsManager == null)
        {
            throw new Exception(
                "AssetsManager tidak tersedia."
            );
        }


        AssetsManager manager =
            assetsManager;


        // =====================================================
        // Nama file
        // =====================================================

        string fileName =
            GetFileName(uri);


        // =====================================================
        // Copy
        // =====================================================

        SetStatus(
            "⏳ Menyalin file..."
        );


        string cachePath =
            CopyToCache(
                uri,
                fileName
            );


        FileInfo fileInfo =
            new FileInfo(cachePath);


        long fileSize =
            fileInfo.Length;


        // =====================================================
        // Load Bundle
        // =====================================================

        SetStatus(
            "⏳ Membuka AssetBundle..."
        );


        BundleFileInstance bundle =
            manager.LoadBundleFile(
                cachePath
            );


        if (bundle == null)
        {
            throw new Exception(
                "AssetBundle gagal dibuka."
            );
        }


        // =====================================================
        // Directory
        // =====================================================

        var directories =
            bundle.file
                .BlockAndDirInfo
                .DirectoryInfos;


        int directoryCount =
            directories.Count;


        // =====================================================
        // Output
        // =====================================================

        StringBuilder output =
            new StringBuilder();


        output.AppendLine(
            "ASSETBUNDLE BERHASIL DIBUKA!"
        );


        output.AppendLine();


        output.AppendLine(
            "Nama: " +
            fileName
        );


        output.AppendLine(
            "Ukuran: " +
            fileSize.ToString("N0") +
            " bytes"
        );


        output.AppendLine();


        output.AppendLine(
            "File dalam Bundle: " +
            directoryCount
        );


        output.AppendLine();


        output.AppendLine(
            "=== SERIALIZED FILE ==="
        );


        output.AppendLine();


        // =====================================================
        // Serialized Files
        // =====================================================

        int number =
            0;


        for (
            int i = 0;
            i < directoryCount;
            i++)
        {
            var directory =
                directories[i];


            string internalName =
                directory.Name;


            number++;


            output.AppendLine(
                number +
                ". " +
                internalName
            );


            try
            {
                // =================================================
                // Load serialized file
                // =================================================

                AssetsFileInstance assetsFile =
                    manager.LoadAssetsFileFromBundle(
                        bundle,
                        i,
                        false
                    );


                if (assetsFile == null)
                {
                    output.AppendLine(
                        "   Gagal memuat SerializedFile."
                    );

                    output.AppendLine();

                    continue;
                }


                // =================================================
                // Unity version
                // =================================================

                string unityVersion =
                    assetsFile
                        .file
                        .Metadata
                        .UnityVersion;


                output.AppendLine(
                    "   Unity Version: " +
                    unityVersion
                );


                // =================================================
                // Asset count
                // =================================================

                int assetCount =
                    assetsFile
                        .file
                        .AssetInfos
                        .Count;


                output.AppendLine(
                    "   Asset Count: " +
                    assetCount
                );


                output.AppendLine();


                output.AppendLine(
                    "   === ASSET LIST ==="
                );


                // =================================================
                // Asset list
                // =================================================

                int assetNumber =
                    0;


                foreach (
                    var assetInfo
                    in assetsFile.file.AssetInfos)
                {
                    assetNumber++;


                    long pathId =
                        assetInfo.PathId;


                    int typeId =
                        assetInfo.TypeId;


                    output.AppendLine(
                        "   " +
                        assetNumber +
                        ". TypeID: " +
                        typeId +
                        " PathID: " +
                        pathId
                    );
                }


                output.AppendLine();
            }
            catch (Exception ex)
            {
                output.AppendLine(
                    "   Error:"
                );


                output.AppendLine(
                    "   " +
                    ex.Message
                );


                output.AppendLine();
            }
        }


        // =====================================================
        // Selesai
        // =====================================================

        SetStatus(
            output.ToString()
        );
    }


    // =========================================================
    // STATUS
    // =========================================================

    private void SetStatus(
        string message)
    {
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


    // =========================================================
    // ERROR
    // =========================================================

    private void ShowError(
        string title,
        Exception ex)
    {
        SetStatus(
            "❌ " +
            title +
            "\n\n" +
            ex.Message
        );


        System.Diagnostics.Debug.WriteLine(
            ex.ToString()
        );
    }
}
