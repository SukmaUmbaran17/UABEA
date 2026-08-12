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
        private LinearLayout? assetList;

        private AssetsManager? assetsManager;


        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            LinearLayout root =
                new LinearLayout(this);

            root.Orientation =
                Orientation.Vertical;

            root.SetPadding(
                30,
                30,
                30,
                30
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


            status =
                new TextView(this);

            status.TextSize =
                16;

            status.Text =
                "Menyiapkan UABEA Android...";


            assetList =
                new LinearLayout(this);

            assetList.Orientation =
                Orientation.Vertical;


            ScrollView scroll =
                new ScrollView(this);

            scroll.AddView(
                assetList
            );


            root.AddView(
                title
            );

            root.AddView(
                openButton
            );

            root.AddView(
                status
            );


            LinearLayout.LayoutParams scrollParams =
                new LinearLayout.LayoutParams(
                    -1,
                    0
                );

            scrollParams.Weight =
                1;


            root.AddView(
                scroll,
                scrollParams
            );


            SetContentView(
                root
            );


            openButton.Click += delegate
            {
                OpenFilePicker();
            };


            try
            {
                assetsManager =
                    new AssetsManager();


                SetStatus(
                    "AssetsTools.NET siap.\n\n" +
                    "Tekan OPEN UNITY3D."
                );
            }
            catch (Exception ex)
            {
                SetStatus(
                    "AssetsTools.NET gagal dimuat.\n\n" +
                    ex.Message
                );
            }
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
                SetStatus(
                    "File Picker gagal.\n\n" +
                    ex.Message
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


            if (
                data == null ||
                data.Data == null)
            {
                SetStatus(
                    "URI file tidak ditemukan."
                );

                return;
            }


            try
            {
                ReadUnityBundle(
                    data.Data
                );
            }
            catch (Exception ex)
            {
                SetStatus(
                    "Gagal membuka file.\n\n" +
                    ex.Message
                );
            }
        }


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
                    int index =
                        cursor.GetColumnIndex(
                            OpenableColumns.DisplayName
                        );


                    if (
                        index >= 0 &&
                        cursor.MoveToFirst())
                    {
                        string? detected =
                            cursor.GetString(
                                index
                            );


                        if (
                            !string.IsNullOrWhiteSpace(
                                detected))
                        {
                            fileName =
                                detected;
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
                Stream? input =
                    ContentResolver.OpenInputStream(
                        uri))
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
                    input.CopyTo(
                        output
                    );
                }
            }


            return path;
        }


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
                "Menyalin file..."
            );


            string fileName =
                GetFileName(
                    uri
                );


            string cachePath =
                CopyToCache(
                    uri,
                    fileName
                );


            FileInfo info =
                new FileInfo(
                    cachePath
                );


            SetStatus(
                "Membuka AssetBundle..."
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


            var directories =
                bundle.file
                    .BlockAndDirInfo
                    .DirectoryInfos;


            int directoryCount =
                0;


            foreach (
                var item
                in directories)
            {
                directoryCount++;
            }


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
                info.Length.ToString("N0") +
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


            ClearAssetList();


            int fileNumber =
                0;


            foreach (
                var directory
                in directories)
            {
                int currentIndex =
                    fileNumber;


                string currentName =
                    directory.Name;


                fileNumber++;


                result.AppendLine(
                    fileNumber +
                    ". " +
                    currentName
                );


                Button fileButton =
                    new Button(this);


                fileButton.Text =
                    currentName;


                fileButton.Click += delegate
                {
                    LoadSerializedFile(
                        bundle,
                        currentIndex
                    );
                };


                if (assetList != null)
                {
                    assetList.AddView(
                        fileButton
                    );
                }


                try
                {
                    AssetsFileInstance assetsFile =
                        assetsManager.LoadAssetsFileFromBundle(
                            bundle,
                            currentIndex,
                            false
                        );


                    if (assetsFile == null)
                    {
                        result.AppendLine(
                            "   Tidak dapat dimuat."
                        );


                        result.AppendLine();


                        continue;
                    }


                    string unityVersion =
                        assetsFile
                            .file
                            .Metadata
                            .UnityVersion;


                    int assetCount =
                        0;


                    foreach (
                        var asset
                        in assetsFile.file.AssetInfos)
                    {
                        assetCount++;
                    }


                    result.AppendLine(
                        "   Unity Version: " +
                        unityVersion
                    );


                    result.AppendLine(
                        "   Asset Count: " +
                        assetCount
                    );


                    result.AppendLine();


                    int assetNumber =
                        0;


                    foreach (
                        var asset
                        in assetsFile.file.AssetInfos)
                    {
                        assetNumber++;


                        result.AppendLine(
                            assetNumber +
                            ". TypeID: " +
                            asset.TypeId +
                            " | PathID: " +
                            asset.PathId
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


        private void LoadSerializedFile(
            BundleFileInstance bundle,
            int index)
        {
            if (assetsManager == null)
            {
                return;
            }


            try
            {
                AssetsFileInstance assetsFile =
                    assetsManager.LoadAssetsFileFromBundle(
                        bundle,
                        index,
                        false
                    );


                if (assetsFile == null)
                {
                    SetStatus(
                        "SerializedFile tidak dapat dimuat."
                    );

                    return;
                }


                StringBuilder result =
                    new StringBuilder();


                result.AppendLine(
                    "=== SERIALIZED FILE ==="
                );


                result.AppendLine();


                result.AppendLine(
                    "Unity Version: " +
                    assetsFile
                        .file
                        .Metadata
                        .UnityVersion
                );


                int count =
                    0;


                foreach (
                    var asset
                    in assetsFile.file.AssetInfos)
                {
                    count++;
                }


                result.AppendLine(
                    "Asset Count: " +
                    count
                );


                result.AppendLine();


                result.AppendLine(
                    "=== ASSET LIST ==="
                );


                result.AppendLine();


                int number =
                    0;


                ClearAssetList();


                foreach (
                    var asset
                    in assetsFile.file.AssetInfos)
                {
                    number++;


                    AssetFileInfo currentAsset =
                        asset;


                    result.AppendLine(
                        number +
                        ". TypeID: " +
                        currentAsset.TypeId +
                        " | PathID: " +
                        currentAsset.PathId
                    );


                    Button assetButton =
                        new Button(this);


                    assetButton.Text =
                        "Asset #" +
                        number +
                        " | TypeID " +
                        currentAsset.TypeId;


                    assetButton.Click += delegate
                    {
                        LoadAssetInspector(
                            assetsFile,
                            currentAsset
                        );
                    };


                    if (assetList != null)
                    {
                        assetList.AddView(
                            assetButton
                        );
                    }
                }


                SetStatus(
                    result.ToString()
                );
            }
            catch (Exception ex)
            {
                SetStatus(
                    "Gagal membaca SerializedFile.\n\n" +
                    ex.Message
                );
            }
        }


        private void LoadAssetInspector(
            AssetsFileInstance assetsFile,
            AssetFileInfo asset)
        {
            if (assetsManager == null)
            {
                return;
            }


            try
            {
                StringBuilder result =
                    new StringBuilder();


                result.AppendLine(
                    "=== ASSET INSPECTOR ==="
                );


                result.AppendLine();


                result.AppendLine(
                    "TypeID: " +
                    asset.TypeId
                );


                result.AppendLine(
                    "PathID: " +
                    asset.PathId
                );


                result.AppendLine();


                result.AppendLine(
                    "=== SERIALIZED FIELDS ==="
                );


                result.AppendLine();


                AssetTypeValueField baseField =
                    assetsManager.GetBaseField(
                        assetsFile,
                        asset
                    );


                if (baseField == null)
                {
                    result.AppendLine(
                        "BaseField tidak tersedia."
                    );


                    SetStatus(
                        result.ToString()
                    );


                    return;
                }


                DumpField(
                    baseField,
                    "",
                    result
                );


                SetStatus(
                    result.ToString()
                );
            }
            catch (Exception ex)
            {
                SetStatus(
                    "Gagal membaca Asset.\n\n" +
                    ex.Message
                );
            }
        }


        private void DumpField(
            AssetTypeValueField field,
            string indent,
            StringBuilder result)
        {
            if (field == null)
            {
                return;
            }


            string fieldName =
                field.FieldName ??
                "(unnamed)";


            string value =
                "";


            try
            {
                if (field.Value != null)
                {
                    value =
                        field.Value.AsString;
                }
            }
            catch
            {
                value =
                    "";
            }


            if (!string.IsNullOrEmpty(value))
            {
                result.AppendLine(
                    indent +
                    fieldName +
                    " = " +
                    value
                );
            }
            else
            {
                result.AppendLine(
                    indent +
                    fieldName
                );
            }


            if (field.Children != null)
            {
                foreach (
                    AssetTypeValueField child
                    in field.Children)
                {
                    DumpField(
                        child,
                        indent + "  ",
                        result
                    );
                }
            }
        }


        private void ClearAssetList()
        {
            if (assetList == null)
            {
                return;
            }


            assetList.RemoveAllViews();
        }


        private void SetStatus(
            string message)
        {
            RunOnUiThread(delegate
            {
                if (status != null)
                {
                    status.Text =
                        message;
                }
            });
        }
    }
}
