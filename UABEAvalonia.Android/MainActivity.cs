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


        protected override void OnCreate(
            Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

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


            TextView title =
                new TextView(this);

            title.Text =
                "UABEA Android";

            title.TextSize =
                24;


            Button openButton =
                new Button(this);

            openButton.Text =
                "OPEN UNITY3D";


            openButton.Click +=
                delegate
                {
                    OpenFilePicker();
                };


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
                    "AssetsTools.NET gagal dimuat.";
            }


            ScrollView scroll =
                new ScrollView(this);

            scroll.AddView(status);


            layout.AddView(title);

            layout.AddView(openButton);


            LinearLayout.LayoutParams parameters =
                new LinearLayout.LayoutParams(
                    -1,
                    0
                );

            parameters.Weight = 1;


            layout.AddView(
                scroll,
                parameters
            );


            SetContentView(layout);
        }


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


            AndroidUri uri =
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
                OpenUnityFile(uri);
            }
            catch (Exception ex)
            {
                ShowError(
                    "Gagal membuka Unity3D",
                    ex
                );
            }
        }


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
                ContentResolver.OpenInputStream(uri);


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
                    input.CopyTo(output);
                }
            }


            return path;
        }


        private void OpenUnityFile(
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
                CopyFileToCache(
                    uri,
                    fileName
                );


            FileInfo info =
                new FileInfo(cachePath);


            long size =
                info.Length;


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
                    "LoadBundleFile mengembalikan null."
                );
            }


            var directoryInfos =
                bundle.file
                    .BlockAndDirInfo
                    .DirectoryInfos;


            int fileCount =
                directoryInfos.Count;


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
                size.ToString("N0") +
                " bytes"
            );

            result.AppendLine();


            result.AppendLine(
                "File dalam Bundle: " +
                fileCount
            );

            result.AppendLine();


            result.AppendLine(
                "=== SERIALIZED FILE ==="
            );

            result.AppendLine();


            for (
                int i = 0;
                i < fileCount;
                i++)
            {
                var directory =
                    directoryInfos[i];


                result.AppendLine(
                    (i + 1) +
                    ". " +
                    directory.Name
                );


                try
                {
                    AssetsFileInstance assetsFile =
                        assetsManager
                            .LoadAssetsFileFromBundle(
                                bundle,
                                i,
                                false
                            );


                    if (assetsFile == null)
                    {
                        result.AppendLine(
                            "   Gagal memuat."
                        );

                        result.AppendLine();

                        continue;
                    }


                    string unityVersion =
                        assetsFile
                            .file
                            .Metadata
                            .UnityVersion;


                    result.AppendLine(
                        "   Unity Version: " +
                        unityVersion
                    );


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


                    result.AppendLine(
                        "   === ASSET LIST ==="
                    );


                    int assetNumber = 0;


                    foreach (
                        var assetInfo
                        in assetsFile.file.AssetInfos)
                    {
                        assetNumber++;


                        result.AppendLine(
                            "   " +
                            assetNumber +
                            ". TypeID: " +
                            assetInfo.TypeId +
                            " PathID: " +
                            assetInfo.PathId
                        );
                    }


                    result.AppendLine();
                }
                catch (Exception ex)
                {
                    result.AppendLine(
                        "   ERROR: " +
                        ex.Message
                    );

                    result.AppendLine();
                }
            }


            SetStatus(
                result.ToString()
            );
        }


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
