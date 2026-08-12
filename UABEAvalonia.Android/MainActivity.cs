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

        private TextView status;

        private AssetsManager assetsManager;


        // =====================================================
        // ON CREATE
        // =====================================================

        protected override void OnCreate(
            Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            // =================================================
            // AssetsManager
            // =================================================

            try
            {
                assetsManager =
                    new AssetsManager();
            }
            catch (Exception ex)
            {
                assetsManager = null;

                System.Diagnostics.Debug.WriteLine(
                    ex.ToString()
                );
            }


            // =================================================
            // Layout utama
            // =================================================

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


            // =================================================
            // Judul
            // =================================================

            TextView title =
                new TextView(this);

            title.Text =
                "UABEA Android";

            title.TextSize =
                24;


            // =================================================
            // Tombol
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
                17;


            if (assetsManager != null)
            {
                status.Text =
                    "AssetsTools.NET berhasil dimuat.\n\n" +
                    "Pilih file Unity3D.";
            }
            else
            {
                status.Text =
                    "❌ AssetsTools.NET gagal dimuat.";
            }


            // =================================================
            // Scroll
            // =================================================

            ScrollView scroll =
                new ScrollView(this);

            scroll.AddView(status);


            // =================================================
            // Masukkan komponen
            // =================================================

            layout.AddView(
                title
            );

            layout.AddView(
                openButton
            );


            LinearLayout.LayoutParams parameters =
                new LinearLayout.LayoutParams(
                    -1,
                    0
                );

            parameters.Weight =
                1;


            layout.AddView(
                scroll,
                parameters
            );


            // =================================================
            // Tampilkan
            // =================================================

            SetContentView(
                layout
            );
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
                    "File Picker gagal",
                    ex
                );
            }
        }


        // =====================================================
        // HASIL FILE PICKER
        // =====================================================

        protected override void OnActivityResult(
            int requestCode,
            Result resultCode,
            Intent data)
        {
            base.OnActivityResult(
                requestCode,
                resultCode,
                data
            );


            if (
                requestCode !=
                PickFileRequestCode)
            {
                return;
            }


            if (
                resultCode !=
                Result.Ok)
            {
                SetStatus(
                    "Pemilihan file dibatalkan."
                );

                return;
            }


            if (data == null)
            {
                SetStatus(
                    "❌ Data file tidak ditemukan."
                );

                return;
            }


            AndroidUri uri =
                data.Data;


            if (uri == null)
            {
                SetStatus(
                    "❌ URI file tidak ditemukan."
                );

                return;
            }


            try
            {
                OpenUnityFile(
                    uri
                );
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
        // NAMA FILE
        // =====================================================

        private string GetFileName(
            AndroidUri uri)
        {
            string fileName =
                "temp.unity3d";


            var cursor =
                ContentResolver.Query(
                    uri,
                    null,
                    null,
                    null,
                    null
                );


            if (cursor != null)
            {
                using (cursor)
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


            // =================================================
            // Bersihkan nama
            // =================================================

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


        // =====================================================
        // COPY FILE KE CACHE
        // =====================================================

        private string CopyFileToCache(
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


            var input =
                ContentResolver.OpenInputStream(
                    uri
                );


            if (input == null)
            {
                throw new Exception(
                    "File tidak dapat dibaca."
                );
            }


            using (input)
            {
                using (
                    FileStream output =
                        File.Create(path))
                {
                    input.CopyTo(
                        output
                    );
                }
            }


            return path;
        }


        // =====================================================
        // BUKA UNITY3D
        // =====================================================

        private void OpenUnityFile(
            AndroidUri uri)
        {
            if (assetsManager == null)
            {
                throw new Exception(
                    "AssetsManager tidak tersedia."
                );
            }


            // =================================================
            // Nama file
            // =================================================

            SetStatus(
                "⏳ Membaca nama file..."
            );


            string fileName =
                GetFileName(
                    uri
                );


            // =================================================
            // Copy ke cache
            // =================================================

            SetStatus(
                "⏳ Menyalin file..."
            );


            string cachePath =
                CopyFileToCache(
                    uri,
                    fileName
                );


            // =================================================
            // Ukuran
            // =================================================

            FileInfo fileInfo =
                new FileInfo(
                    cachePath
                );


            long fileSize =
                fileInfo.Length;


            // =================================================
            // Buka Bundle
            // =================================================

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
            // DirectoryInfos
            // =================================================

            var directoryInfos =
                bundle.file
                    .BlockAndDirInfo
                    .DirectoryInfos;


            // =================================================
            // Hitung file secara manual
            // Tidak menggunakan .Count
            // =================================================

            int fileCount =
                0;


            foreach (
                var directory
                in directoryInfos)
            {
                fileCount++;
            }


            // =================================================
            // Output
            // =================================================

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
