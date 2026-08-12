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
using System.Globalization;

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


        // =========================================================
        // ON CREATE
        // =========================================================

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


        // =========================================================
        // MAIN UI
        // =========================================================

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


        // =========================================================
        // FILE NAME
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


        // =========================================================
        // COPY TO CACHE
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


        // =========================================================
        // OPEN UNITY BUNDLE
        // =========================================================

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


        // =========================================================
        // SERIALIZED FILE
        // =========================================================

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


        // =========================================================
        // ASSET LIST
        // =========================================================

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


        // =========================================================
        // TYPE NAME
        // =========================================================

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


        // =========================================================
        // ASSET INSPECTOR
        // =========================================================

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
                    asset.PathId +
                    "\n\n" +
                    "Tekan field yang memiliki nilai " +
                    "untuk mengedit.";


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
                    "Asset Inspector aktif.\n\n" +
                    "Field yang memiliki nilai " +
                    "dapat ditekan untuk diedit."
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


        // =========================================================
        // BACK BUTTON
        // =========================================================

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


        // =========================================================
        // ANDROID BACK
        // =========================================================

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


        // =========================================================
        // SERIALIZED FILE LIST
        // =========================================================

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
                    number +
                    ". " +
                    currentName
                );
            }


            SetStatus(
                result.ToString()
            );
        }


        // =========================================================
        // FIELD TREE
        // =========================================================

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
                if (
                    !string.IsNullOrEmpty(
                        value))
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
            else
            {
                text.Click += delegate
                {
                    ShowFieldEditor(
                        field
                    );
                };
            }


            row.AddView(
                childrenLayout
            );


            assetList.AddView(
                row
            );
        }


        // =========================================================
        // FIELD TREE CHILD
        // =========================================================

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
                if (
                    !string.IsNullOrEmpty(
                        value))
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
                text.Click += delegate
                {
                    ShowFieldEditor(
                        field
                    );
                };

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


        // =========================================================
        // READ FIELD VALUE
        // =========================================================

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
                    string value =
                        field.Value.AsString;

                    if (
                        !string.IsNullOrEmpty(
                            value))
                    {
                        return value;
                    }
                }
            }
            catch
            {
            }


            return "";
        }


        // =========================================================
        // FIELD EDITOR
        // =========================================================

        private void ShowFieldEditor(
            AssetTypeValueField field)
        {
            if (field == null)
            {
                return;
            }


            string fieldName =
                field.FieldName ??
                "(unnamed)";


            string currentValue =
                GetFieldValue(
                    field
                );


            AlertDialog.Builder builder =
                new AlertDialog.Builder(
                    this
                );


            builder.SetTitle(
                "EDIT FIELD"
            );


            LinearLayout layout =
                new LinearLayout(this);


            layout.Orientation =
                Orientation.Vertical;


            layout.SetPadding(
                40,
                10,
                40,
                10
            );


            TextView info =
                new TextView(this);


            info.Text =
                "Field: " +
                fieldName +
                "\n\n" +
                "Nilai saat ini:\n" +
                currentValue +
                "\n\n" +
                "Masukkan nilai baru:";


            info.TextSize =
                16;


            layout.AddView(
                info
            );


            EditText input =
                new EditText(this);


            input.Text =
                currentValue;


            input.SetSingleLine(
                true
            );


            layout.AddView(
                input
            );


            builder.SetView(
                layout
            );


            builder.SetNegativeButton(
                "CANCEL",
                (sender, args) =>
                {
                }
            );


            builder.SetPositiveButton(
                "APPLY",
                (sender, args) =>
                {
                    try
                    {
                        string newValue =
                            input.Text ?? "";


                        SetFieldValue(
                            field,
                            newValue
                        );


                        SetStatus(
                            "Field berhasil diubah.\n\n" +
                            fieldName +
                            " = " +
                            newValue
                        );
                    }
                    catch (Exception ex)
                    {
                        SetStatus(
                            "Gagal mengubah field:\n\n" +
                            ex.Message
                        );
                    }
                }
            );


            builder.Show();
        }


        // =========================================================
        // SET FIELD VALUE
        //
        // Kita gunakan nama tipe dari template field,
        // tetapi lewat refleksi agar kompatibel dengan
        // variasi API AssetsTools.NET yang digunakan project.
        // =========================================================

        private void SetFieldValue(
            AssetTypeValueField field,
            string value)
        {
            if (field == null)
            {
                throw new Exception(
                    "Field tidak tersedia."
                );
            }


            string typeName =
                GetFieldTypeName(
                    field
                );


            switch (typeName)
            {
                case "string":
                case "String":
                    field.Value.AsString =
                        value;
                    break;


                case "bool":
                case "Bool":
                    if (
                        !bool.TryParse(
                            value,
                            out bool boolValue))
                    {
                        throw new Exception(
                            "Bool harus true atau false."
                        );
                    }

                    field.Value.AsBool =
                        boolValue;

                    break;


                case "int":
                case "SInt32":
                    if (
                        !int.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out int intValue))
                    {
                        throw new Exception(
                            "Nilai harus integer."
                        );
                    }

                    field.Value.AsInt =
                        intValue;

                    break;


                case "unsigned int":
                case "UInt32":
                    if (
                        !uint.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out uint uintValue))
                    {
                        throw new Exception(
                            "Nilai harus unsigned integer."
                        );
                    }

                    field.Value.AsUInt =
                        uintValue;

                    break;


                case "long":
                case "SInt64":
                    if (
                        !long.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out long longValue))
                    {
                        throw new Exception(
                            "Nilai harus long."
                        );
                    }

                    field.Value.AsLong =
                        longValue;

                    break;


                case "unsigned long":
                case "UInt64":
                    if (
                        !ulong.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out ulong ulongValue))
                    {
                        throw new Exception(
                            "Nilai harus unsigned long."
                        );
                    }

                    field.Value.AsULong =
                        ulongValue;

                    break;


                case "float":
                case "Single":
                    if (
                        !float.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out float floatValue))
                    {
                        throw new Exception(
                            "Nilai harus float."
                        );
                    }

                    field.Value.AsFloat =
                        floatValue;

                    break;


                case "double":
                case "Double":
                    if (
                        !double.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double doubleValue))
                    {
                        throw new Exception(
                            "Nilai harus double."
                        );
                    }

                    field.Value.AsDouble =
                        doubleValue;

                    break;


                case "short":
                case "SInt16":
                    if (
                        !short.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out short shortValue))
                    {
                        throw new Exception(
                            "Nilai harus short."
                        );
                    }

                    field.Value.AsShort =
                        shortValue;

                    break;


                case "unsigned short":
                case "UInt16":
                    if (
                        !ushort.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out ushort ushortValue))
                    {
                        throw new Exception(
                            "Nilai harus ushort."
                        );
                    }

                    field.Value.AsUShort =
                        ushortValue;

                    break;


                case "byte":
                case "UInt8":
                    if (
                        !byte.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out byte byteValue))
                    {
                        throw new Exception(
                            "Nilai harus byte."
                        );
                    }

                    field.Value.AsByte =
                        byteValue;

                    break;


                case "SByte":
                    if (
                        !sbyte.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out sbyte sbyteValue))
                    {
                        throw new Exception(
                            "Nilai harus sbyte."
                        );
                    }

                    field.Value.AsSByte =
                        sbyteValue;

                    break;


                default:
                    throw new Exception(
                        "Tipe field belum didukung:\n" +
                        typeName
                    );
            }
        }


        // =========================================================
        // GET FIELD TYPE
        // =========================================================

        private string GetFieldTypeName(
            AssetTypeValueField field)
        {
            try
            {
                var template =
                    field.TemplateField;


                if (template != null)
                {
                    Type templateType =
                        template.GetType();


                    var property =
                        templateType.GetProperty(
                            "Type"
                        );


                    if (property != null)
                    {
                        object? result =
                            property.GetValue(
                                template
                            );


                        if (result != null)
                        {
                            return result.ToString()
                                ?? "";
                        }
                    }


                    var typeProperty =
                        templateType.GetProperty(
                            "TypeName"
                        );


                    if (typeProperty != null)
                    {
                        object? result =
                            typeProperty.GetValue(
                                template
                            );


                        if (result != null)
                        {
                            return result.ToString()
                                ?? "";
                        }
                    }
                }
            }
            catch
            {
            }


            return "";
        }


        // =========================================================
        // CLEAR UI
        // =========================================================

        private void ClearAssetList()
        {
            if (assetList == null)
            {
                return;
            }


            assetList.RemoveAllViews();
        }


        // =========================================================
        // STATUS
        // =========================================================

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
