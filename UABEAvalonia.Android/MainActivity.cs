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

using AndroidUri = global::Android.Net.Uri;
using AndroidViewStates = global::Android.Views.ViewStates;

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

        private bool showingSerializedFiles;
        private bool showingAssetList;
        private bool showingInspector;


        // =========================================================
        // CREATE
        // =========================================================

        protected override void OnCreate(
            Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            BuildMainInterface();

            try
            {
                assetsManager =
                    new AssetsManager();

                SetStatus(
                    "AssetsTools.NET siap.\n" +
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
        // MAIN INTERFACE
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
                14;

            status.Text =
                "Menyiapkan UABEA Android...";


            // =====================================================
            // STATUS DIBERI TINGGI TETAP
            // AGAR TIDAK MEMAKAN SELURUH LAYAR
            // =====================================================

            LinearLayout.LayoutParams statusParams =
                new LinearLayout.LayoutParams(
                    -1,
                    75
                );


            root.AddView(
                title
            );

            root.AddView(
                openButton
            );

            root.AddView(
                status,
                statusParams
            );


            // =====================================================
            // ASSET LIST
            // =====================================================

            assetList =
                new LinearLayout(this);

            assetList.Orientation =
                Orientation.Vertical;


            ScrollView scroll =
                new ScrollView(this);

            scroll.FillViewport =
                true;

            scroll.AddView(
                assetList
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


        // =========================================================
        // ACTIVITY RESULT
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
        // GET FILE NAME
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
        // READ UNITY BUNDLE
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
                GetFileName(uri);


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


            currentAssetsFile =
                null;


            showingSerializedFiles =
                true;

            showingAssetList =
                false;

            showingInspector =
                false;


            ShowSerializedFiles(
                info.Length,
                fileName
            );
        }


        // =========================================================
        // SERIALIZED FILE LIST
        // =========================================================

        private void ShowSerializedFiles(
            long fileSize,
            string fileName)
        {
            if (currentBundle == null)
            {
                return;
            }


            ClearAssetList();


            TextView header =
                new TextView(this);


            header.Text =
                "ASSETBUNDLE BERHASIL DIBUKA!\n\n" +
                "Nama: " +
                fileName +
                "\n" +
                "Ukuran: " +
                fileSize.ToString("N0") +
                " bytes\n\n" +
                "Pilih SerializedFile:";


            header.TextSize =
                16;


            if (assetList != null)
            {
                assetList.AddView(
                    header
                );
            }


            var directories =
                currentBundle
                    .file
                    .BlockAndDirInfo
                    .DirectoryInfos;


            int index =
                0;


            foreach (
                var directory
                in directories)
            {
                int currentIndex =
                    index;


                index++;


                string currentName =
                    directory.Name;


                Button button =
                    new Button(this);


                button.Text =
                    index +
                    ". " +
                    currentName;


                button.SetMinHeight(
                    80
                );


                button.Click += delegate
                {
                    LoadSerializedFile(
                        currentIndex
                    );
                };


                if (assetList != null)
                {
                    assetList.AddView(
                        button
                    );
                }
            }


            SetStatus(
                "Bundle berhasil dibuka.\n" +
                "Pilih SerializedFile."
            );
        }


        // =========================================================
        // LOAD SERIALIZED FILE
        // =========================================================

        private void LoadSerializedFile(
            int index)
        {
            if (
                assetsManager == null ||
                currentBundle == null)
            {
                return;
            }


            try
            {
                SetStatus(
                    "Memuat SerializedFile..."
                );


                AssetsFileInstance assetsFile =
                    assetsManager.LoadAssetsFileFromBundle(
                        currentBundle,
                        index,
                        false
                    );


                if (assetsFile == null)
                {
                    throw new Exception(
                        "SerializedFile tidak dapat dimuat."
                    );
                }


                currentAssetsFile =
                    assetsFile;


                showingSerializedFiles =
                    false;

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
                    "Gagal memuat SerializedFile:\n\n" +
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
            ClearAssetList();


            if (assetList == null)
            {
                return;
            }


            // -----------------------------------------------------
            // BACK TO SERIALIZED FILE
            // -----------------------------------------------------

            Button back =
                new Button(this);


            back.Text =
                "← KEMBALI KE SERIALIZED FILE";


            back.Click += delegate
            {
                ReturnToSerializedFiles();
            };


            assetList.AddView(
                back
            );


            // -----------------------------------------------------
            // HEADER
            // -----------------------------------------------------

            TextView header =
                new TextView(this);


            header.Text =
                "=== ASSET LIST ===\n\n" +
                "Unity Version: " +
                assetsFile.file.Metadata.UnityVersion +
                "\n\n" +
                "Pilih asset:";


            header.TextSize =
                16;


            assetList.AddView(
                header
            );


            // -----------------------------------------------------
            // ASSETS
            // -----------------------------------------------------

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


                Button button =
                    new Button(this);


                button.Text =
                    "ASSET #" +
                    number +
                    " | " +
                    typeName +
                    "\n" +
                    "TypeID: " +
                    currentAsset.TypeId +
                    "\n" +
                    "PathID: " +
                    currentAsset.PathId;


                button.SetMinHeight(
                    110
                );


                button.Click += delegate
                {
                    OpenAssetInspector(
                        assetsFile,
                        currentAsset
                    );
                };


                assetList.AddView(
                    button
                );
            }


            SetStatus(
                "Asset List: " +
                number +
                " asset.\n" +
                "Tekan salah satu asset."
            );
        }


        // =========================================================
        // GET ASSET TYPE NAME
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
                    !string.IsNullOrEmpty(name) &&
                    name != typeId.ToString())
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
        // OPEN ASSET INSPECTOR
        // =========================================================

        private void OpenAssetInspector(
            AssetsFileInstance assetsFile,
            AssetFileInfo asset)
        {
            if (assetsManager == null)
            {
                return;
            }


            try
            {
                SetStatus(
                    "Membaca asset..."
                );


                AssetTypeValueField baseField =
                    assetsManager.GetBaseField(
                        assetsFile,
                        asset
                    );


                if (baseField == null)
                {
                    throw new Exception(
                        "GetBaseField mengembalikan NULL."
                    );
                }


                ClearAssetList();


                if (assetList == null)
                {
                    return;
                }


                // -------------------------------------------------
                // BACK
                // -------------------------------------------------

                Button back =
                    new Button(this);


                back.Text =
                    "← KEMBALI KE ASSET LIST";


                back.Click += delegate
                {
                    ReturnToAssetList();
                };


                assetList.AddView(
                    back
                );


                // -------------------------------------------------
                // HEADER
                // -------------------------------------------------

                string typeName =
                    GetAssetTypeName(
                        asset.TypeId
                    );


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
                    "Tekan ▶ untuk membuka field.";


                header.TextSize =
                    16;


                assetList.AddView(
                    header
                );


                // -------------------------------------------------
                // ROOT
                // -------------------------------------------------

                int rootChildren =
                    GetChildCount(
                        baseField
                    );


                TextView debug =
                    new TextView(this);


                debug.Text =
                    "\nRoot: " +
                    SafeFieldName(
                        baseField
                    ) +
                    "\nChild Count: " +
                    rootChildren +
                    "\n";


                debug.TextSize =
                    14;


                assetList.AddView(
                    debug
                );


                AddFieldTree(
                    baseField,
                    0
                );


                showingAssetList =
                    true;

                showingInspector =
                    true;


                SetStatus(
                    "Inspector dibuka: " +
                    typeName +
                    "\n" +
                    "Child: " +
                    rootChildren
                );
            }
            catch (Exception ex)
            {
                ClearAssetList();


                if (assetList != null)
                {
                    Button back =
                        new Button(this);


                    back.Text =
                        "← KEMBALI KE ASSET LIST";


                    back.Click += delegate
                    {
                        ReturnToAssetList();
                    };


                    assetList.AddView(
                        back
                    );
                }


                SetStatus(
                    "Inspector gagal:\n\n" +
                    ex.Message
                );
            }
        }


        // =========================================================
        // FIELD TREE
        // =========================================================

        private void AddFieldTree(
            AssetTypeValueField field,
            int depth)
        {
            if (
                field == null ||
                assetList == null)
            {
                return;
            }


            int childCount =
                GetChildCount(
                    field
                );


            string fieldName =
                SafeFieldName(
                    field
                );


            string value =
                GetDisplayValue(
                    field
                );


            LinearLayout container =
                new LinearLayout(this);


            container.Orientation =
                Orientation.Vertical;


            TextView text =
                new TextView(this);


            text.TextSize =
                15;


            string indent =
                MakeIndent(
                    depth
                );


            if (childCount > 0)
            {
                text.Text =
                    indent +
                    "▶ " +
                    fieldName +
                    " [" +
                    childCount +
                    "]";
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


            container.AddView(
                text
            );


            if (childCount > 0)
            {
                LinearLayout children =
                    new LinearLayout(this);


                children.Orientation =
                    Orientation.Vertical;


                children.Visibility =
                    AndroidViewStates.Gone;


                for (
                    int i = 0;
                    i < childCount;
                    i++)
                {
                    AssetTypeValueField? child =
                        GetChild(
                            field,
                            i
                        );


                    if (child != null)
                    {
                        AddFieldToLayout(
                            children,
                            child,
                            depth + 1
                        );
                    }
                }


                container.AddView(
                    children
                );


                text.Click += delegate
                {
                    if (
                        children.Visibility ==
                        AndroidViewStates.Gone)
                    {
                        children.Visibility =
                            AndroidViewStates.Visible;


                        text.Text =
                            indent +
                            "▼ " +
                            fieldName +
                            " [" +
                            childCount +
                            "]";
                    }
                    else
                    {
                        children.Visibility =
                            AndroidViewStates.Gone;


                        text.Text =
                            indent +
                            "▶ " +
                            fieldName +
                            " [" +
                            childCount +
                            "]";
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


            assetList.AddView(
                container
            );
        }


        // =========================================================
        // CHILD FIELD
        // =========================================================

        private void AddFieldToLayout(
            LinearLayout parent,
            AssetTypeValueField field,
            int depth)
        {
            int childCount =
                GetChildCount(
                    field
                );


            string fieldName =
                SafeFieldName(
                    field
                );


            string value =
                GetDisplayValue(
                    field
                );


            TextView text =
                new TextView(this);


            text.TextSize =
                15;


            string indent =
                MakeIndent(
                    depth
                );


            if (childCount > 0)
            {
                text.Text =
                    indent +
                    "▶ " +
                    fieldName +
                    " [" +
                    childCount +
                    "]";
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


            if (childCount <= 0)
            {
                text.Click += delegate
                {
                    ShowFieldEditor(
                        field
                    );
                };


                return;
            }


            LinearLayout children =
                new LinearLayout(this);


            children.Orientation =
                Orientation.Vertical;


            children.Visibility =
                AndroidViewStates.Gone;


            for (
                int i = 0;
                i < childCount;
                i++)
            {
                AssetTypeValueField? child =
                    GetChild(
                        field,
                        i
                    );


                if (child != null)
                {
                    AddFieldToLayout(
                        children,
                        child,
                        depth + 1
                    );
                }
            }


            parent.AddView(
                children
            );


            text.Click += delegate
            {
                if (
                    children.Visibility ==
                    AndroidViewStates.Gone)
                {
                    children.Visibility =
                        AndroidViewStates.Visible;


                    text.Text =
                        indent +
                        "▼ " +
                        fieldName +
                        " [" +
                        childCount +
                        "]";
                }
                else
                {
                    children.Visibility =
                        AndroidViewStates.Gone;


                    text.Text =
                        indent +
                        "▶ " +
                        fieldName +
                        " [" +
                        childCount +
                        "]";
                }
            };
        }


        // =========================================================
        // CHILD COUNT
        // =========================================================

        private int GetChildCount(
            AssetTypeValueField field)
        {
            try
            {
                if (field.Children == null)
                {
                    return 0;
                }


                return field.Children.Count;
            }
            catch
            {
                return 0;
            }
        }


        // =========================================================
        // GET CHILD
        // =========================================================

        private AssetTypeValueField? GetChild(
            AssetTypeValueField field,
            int index)
        {
            try
            {
                if (
                    field.Children == null ||
                    index < 0 ||
                    index >= field.Children.Count)
                {
                    return null;
                }


                return field.Children[index];
            }
            catch
            {
                return null;
            }
        }


        // =========================================================
        // FIELD NAME
        // =========================================================

        private string SafeFieldName(
            AssetTypeValueField field)
        {
            try
            {
                if (
                    !string.IsNullOrEmpty(
                        field.FieldName))
                {
                    return field.FieldName;
                }
            }
            catch
            {
            }


            return "(unnamed)";
        }


        // =========================================================
        // VALUE
        // =========================================================

        private string GetDisplayValue(
            AssetTypeValueField field)
        {
            try
            {
                if (field.Value == null)
                {
                    return "";
                }
            }
            catch
            {
                return "";
            }


            // -----------------------------------------------------
            // String
            // -----------------------------------------------------

            try
            {
                string value =
                    field.Value.AsString;


                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch
            {
            }


            // -----------------------------------------------------
            // Bool
            // -----------------------------------------------------

            try
            {
                return field.Value.AsBool
                    .ToString();
            }
            catch
            {
            }


            // -----------------------------------------------------
            // Int
            // -----------------------------------------------------

            try
            {
                return field.Value.AsInt
                    .ToString(
                        CultureInfo.InvariantCulture
                    );
            }
            catch
            {
            }


            // -----------------------------------------------------
            // UInt
            // -----------------------------------------------------

            try
            {
                return field.Value.AsUInt
                    .ToString(
                        CultureInfo.InvariantCulture
                    );
            }
            catch
            {
            }


            // -----------------------------------------------------
            // Long
            // -----------------------------------------------------

            try
            {
                return field.Value.AsLong
                    .ToString(
                        CultureInfo.InvariantCulture
                    );
            }
            catch
            {
            }


            // -----------------------------------------------------
            // ULong
            // -----------------------------------------------------

            try
            {
                return field.Value.AsULong
                    .ToString(
                        CultureInfo.InvariantCulture
                    );
            }
            catch
            {
            }


            // -----------------------------------------------------
            // Float
            // -----------------------------------------------------

            try
            {
                return field.Value.AsFloat
                    .ToString(
                        CultureInfo.InvariantCulture
                    );
            }
            catch
            {
            }


            // -----------------------------------------------------
            // Double
            // -----------------------------------------------------

            try
            {
                return field.Value.AsDouble
                    .ToString(
                        CultureInfo.InvariantCulture
                    );
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
            string fieldName =
                SafeFieldName(
                    field
                );


            string currentValue =
                GetDisplayValue(
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
                20,
                40,
                20
            );


            TextView info =
                new TextView(this);


            info.Text =
                "Field:\n" +
                fieldName +
                "\n\n" +
                "Nilai saat ini:\n" +
                currentValue +
                "\n\n" +
                "Nilai baru:";


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
                        ApplyFieldValue(
                            field,
                            input.Text ?? ""
                        );


                        SetStatus(
                            "Field berhasil diubah:\n" +
                            fieldName
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
        // APPLY FIELD VALUE
        // =========================================================

        private void ApplyFieldValue(
            AssetTypeValueField field,
            string value)
        {
            // =====================================================
            // Karena tipe field bisa berbeda-beda, kita coba
            // beberapa tipe yang umum digunakan Unity.
            // =====================================================

            try
            {
                field.Value.AsString =
                    value;

                return;
            }
            catch
            {
            }


            if (
                bool.TryParse(
                    value,
                    out bool boolValue))
            {
                try
                {
                    field.Value.AsBool =
                        boolValue;

                    return;
                }
                catch
                {
                }
            }


            if (
                int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int intValue))
            {
                try
                {
                    field.Value.AsInt =
                        intValue;

                    return;
                }
                catch
                {
                }
            }


            if (
                uint.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out uint uintValue))
            {
                try
                {
                    field.Value.AsUInt =
                        uintValue;

                    return;
                }
                catch
                {
                }
            }


            if (
                long.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out long longValue))
            {
                try
                {
                    field.Value.AsLong =
                        longValue;

                    return;
                }
                catch
                {
                }
            }


            if (
                ulong.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out ulong ulongValue))
            {
                try
                {
                    field.Value.AsULong =
                        ulongValue;

                    return;
                }
                catch
                {
                }
            }


            if (
                float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float floatValue))
            {
                try
                {
                    field.Value.AsFloat =
                        floatValue;

                    return;
                }
                catch
                {
                }
            }


            if (
                double.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double doubleValue))
            {
                try
                {
                    field.Value.AsDouble =
                        doubleValue;

                    return;
                }
                catch
                {
                }
            }


            throw new Exception(
                "Tipe field tidak dapat diubah " +
                "oleh editor ini."
            );
        }


        // =========================================================
        // INDENT
        // =========================================================

        private string MakeIndent(
            int depth)
        {
            if (depth <= 0)
            {
                return "";
            }


            return new string(
                ' ',
                depth * 4
            );
        }


        // =========================================================
        // BACK TO ASSET LIST
        // =========================================================

        private void ReturnToAssetList()
        {
            if (currentAssetsFile == null)
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
        // BACK TO SERIALIZED FILE
        // =========================================================

        private void ReturnToSerializedFiles()
        {
            if (currentBundle == null)
            {
                return;
            }


            showingInspector =
                false;

            showingAssetList =
                false;

            showingSerializedFiles =
                true;


            ClearAssetList();


            ShowSerializedFiles(
                0,
                "AssetBundle"
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


            if (showingAssetList)
            {
                ReturnToSerializedFiles();
                return;
            }


            if (showingSerializedFiles)
            {
                showingSerializedFiles =
                    false;


                ClearAssetList();


                SetStatus(
                    "Tekan OPEN UNITY3D."
                );


                return;
            }


            base.OnBackPressed();
        }


        // =========================================================
        // CLEAR LIST
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
    }
}
