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
        // AssetsManager
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
                ex.ToString()
            );
        }

        // =====================================================
        // Layout
        // =====================================================

        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        layout.SetPadding(24, 24, 24, 24);

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
                ? "AssetsTools.NET siap.\n\nSilakan pilih file Unity3D."
                : "AssetsTools.NET gagal dimuat."
        };

        status.TextSize = 18;

        // =====================================================
        // ScrollView
        // =====================================================

        var scroll = new ScrollView(this);

        scroll.AddView(status);

        // =====================================================
        // Tambahkan komponen
        // =====================================================

        layout.AddView(title);
        layout.AddView(button);

        layout.AddView(
            scroll,
            new LinearLayout.LayoutParams(
                -1,
                0,
                1
            )
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
            ShowError(
                "Gagal membuka File Picker",
                ex
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
            SetStatus(
                "Pemilihan file dibatalkan."
            );

            return;
        }

        if (data?.Data == null)
        {
            SetStatus(
                "❌ URI file tidak ditemukan."
            );

            return;
        }

        try
        {
            var uri = data.Data;

            string fileName =
                GetFileName(uri);

            string cachePath =
                CopyToCache(
                    uri,
                    fileName
                );

            long fileSize =
                new FileInfo(cachePath).Length;

            // =================================================
            // BACA UNITY3D
            // =================================================

            ReadUnityBundle(
                cachePath,
                fileName,
                fileSize
            );
        }
        catch (Exception ex)
        {
            ShowError(
                "Gagal membuka file",
                ex
            );
        }
    }

    // =========================================================
    // NAMA FILE
    // =========================================================

    private string GetFileName(Android.Net.Uri uri)
    {
        string fileName = "temp.unity3d";

        using (
            var cursor = ContentResolver.Query(
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
                    string? detected =
                        cursor.GetString(nameIndex);

                    if (!string.IsNullOrWhiteSpace(
                        detected))
                    {
                        fileName = detected;
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
    // COPY URI → CACHE
    // =========================================================

    private string CopyToCache(
        Android.Net.Uri uri,
        string fileName)
    {
        string cacheDirectory =
            CacheDir?.AbsolutePath
            ?? throw new Exception(
                "Cache directory tidak tersedia."
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

        return cachePath;
    }

    // =========================================================
    // BACA ASSETBUNDLE
    // =========================================================

    private void ReadUnityBundle(
        string cachePath,
        string fileName,
        long fileSize)
    {
        if (assetsManager == null)
        {
            throw new Exception(
                "AssetsManager belum tersedia."
            );
        }

        var output = new StringBuilder();

        output.AppendLine(
            "ASSETBUNDLE BERHASIL DIBUKA!"
        );

        output.AppendLine();

        output.AppendLine(
            $"Nama: {fileName}"
        );

        output.AppendLine(
            $"Ukuran: {fileSize:N0} bytes"
        );

        output.AppendLine();

        // =====================================================
        // Load Bundle
        // =====================================================

        BundleFileInstance bundle =
            assetsManager.LoadBundleFile(
                cachePath
            );

        if (bundle == null)
        {
            throw new Exception(
                "BundleFileInstance bernilai null."
            );
        }

        // =====================================================
        // Daftar file di Bundle
        // =====================================================

        int bundleFileCount =
            bundle.file.BlockAndDirInfo
                .DirectoryInfos.Count;

        output.AppendLine(
            $"File dalam Bundle: {bundleFileCount}"
        );

        output.AppendLine();

        output.AppendLine(
            "=== SERIALIZED FILE ==="
        );

        output.AppendLine();

        // =====================================================
        // Setiap file dalam Bundle
        // =====================================================

        int serializedIndex = 0;

        foreach (
            var dirInfo
            in bundle.file.BlockAndDirInfo.DirectoryInfos)
        {
            serializedIndex++;

            string internalName =
                dirInfo.Name;

            output.AppendLine(
                $"{serializedIndex}. {internalName}"
            );

            try
            {
                // =============================================
                // Load serialized file dari bundle
                // =============================================

                AssetsFileInstance assetsFile =
                    assetsManager.LoadAssetsFileFromBundle(
                        bundle,
                        internalName,
                        false
                    );

                if (assetsFile == null)
                {
                    output.AppendLine(
                        "   ❌ Gagal memuat SerializedFile"
                    );

                    output.AppendLine();

                    continue;
                }

                // =============================================
                // Unity version
                // =============================================

                string unityVersion =
                    assetsFile.file.Metadata.UnityVersion;

                output.AppendLine(
                    $"   Unity Version: {unityVersion}"
                );

                // =============================================
                // Asset count
                // =============================================

                int assetCount =
                    assetsFile.file.AssetInfos.Count;

                output.AppendLine(
                    $"   Asset Count: {assetCount}"
                );

                output.AppendLine();

                output.AppendLine(
                    "   === ASSET LIST ==="
                );

                // =============================================
                // Semua asset
                // =============================================

                int index = 0;

                foreach (
                    var assetInfo
                    in assetsFile.file.AssetInfos)
                {
                    index++;

                    long pathId =
                        assetInfo.PathId;

                    int typeId =
                        assetInfo.TypeId;

                    output.AppendLine(
                        $"   {index}. " +
                        $"TypeID: {typeId}  " +
                        $"PathID: {pathId}"
                    );
                }

                output.AppendLine();
            }
            catch (Exception ex)
            {
                output.AppendLine(
                    "   ❌ Error membaca asset:"
                );

                output.AppendLine(
                    $"   {ex.Message}"
                );

                output.AppendLine();
            }
        }

        SetStatus(
            output.ToString()
        );
    }

    // =========================================================
    // STATUS
    // =========================================================

    private void SetStatus(
        string text)
    {
        RunOnUiThread(() =>
        {
            if (status != null)
            {
                status.Text = text;
            }
        });
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
