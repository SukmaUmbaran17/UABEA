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

using ViewStates =
    global::Android.Views.ViewStates;

using AndroidUri =
    global::Android.Net.Uri;

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

        private BundleFileInstance? currentBundle;
        private AssetsFileInstance? currentAssetsFile;

        private int currentSerializedIndex = -1;

        private bool showingAssetList = false;
        private bool showingInspector = false;


        protected override void OnCreate(
            Bundle? savedInstanceState)
        {
            base.OnCreate(
                savedInstanceState
            );


            BuildMainInterface();


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


        private void BuildMainInterface()
        {
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


            currentBundle =
                bundle;


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


                        string typeName =
                            GetAssetTypeName(
                                asset.TypeId
                            );


                        result.AppendLine(
                            assetNumber +
                            ". " +
                            typeName +
                            " | TypeID: " +
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


            showingAssetList =
                false;

            showingInspector =
                false;


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


                currentBundle =
                    bundle;

                currentAssetsFile =
                    assetsFile;

                currentSerializedIndex =
                    index;

                showingAssetList =
                    true;

                showingInspector =
                    false;


                ShowAssetList(
                    assetsFile
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


        private void ShowAssetList(
            AssetsFileInstance assetsFile)
        {
            StringBuilder result =
                new StringBuilder();


            result.AppendLine(
                "=== ASSET LIST ==="
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
                "Pilih asset:"
            );


            result.AppendLine();


            ClearAssetList();


            int number =
                0;


            foreach (
                var asset
                in assetsFile.file.AssetInfos)
            {
                number++;


                AssetFileInfo currentAsset =
                    asset;


                string typeName =
                    GetAssetTypeName(
                        currentAsset.TypeId
                    );


                result.AppendLine(
                    "ASSET #" +
                    number +
                    " | " +
                    typeName
                );


                result.AppendLine(
                    "TypeID: " +
                    currentAsset.TypeId
                );


                result.AppendLine(
                    "PathID: " +
                    currentAsset.PathId
                );


                result.AppendLine();


                Button assetButton =
                    new Button(this);


                assetButton.Text =
                    "ASSET #" +
                    number +
                    " | " +
                    typeName +
                    "\nTypeID: " +
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


        private string GetAssetTypeName(
            int typeId)
        {
            try
            {
                AssetClassID classId =
                    (AssetClassID)typeId;


                string name =
                    classId.ToString();


                if (
                    !string.IsNullOrWhiteSpace(
                        name) &&
                    name !=
                    typeId.ToString())
                {
                    return name;
                }
            }
            catch
            {
            }


            return "Unknown";
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
                ClearAssetList();


                string typeName =
                    GetAssetTypeName(
                        asset.TypeId
                    );


                AddBackButton();


                AssetTypeValueField baseField =
                    assetsManager.GetBaseField(
                        assetsFile,
                        asset
                    );


                if (baseField == null)
                {
                    SetStatus(
                        "BaseField tidak tersedia."
                    );

                    return;
                }


                TextView header =
                    new TextView(this);


                header.Text =
                    "=== ASSET INSPECTOR ===\n\n" +
                    "Type: " +
                    typeName +
                    "\n" +
                    "TypeID: " +
                    asset.TypeId +
                    "\n" +
                    "PathID: " +
                    asset.PathId;


                header.TextSize =
                    16;


                if (assetList != null)
                {
                    assetList.AddView(
                        header
                    );
                }


                AddTreeNode(
                    baseField,
                    0
                );


                showingAssetList =
                    true;

                showingInspector =
                    true;


                SetStatus(
                    "Asset berhasil dibuka.\n" +
                    "Type: " +
                    typeName +
                    "\n\n" +
                    "Tekan ▶ untuk membuka node."
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


        private void AddBackButton()
        {
            Button backButton =
                new Button(this);


            backButton.Text =
                "← KEMBALI KE ASSET LIST";


            backButton.Click += delegate
            {
                ReturnToAssetList();
            };


            if (assetList != null)
            {
                assetList.AddView(
                    backButton
                );
            }
        }


        private void ReturnToAssetList()
        {
            if (
                currentAssetsFile == null)
            {
                return;
            }


            showingInspector =
                false;


            showingAssetList =
                true;


            ShowAssetList(
                currentAssetsFile
            );
        }


        public override void OnBackPressed()
        {
            if (showingInspector)
            {
                ReturnToAssetList();

                return;
            }


            if (
                showingAssetList &&
                currentBundle != null)
            {
                showingAssetList =
                    false;

                showingInspector =
                    false;

                ShowSerializedFiles();

                return;
            }


            base.OnBackPressed();
        }


        private void ShowSerializedFiles()
        {
            if (currentBundle == null)
            {
                return;
            }


            ClearAssetList();


            StringBuilder result =
                new StringBuilder();


            result.AppendLine(
                "=== SERIALIZED FILE ==="
            );


            result.AppendLine();


            int number =
                0;


            foreach (
                var directory
                in currentBundle
                    .file
                    .BlockAndDirInfo
                    .DirectoryInfos)
            {
                int currentIndex =
                    number;


                number++;


                string currentName =
                    directory.Name;


                Button button =
                    new Button(this);


                button.Text =
                    currentName;


                button.Click += delegate
                {
                    LoadSerializedFile(
                        currentBundle,
                        currentIndex
                    );
                };


                if (assetList != null)
                {
                    assetList.AddView(
                        button
                    );
                }


                result.AppendLine(
                    (number) +
                    ". " +
                    currentName
                );
            }


            SetStatus(
                result.ToString()
            );
        }


        private void AddTreeNode(
            AssetTypeValueField field,
            int depth)
        {
            if (
                field == null ||
                assetList == null)
            {
                return;
            }


            bool hasChildren =
                field.Children != null &&
                field.Children.Count > 0;


            string fieldName =
                field.FieldName ??
                "(unnamed)";


            string value =
                GetFieldValue(
                    field
                );


            LinearLayout row =
                new LinearLayout(this);


            row.Orientation =
                Orientation.Vertical;


            TextView text =
                new TextView(this);


            text.TextSize =
                15;


            string indent =
                new string(
                    ' ',
                    depth * 4
                );


            if (hasChildren)
            {
                text.Text =
                    indent +
                    "▶ " +
                    fieldName;
            }
            else
            {
                if (!string.IsNullOrEmpty(value))
                {
                    text.Text =
                        indent +
                        "• " +
                        fieldName +
                        " = " +
                        value;
                }
                else
                {
                    text.Text =
                        indent +
                        "• " +
                        fieldName;
                }
            }


            row.AddView(
                text
            );


            LinearLayout childrenLayout =
                new LinearLayout(this);


            childrenLayout.Orientation =
                Orientation.Vertical;


            childrenLayout.Visibility =
                ViewStates.Gone;


            if (hasChildren)
            {
                foreach (
                    AssetTypeValueField child
                    in field.Children)
                {
                    AddTreeNodeToLayout(
                        childrenLayout,
                        child,
                        depth + 1
                    );
                }


                text.Click += delegate
                {
                    if (
                        childrenLayout.Visibility ==
                        ViewStates.Gone)
                    {
                        childrenLayout.Visibility =
                            ViewStates.Visible;


                        text.Text =
                            indent +
                            "▼ " +
                            fieldName;
                    }
                    else
                    {
                        childrenLayout.Visibility =
                            ViewStates.Gone;


                        text.Text =
                            indent +
                            "▶ " +
                            fieldName;
                    }
                };
            }


            row.AddView(
                childrenLayout
            );


            assetList.AddView(
                row
            );
        }


        private void AddTreeNodeToLayout(
            LinearLayout parent,
            AssetTypeValueField field,
            int depth)
        {
            if (field == null)
            {
                return;
            }


            bool hasChildren =
                field.Children != null &&
                field.Children.Count > 0;


            string fieldName =
                field.FieldName ??
                "(unnamed)";


            string value =
                GetFieldValue(
                    field
                );


            TextView text =
                new TextView(this);


            text.TextSize =
                15;


            string indent =
                new string(
                    ' ',
                    depth * 4
                );


            if (hasChildren)
            {
                text.Text =
                    indent +
                    "▶ " +
                    fieldName;
            }
            else
            {
                if (!string.IsNullOrEmpty(value))
                {
                    text.Text =
                        indent +
                        "• " +
                        fieldName +
                        " = " +
                        value;
                }
                else
                {
                    text.Text =
                        indent +
                        "• " +
                        fieldName;
                }
            }


            parent.AddView(
                text
            );


            if (!hasChildren)
            {
                return;
            }


            LinearLayout childrenLayout =
                new LinearLayout(this);


            childrenLayout.Orientation =
                Orientation.Vertical;


            childrenLayout.Visibility =
                ViewStates.Gone;


            foreach (
                AssetTypeValueField child
                in field.Children)
            {
                AddTreeNodeToLayout(
                    childrenLayout,
                    child,
                    depth + 1
                );
            }


            parent.AddView(
                childrenLayout
            );


            text.Click += delegate
            {
                if (
                    childrenLayout.Visibility ==
                    ViewStates.Gone)
                {
                    childrenLayout.Visibility =
                        ViewStates.Visible;


                    text.Text =
                        indent +
                        "▼ " +
                        fieldName;
                }
                else
                {
                    childrenLayout.Visibility =
                        ViewStates.Gone;


                    text.Text =
                        indent +
                        "▶ " +
                        fieldName;
                }
            };
        }


        private string GetFieldValue(
            AssetTypeValueField field)
        {
            if (field == null)
            {
                return "";
            }


            try
            {
                if (field.Value != null)
                {
                    return field.Value.AsString;
                }
            }
            catch
            {
            }


            return "";
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
