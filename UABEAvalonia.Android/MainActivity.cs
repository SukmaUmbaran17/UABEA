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


        // =========================================================
        // ON CREATE
        // =========================================================

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // -----------------------------------------------------
            // BUAT UI TERLEBIH DAHULU
            // -----------------------------------------------------

            LinearLayout root = new LinearLayout(this);

            root.Orientation = Orientation.Vertical;

            root.SetPadding(
                30,
                30,
                30,
                30
            );


            // -----------------------------------------------------
            // JUDUL
            // -----------------------------------------------------

            TextView title = new TextView(this);

            title.Text = "UABEA Android";

            title.TextSize = 24;


            // -----------------------------------------------------
            // TOMBOL
            // -----------------------------------------------------

            Button openButton = new Button(this);

            openButton.Text = "OPEN UNITY3D";


            // -----------------------------------------------------
            // STATUS
            // -----------------------------------------------------

            status = new TextView(this);

            status.TextSize = 16;

            status.Text =
                "Menyiapkan UABEA Android...\n";


            // -----------------------------------------------------
            // SCROLL
            // -----------------------------------------------------

            ScrollView scrollView = new ScrollView(this);

            scrollView.AddView(status);


            // -----------------------------------------------------
            // MASUKKAN KE ROOT
            // -----------------------------------------------------

            root.AddView(title);

            root.AddView(openButton);


            LinearLayout.LayoutParams scrollParams =
                new LinearLayout.LayoutParams(
                    -1,
                    0
                );

            scrollParams.Weight = 1;

            root.AddView(
                scrollView,
                scrollParams
            );


            // -----------------------------------------------------
            // TAMPILKAN UI
            // -----------------------------------------------------

            SetContentView(root);


            // -----------------------------------------------------
            // EVENT TOMBOL
            // -----------------------------------------------------

            openButton.Click += delegate
            {
                OpenFilePicker();
            };


            // -----------------------------------------------------
            // BARU SEKARANG BUAT ASSETSMANAGER
            // -----------------------------------------------------

            try
            {
                assetsManager = new AssetsManager();

                SetStatus(
                    "✅ AssetsTools.NET siap.\n\n" +
                    "Tekan OPEN UNITY3D untuk memilih file."
                );
            }
            catch (Exception ex)
            {
                assetsManager = null;

                SetStatus(
                    "❌ AssetsTools.NET gagal dimuat.\n\n" +
                    ex.Message
                );
            }
        }


        // =========================================================
        // UNITY TYPE ID
        // =========================================================

        private string GetUnityTypeName(int typeId)
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
                    return "TypeID " + typeId;
            }
        }


        // =========================================================
        // FILE PICKER
        // =========================================================

        private void OpenFilePicker()
        {
            try
            {
                Intent intent =
                    new Intent(Intent.ActionOpenDocument);

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
                SetStatus(
                    "❌ File Picker gagal.\n\n" +
                    ex.Message
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
                    "❌ Data file kosong."
                );

                return;
            }


            AndroidUri? uri = data.Data;


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
                SetStatus(
                    "❌ Gagal membuka file.\n\n" +
                    ex.Message
                );

                System.Diagnostics.Debug.WriteLine(
                    ex.ToString()
                );
            }
        }


        // =========================================================
        // GET FILE NAME
        // =========================================================

        private string GetFileName(AndroidUri uri)
        {
            string fileName = "temp.unity3d";


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
                            fileName = detectedName;
                        }
                    }
                }
            }


            // -----------------------------------------------------
            // AMANKAN NAMA FILE
            // -----------------------------------------------------

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
        // COPY FILE TO CACHE
        // =========================================================

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


            string cachePath =
                Path.Combine(
                    CacheDir.AbsolutePath,
                    fileName
                );


            using (
                Stream? input =
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
        // READ UNITY BUNDLE
        // =========================================================

        private void ReadUnityBundle(AndroidUri uri)
        {
            if (assetsManager == null)
            {
                throw new Exception(
                    "AssetsManager belum tersedia."
                );
            }


            SetStatus(
                "⏳ Membaca file..."
            );


            // -----------------------------------------------------
            // NAMA FILE
            // -----------------------------------------------------

            string fileName =
                GetFileName(uri);


            SetStatus(
                "⏳ Menyalin file ke cache..."
            );


            // -----------------------------------------------------
            // COPY
            // -----------------------------------------------------

            string cachePath =
                CopyToCache(
                    uri,
                    fileName
                );


            FileInfo info =
                new FileInfo(cachePath);


            long fileSize =
                info.Length;


            SetStatus(
                "⏳ Membuka AssetBundle..."
            );


            // -----------------------------------------------------
            // LOAD BUNDLE
            // -----------------------------------------------------

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


            // -----------------------------------------------------
            // DIRECTORY
            // -----------------------------------------------------

            var directories =
                bundle.file
                    .BlockAndDirInfo
                    .DirectoryInfos;


            int directoryCount = 0;

            foreach (var directory in directories)
            {
                directoryCount++;
            }


            // -----------------------------------------------------
            // HASIL
            // -----------------------------------------------------

            StringBuilder result =
                new StringBuilder();


            result.AppendLine(
                "ASSETBUNDLE BERHASIL DIBUKA!"
            );

            result.AppendLine();

            result.AppendLine(
                "Nama: " +
                fileName
            );

            result.AppendLine(
                "Ukuran: " +
                fileSize.ToString("N0") +
                " bytes"
            );

            result.AppendLine();

            result.AppendLine(
                "File dalam Bundle: " +
                directoryCount
            );

            result.AppendLine();

            result.AppendLine(
                "=== SERIALIZED FILE ==="
            );

            result.AppendLine();


            // -----------------------------------------------------
            // LOAD SETIAP FILE DALAM BUNDLE
            // -----------------------------------------------------

            int fileNumber = 0;


            foreach (var directory in directories)
            {
                fileNumber++;


                result.AppendLine(
                    fileNumber +
                    ". " +
                    directory.Name
                );


                try
                {
                    SetStatus(
                        "⏳ Membaca serialized file " +
                        fileNumber +
                        "..."
                    );


                    AssetsFileInstance assetsFile =
                        assetsManager.LoadAssetsFileFromBundle(
                            bundle,
                            fileNumber - 1,
                            false
                        );


                    if (assetsFile == null)
                    {
                        result.AppendLine(
                            "   ❌ Tidak dapat dimuat."
                        );

                        result.AppendLine();

                        continue;
                    }


                    // -------------------------------------------------
                    // UNITY VERSION
                    // -------------------------------------------------

                    string unityVersion =
                        assetsFile
                            .file
                            .Metadata
                            .UnityVersion;


                    result.AppendLine(
                        "   Unity Version: " +
                        unityVersion
                    );


                    // -------------------------------------------------
                    // ASSET COUNT
                    // -------------------------------------------------

                    int assetCount = 0;


                    foreach (
                        var assetInfo
                        in assetsFile.file.AssetInfos)
                    {
                        assetCount++;
                    }


                    result.AppendLine(
                        "   Asset Count: " +
                        assetCount
                    );


                    result.AppendLine();


                    // -------------------------------------------------
                    // ASSET LIST
                    // -------------------------------------------------

                    result.AppendLine(
                        "   === ASSET LIST ==="
                    );

                    result.AppendLine();


                    int assetNumber = 0;


                    foreach (
                        var assetInfo
                        in assetsFile.file.AssetInfos)
                    {
                        assetNumber++;


                        string typeName =
                            GetUnityTypeName(
                                assetInfo.TypeId
                            );


                        result.AppendLine(
                            "   " +
                            assetNumber +
                            ". " +
                            typeName
                        );


                        result.AppendLine(
                            "      TypeID: " +
                            assetInfo.TypeId
                        );


                        result.AppendLine(
                            "      PathID: " +
                            assetInfo.PathId
                        );


                        result.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    result.AppendLine(
                        "   ❌ ERROR: " +
                        ex.Message
                    );

                    result.AppendLine();
                }
            }


            // -----------------------------------------------------
            // TAMPILKAN HASIL
            // -----------------------------------------------------

            SetStatus(
                result.ToString()
            );
        }


        // =========================================================
        // SET STATUS
        // =========================================================

        private void SetStatus(string message)
        {
            RunOnUiThread(delegate
            {
                if (status != null)
                {
                    status.Text = message;
                }
            });
        }
    }
}
