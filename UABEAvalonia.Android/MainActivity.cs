using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;

using AssetsTools.NET.Extra;

using System;
using System.IO;
using System.Text;

using AndroidUri = global::Android.Net.Uri;

namespace UABEAvalonia.Android
{
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

            // =================================================
            // AssetsManager
            // =================================================

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


            // =================================================
            // Layout
            // =================================================

            LinearLayout layout =
                new LinearLayout(this);

            layout.Orientation =
                Orientation.Vertical;

            layout.SetPadding(
                30,
                30,
                30,
                30
            );


            // =================================================
            // Title
            // =================================================

            TextView title =
                new TextView(this);

            title.Text =
                "UABEA Android";

            title.TextSize =
                24;


            // =================================================
            // Open button
            // =================================================

            Button openButton =
                new Button(this);

            openButton.Text =
                "OPEN UNITY3D";


            openButton.Click +=
                delegate
                {
                    OpenFilePicker();
                };


            // =================================================
            // Status
            // =================================================

            status =
                new TextView(this);

            status.TextSize =
                16;


            if (assetsManager != null)
            {
                status.Text =
                    "✅ AssetsTools.NET siap.\n\n" +
                    "Tekan OPEN UNITY3D.";
            }
            else
            {
                status.Text =
                    "❌ AssetsTools.NET gagal dimuat.";
            }


            // =================================================
            // ScrollView
            // =================================================

            ScrollView scroll =
                new ScrollView(this);

            scroll.AddView(status);


            // =================================================
            // Layout
            // =================================================

            layout.AddView(title);

            layout.AddView(openButton);


            LinearLayout.LayoutParams scrollParams =
                new LinearLayout.LayoutParams(
                    -1,
                    0
                );

            scrollParams.Weight =
                1;


            layout.AddView(
                scroll,
                scrollParams
            );


            SetContentView(layout);
        }


        // =====================================================
        // TYPE ID → NAMA UNITY
        // =====================================================

        private string GetUnityTypeName(
            int typeId)
        {
            switch (typeId)
            {
                case 1:
                    return "GameObject";

                case 4:
                    return "Transform";

                case 21:
                    return "Material";

                case 28:
                    return "Texture2D";

                case 43:
                    return "Mesh";

                case 48:
                    return "Shader";

                case 74:
                    return "AnimationClip";

                case 83:
                    return "AudioClip";

                case 114:
                    return "MonoBehaviour";

                case 115:
                    return "MonoScript";

                case 142:
                    return "AssetBundle";

                case 198:
                    return "ParticleSystem";

                case 199:
                    return "ParticleSystemRenderer";

                default:
                    return "Unknown";
            }
        }


        // =====================================================
        // FILE PICKER
        // =====================================================

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

                intent.SetType("*/*");


                StartActivityForResult(
                    intent,
                    PickFileRequestCode
                );
            }
            catch (Exception ex)
            {
                ShowError(
                    "File Picker gagal",
                    ex
                );
            }
        }


        // =====================================================
        // FILE PICKER RESULT
        // =====================================================

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
                    "❌ Data file kosong."
                );

                return;
            }


            AndroidUri? uri =
                data.Data;


            if (uri == null)
            {
                SetStatus(
                    "❌ URI file kosong."
                );

                return;
            }


            try
            {
                ReadUnityBundle(uri);
            }
            catch (Exception ex)
            {
                ShowError(
                    "Gagal membuka file",
                    ex
                );
            }
        }


        // =====================================================
        // GET FILE NAME
        // =====================================================

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


            return fileName;
        }


        // =====================================================
        // COPY FILE TO CACHE
        // =====================================================

        private string CopyToCache(
            AndroidUri uri,
            string fileName)
        {
            if (CacheDir == null)
            {
                throw new Exception(
                    "CacheDir tidak tersedia."
                );
            }


            string path =
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
                        File.Create(path))
                {
                    input.CopyTo(output);
                }
            }


            return path;
        }


        // =====================================================
        // READ UNITY BUNDLE
        // =====================================================

        private void ReadUnityBundle(
            AndroidUri uri)
        {
            if (assetsManager == null)
            {
                throw new Exception(
                    "AssetsManager tidak tersedia."
                );
            }


            SetStatus(
                "⏳ Membaca nama file..."
            );


            string fileName =
                GetFileName(uri);


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


            SetStatus(
                "⏳ Membuka AssetBundle..."
            );


            BundleFileInstance bundle =
                assetsManager.LoadBundleFile(
                    cachePath
                );


            if (bundle == null)
            {
                throw new Exception(
                    "AssetBundle gagal dibuka."
                );
            }


            // =================================================
            // DIRECTORY INFOS
            // =================================================

            var directories =
                bundle.file
                    .BlockAndDirInfo
                    .DirectoryInfos;


            int directoryCount =
                0;


            foreach (
                var directory
                in directories)
            {
                directoryCount++;
            }


            // =================================================
            // OUTPUT
            // =================================================

            StringBuilder text =
                new StringBuilder();


            text.AppendLine(
                "ASSETBUNDLE BERHASIL DIBUKA!"
            );

            text.AppendLine();


            text.AppendLine(
                "Nama: " +
                fileName
            );


            text.AppendLine(
                "Ukuran: " +
                fileSize.ToString("N0") +
                " bytes"
            );


            text.AppendLine();


            text.AppendLine(
                "File dalam Bundle: " +
                directoryCount
            );


            text.AppendLine();


            text.AppendLine(
                "=== SERIALIZED FILE ==="
            );


            text.AppendLine();


            // =================================================
            // SERIALIZED FILE
            // =================================================

            int fileNumber =
                0;


            foreach (
                var directory
                in directories)
            {
                fileNumber++;


                text.AppendLine(
                    fileNumber +
                    ". " +
                    directory.Name
                );


                try
                {
                    AssetsFileInstance assetsFile =
                        assetsManager
                            .LoadAssetsFileFromBundle(
                                bundle,
                                fileNumber - 1,
                                false
                            );


                    if (assetsFile == null)
                    {
                        text.AppendLine(
                            "   ❌ Tidak dapat dimuat."
                        );

                        text.AppendLine();

                        continue;
                    }


                    // =========================================
                    // UNITY VERSION
                    // =========================================

                    string unityVersion =
                        assetsFile
                            .file
                            .Metadata
                            .UnityVersion;


                    text.AppendLine(
                        "   Unity Version: " +
                        unityVersion
                    );


                    // =========================================
                    // ASSET COUNT
                    // =========================================

                    int assetCount =
                        0;


                    foreach (
                        var asset
                        in assetsFile.file.AssetInfos)
                    {
                        assetCount++;
                    }


                    text.AppendLine(
                        "   Asset Count: " +
                        assetCount
                    );


                    text.AppendLine();


                    // =========================================
                    // ASSET LIST
                    // =========================================

                    text.AppendLine(
                        "   === ASSET LIST ==="
                    );


                    int assetNumber =
                        0;


                    foreach (
                        var asset
                        in assetsFile.file.AssetInfos)
                    {
                        assetNumber++;


                        string typeName =
                            GetUnityTypeName(
                                asset.TypeId
                            );


                        text.AppendLine(
                            "   " +
                            assetNumber +
                            ". " +
                            typeName
                        );


                        text.AppendLine(
                            "      TypeID: " +
                            asset.TypeId
                        );


                        text.AppendLine(
                            "      PathID: " +
                            asset.PathId
                        );


                        text.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    text.AppendLine(
                        "   ❌ ERROR: " +
                        ex.Message
                    );


                    text.AppendLine();
                }
            }


            // =================================================
            // TAMPILKAN HASIL
            // =================================================

            SetStatus(
                text.ToString()
            );
        }


        // =====================================================
        // STATUS
        // =====================================================

        private void SetStatus(
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


        // =====================================================
        // ERROR
        // =====================================================

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
                title
            );


            System.Diagnostics.Debug.WriteLine(
                ex.ToString()
            );
        }
    }
}
