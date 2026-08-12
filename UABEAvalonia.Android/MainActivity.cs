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
        private LinearLayout? assetContainer;

        private AssetsManager? assetsManager;

        private BundleFileInstance? currentBundle;
        private AssetsFileInstance? currentAssetsFile;


        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                assetsManager = new AssetsManager();
            }
            catch (Exception ex)
            {
                assetsManager = null;
                System.Diagnostics.Debug.WriteLine(ex);
            }

            LinearLayout root = new LinearLayout(this);
            root.Orientation = Orientation.Vertical;
            root.SetPadding(24, 24, 24, 24);

            TextView title = new TextView(this);
            title.Text = "UABEA Android";
            title.TextSize = 24;

            Button openButton = new Button(this);
            openButton.Text = "OPEN UNITY3D";

            openButton.Click += delegate
            {
                OpenFilePicker();
            };

            status = new TextView(this);
            status.TextSize = 16;

            if (assetsManager != null)
            {
                status.Text =
                    "AssetsTools.NET siap.\n\n" +
                    "Tekan OPEN UNITY3D.";
            }
            else
            {
                status.Text =
                    "AssetsTools.NET gagal dimuat.";
            }

            assetContainer = new LinearLayout(this);
            assetContainer.Orientation = Orientation.Vertical;

            ScrollView scroll = new ScrollView(this);
            scroll.AddView(assetContainer);

            root.AddView(title);
            root.AddView(openButton);
            root.AddView(status);

            LinearLayout.LayoutParams scrollParams =
                new LinearLayout.LayoutParams(
                    -1,
                    0
                );

            scrollParams.Weight = 1;

            root.AddView(
                scroll,
                scrollParams
            );

            SetContentView(root);
        }


        // =====================================================
        // FILE PICKER
        // =====================================================

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
                ShowError(
                    "File Picker gagal",
                    ex
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

            if (data == null || data.Data == null)
            {
                SetStatus(
                    "File tidak ditemukan."
                );

                return;
            }

            try
            {
                ReadBundle(data.Data);
            }
            catch (Exception ex)
            {
                ShowError(
                    "Gagal membuka Unity3D",
                    ex
                );
            }
        }


        // =====================================================
        // GET FILE NAME
        // =====================================================

        private string GetFileName(AndroidUri uri)
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
                    int index =
                        cursor.GetColumnIndex(
                            OpenableColumns.DisplayName
                        );

                    if (index >= 0 && cursor.MoveToFirst())
                    {
                        string? name =
                            cursor.GetString(index);

                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            fileName = name;
                        }
                    }
                }
            }

            return fileName;
        }


        // =====================================================
        // COPY FILE
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
        // READ BUNDLE
        // =====================================================

        private void ReadBundle(AndroidUri uri)
        {
            if (assetsManager == null)
            {
                throw new Exception(
                    "AssetsManager tidak tersedia."
                );
            }

            SetStatus(
                "Membaca file..."
            );

            string fileName =
                GetFileName(uri);

            string cachePath =
                CopyToCache(
                    uri,
                    fileName
                );

            FileInfo info =
                new FileInfo(cachePath);

            SetStatus(
                "Membuka AssetBundle..."
            );

            Bundle
