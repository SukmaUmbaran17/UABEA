using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;

using AssetsTools.NET.Extra;

using System;
using System.Collections.Generic;
using System.IO;

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

        private LinearLayout? mainLayout;
        private TextView? status;

        private AssetsManager? assetsManager;

        private readonly List<AssetInfoData> assetList =
            new List<AssetInfoData>();


        // =====================================================
        // DATA ASSET
        // =====================================================

        private class AssetInfoData
        {
            public int Number { get; set; }

            public int TypeId { get; set; }

            public long PathId { get; set; }

            public string TypeName { get; set; } = "Unknown";

            public string FileName { get; set; } = "";
        }


        // =====================================================
        // ON CREATE
        // =====================================================

        protected override void OnCreate(
            Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            // =================================================
            // ASSETS MANAGER
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
            // MAIN LAYOUT
            // =================================================

            mainLayout =
                new LinearLayout(this);

            mainLayout.Orientation =
                Orientation.Vertical;

            mainLayout.SetPadding(
                30,
                30,
                30,
                30
            );


            // =================================================
            // TITLE
            // =================================================

            TextView title =
                new TextView(this);

            title.Text =
                "UABEA Android";

            title.TextSize =
                24;


            // =================================================
            // OPEN BUTTON
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
            // STATUS
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
            // SCROLL
            // =================================================

            ScrollView scroll =
                new ScrollView(this);

            scroll.AddView(status);


            // =================================================
            // ADD VIEW
            // =================================================

            mainLayout.AddView(title);

            mainLayout.AddView(openButton);


            LinearLayout.LayoutParams scrollParams =
                new LinearLayout.LayoutParams(
                    -1,
                    0
                );

            scrollParams.Weight =
                1;


            mainLayout.AddView(
                scroll,
                scrollParams
            );


            SetContentView(mainLayout);
        }


        // =====================================================
        // UNITY TYPE NAME
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
        // COPY TO CACHE
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
                "⏳ Membaca file..."
            );


            string fileName =
                GetFileName(uri);


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
            // DIRECTORY
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
            // CLEAR OLD ASSETS
            // =================================================

            assetList.Clear();


            // =================================================
            // HEADER
            // =================================================

            LinearLayout assetLayout =
                CreateAssetLayout();


            AddHeader(
                assetLayout,
                fileName,
                fileSize,
                directoryCount
            );


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


                AddText(
                    assetLayout,
                    ""
                );


                AddText(
                    assetLayout,
                    "=== SERIALIZED FILE " +
                    fileNumber +
                    " ==="
                );


                AddText(
                    assetLayout,
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
                        AddText(
                            assetLayout,
                            "❌ Tidak dapat dimuat."
                        );

                        continue;
                    }


                    string unityVersion =
                        assetsFile
                            .file
                            .Metadata
                            .UnityVersion;


                    AddText(
                        assetLayout,
                        "Unity Version: " +
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


                    AddText(
                        assetLayout,
                        "Asset Count: " +
                        assetCount
                    );


                    AddText(
                        assetLayout,
                        ""
                    );


                    AddText(
                        assetLayout,
                        "=== ASSET LIST ==="
                    );


                    // =========================================
                    // ASSET BUTTONS
                    // =========================================

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


                        AssetInfoData info =
                            new AssetInfoData
                            {
                                Number =
                                    assetNumber,

                                TypeId =
                                    asset.TypeId,

                                PathId =
                                    asset.PathId,

                                TypeName =
                                    typeName,

                                FileName =
                                    directory.Name
                            };


                        assetList.Add(info);


                        Button assetButton =
                            new Button(this);


                        assetButton.Text =
                            assetNumber +
                            ". " +
                            typeName +
                            "\n" +
                            "TypeID: " +
                            asset.TypeId;


                        assetButton.TextSize =
                            14;


                        AssetInfoData selected =
                            info;


                        assetButton.Click +=
                            delegate
                            {
                                ShowAssetDetail(
                                    selected
                                );
                            };


                        assetLayout.AddView(
                            assetButton
                        );
                    }
                }
                catch (Exception ex)
                {
                    AddText(
                        assetLayout,
                        "❌ ERROR: " +
                        ex.Message
                    );
                }
            }


            // =================================================
            // REPLACE CONTENT
            // =================================================

            SetContentView(
                assetLayout
            );
        }


        // =====================================================
        // CREATE ASSET LAYOUT
        // =====================================================

        private LinearLayout CreateAssetLayout()
        {
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


            ScrollView scroll =
                new ScrollView(this);


            LinearLayout content =
                new LinearLayout(this);

            content.Orientation =
                Orientation.Vertical;


            scroll.AddView(content);


            layout.AddView(
                scroll,
                new LinearLayout.LayoutParams(
                    -1,
                    -1
                )
            );


            return content;
        }


        // =====================================================
        // HEADER
        // =====================================================

        private void AddHeader(
            LinearLayout layout,
            string fileName,
            long fileSize,
            int directoryCount)
        {
            AddText(
                layout,
                "UABEA Android"
            );


            AddText(
                layout,
                ""
            );


            AddText(
                layout,
                "ASSETBUNDLE BERHASIL DIBUKA!"
            );


            AddText(
                layout,
                ""
            );


            AddText(
                layout,
                "Nama: " +
                fileName
            );


            AddText(
                layout,
                "Ukuran: " +
                fileSize.ToString("N0") +
                " bytes"
            );


            AddText(
                layout,
                "File dalam Bundle: " +
                directoryCount
            );


            AddText(
                layout,
                ""
            );
        }


        // =====================================================
        // ADD TEXT
        // =====================================================

        private void AddText(
            LinearLayout layout,
            string text)
        {
            TextView view =
                new TextView(this);


            view.Text =
                text;


            view.TextSize =
                16;


            view.SetPadding(
                0,
                8,
                0,
                8
            );


            layout.AddView(
                view
            );
        }


        // =====================================================
        // ASSET DETAIL
        // =====================================================

        private void ShowAssetDetail(
            AssetInfoData asset)
        {
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


            TextView title =
                new TextView(this);

            title.Text =
                "ASSET DETAIL";

            title.TextSize =
                24;


            layout.AddView(
                title
            );


            AddText(
                layout,
                ""
            );


            AddText(
                layout,
                "Asset #" +
                asset.Number
            );


            AddText(
                layout,
                "Type: " +
                asset.TypeName
            );


            AddText(
                layout,
                "TypeID: " +
                asset.TypeId
            );


            AddText(
                layout,
                "PathID: " +
                asset.PathId
            );


            AddText(
                layout,
                "Serialized File: " +
                asset.FileName
            );


            AddText(
                layout,
                ""
            );


            // =================================================
            // BACK BUTTON
            // =================================================

            Button backButton =
                new Button(this);

            backButton.Text =
                "BACK TO ASSET LIST";


            backButton.Click +=
                delegate
                {
                    // Kembali ke file yang sedang dibuka.
                    // Untuk tahap ini Activity di-recreate
                    // supaya picker tidak perlu dibuka lagi.
                    RebuildCurrentBundleView();
                };


            layout.AddView(
                backButton
            );


            SetContentView(
                layout
            );
        }


        // =====================================================
        // CURRENT BUNDLE VIEW
        // =====================================================

        private void RebuildCurrentBundleView()
        {
            if (assetList.Count == 0)
            {
                SetStatus(
                    "Tidak ada asset."
                );

                return;
            }


            LinearLayout layout =
                CreateAssetLayout();


            AddText(
                layout,
                "UABEA Android"
            );


            AddText(
                layout,
                ""
            );


            AddText(
                layout,
                "=== ASSET LIST ==="
            );


            foreach (
                AssetInfoData asset
                in assetList)
            {
                AssetInfoData selected =
                    asset;


                Button button =
                    new Button(this);


                button.Text =
                    asset.Number +
                    ". " +
                    asset.TypeName +
                    "\nTypeID: " +
                    asset.TypeId;


                button.Click +=
                    delegate
                    {
                        ShowAssetDetail(
                            selected
                        );
                    };


                layout.AddView(
                    button
                );
            }


            SetContentView(
                layout
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
