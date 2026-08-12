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
using System.Collections.Generic;

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

        private bool showingSerializedFiles = false;
        private bool showingAssetList = false;
        private bool showingInspector = false;


        // =========================================================
        // ON CREATE
        // =========================================================

        protected override void OnCreate(
            Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            BuildMainInterface();

            try
            {
                assetsManager = new AssetsManager();

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

            scroll.AddView(assetList);


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

                intent.SetType("*/*");

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


            if (resultCode != Result.Ok)
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
                ReadUnityBundle(data.Data);
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
                            cursor.GetString(index);


                        if (
                            !string.IsNullOrWhiteSpace(
                                detected))
                        {
                            fileName = detected;
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


            var directories =
                currentBundle
                    .file
                    .BlockAndDirInfo
                    .DirectoryInfos;


            int count =
                0;


            foreach (var d in directories)
            {
                count++;
            }


            result.AppendLine(
                "File dalam Bundle: " +
                count
            );

            result.AppendLine();

            result.AppendLine(
                "=== SERIALIZED FILE ==="
            );

            result.AppendLine();


            int index =
                0;


            foreach (var directory in directories)
            {
                int currentIndex =
                    index;


                index++;


                string name =
                    directory.Name;


                Button button =
                    new Button(this);

                button.Text =
                    name;


                button.Click += delegate
                {
                    LoadSerializedFile(
                        currentIndex
                    );
                };


                if (assetList != null)
                {
                    assetList.AddView(button);
                }


                result.AppendLine(
                    index +
                    ". " +
                    name
                );
            }


            SetStatus(
                result.ToString()
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


            StringBuilder result =
                new StringBuilder();


            result.AppendLine(
                "=== ASSET LIST ==="
            );

            result.AppendLine();

            result.AppendLine(
                "Unity Version: " +
                assetsFile.file.Metadata.UnityVersion
            );

            result.AppendLine();


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
                    "\nTypeID: " +
                    currentAsset.TypeId +
                    " | PathID: " +
                    currentAsset.PathId;


                button.Click += delegate
                {
                    OpenAssetInspector(
                        assetsFile,
                        currentAsset
                    );
                };


                if (assetList != null)
                {
                    assetList.AddView(button);
                }


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
            }


            SetStatus(
                result.ToString()
            );
        }


        // =========================================================
        // ASSET TYPE NAME
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
        // OPEN INSPECTOR
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
                    "Membaca TypeTree..."
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


                AddBackToAssetListButton();


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
                    "Tekan field untuk melihat/mengedit nilai.";


                header.TextSize =
                    16;


                if (assetList != null)
                {
                    assetList.AddView(header);
                }


                int childCount =
                    GetChildCount(baseField);


                TextView debug =
                    new TextView(this);


                debug.Text =
                    "\nRoot: " +
                    SafeFieldName(baseField) +
                    "\nChild count: " +
                    childCount +
                    "\n";


                debug.TextSize =
                    14;


                if (assetList != null)
                {
                    assetList.AddView(debug);
                }


                // ROOT
                AddFieldTree(
                    baseField,
                    0,
                    true
                );


                showingAssetList =
                    true;

                showingInspector =
                    true;


                SetStatus(
                    "Inspector: " +
                    typeName +
                    "\n\n" +
                    "Root child count: " +
                    childCount
                );
            }
            catch (Exception ex)
            {
                ClearAssetList();


                AddBackToAssetListButton();


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
            int depth,
            bool root)
        {
            if (
                field == null ||
                assetList == null)
            {
                return;
            }


            int childCount =
                GetChildCount(field);


            string fieldName =
                SafeFieldName(field);


            string value =
                GetDisplayValue(field);


            LinearLayout container =
                new LinearLayout(this);


            container.Orientation =
                Orientation.Vertical;


            TextView text =
                new TextView(this);


            text.TextSize =
                15;


            string indent =
                MakeIndent(depth);


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


            container.AddView(text);


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
                    AssetTypeValueField child =
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
            if (field == null)
            {
                return;
            }


            int childCount =
                GetChildCount(field);


            string fieldName =
                SafeFieldName(field);


            string value =
                GetDisplayValue(field);


            TextView text =
                new TextView(this);


            text.TextSize =
                15;


            string indent =
                MakeIndent(depth);


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


            parent.AddView(text);


            if (childCount <= 0)
            {
                text.Click += delegate
                {
                    ShowFieldEditor(field);
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
                AssetTypeValueField child =
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


            parent.AddView(children);


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
        // CHILD HELPERS
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
        // FIELD VALUE
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


                string type =
                    GetValueTypeName(
                        field
                    );


                switch (type)
                {
                    case "string":
                    case "String":
                        return field.Value.AsString ?? "";


                    case "bool":
                    case "Bool":
                        return field.Value.AsBool.ToString();


                    case "SInt8":
                    case "byte":
                        return field.Value.AsSByte.ToString();


                    case "UInt8":
                        return field.Value.AsByte.ToString();


                    case "SInt16":
                    case "short":
                        return field.Value.AsShort.ToString();


                    case "UInt16":
                        return field.Value.AsUShort.ToString();


                    case "SInt32":
                    case "int":
                        return field.Value.AsInt.ToString();


                    case "UInt32":
                    case "unsigned int":
                        return field.Value.AsUInt.ToString();


                    case "SInt64":
                    case "long":
                        return field.Value.AsLong.ToString();


                    case "UInt64":
                    case "unsigned long":
                        return field.Value.AsULong.ToString();


                    case "float":
                    case "Single":
                        return field.Value.AsFloat
                            .ToString(
                                CultureInfo.InvariantCulture
                            );


                    case "double":
                    case "Double":
                        return field.Value.AsDouble
                            .ToString(
                                CultureInfo.InvariantCulture
                            );
                }
            }
            catch
            {
            }


            // Fallback.
            // Jangan biarkan field hilang hanya karena
            // tipe nilainya belum kita kenali.
            try
            {
                string raw =
                    field.Value?.AsString ?? "";


                if (!string.IsNullOrEmpty(raw))
                {
                    return raw;
                }
            }
            catch
            {
            }


            return "";
        }


        // =========================================================
        // VALUE TYPE
        // =========================================================

        private string GetValueTypeName(
            AssetTypeValueField field)
        {
            try
            {
                var template =
                    field.TemplateField;


                if (template != null)
                {
                    Type t =
                        template.GetType();


                    var p =
                        t.GetProperty("Type");


                    if (p != null)
                    {
                        object? value =
                            p.GetValue(template);


                        if (value != null)
                        {
                            return value.ToString()
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
                SafeFieldName(field);


            string currentValue =
                GetDisplayValue(field);


            AlertDialog.Builder builder =
                new AlertDialog.Builder(this);


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
                "Field:\n" +
                fieldName +
                "\n\n" +
                "Nilai saat ini:\n" +
                currentValue +
                "\n";


            info.TextSize =
                16;


            layout.AddView(info);


            EditText input =
                new EditText(this);


            input.Text =
                currentValue;


            input.SetSingleLine(true);


            layout.AddView(input);


            builder.SetView(layout);


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


                        ApplyFieldValue(
                            field,
                            newValue
                        );


                        SetStatus(
                            "Field berhasil diubah:\n\n" +
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
        // APPLY FIELD
        // =========================================================

        private void ApplyFieldValue(
            AssetTypeValueField field,
            string value)
        {
            string type =
                GetValueTypeName(field);


            switch (type)
            {
                case "string":
                case "String":
                    field.Value.AsString =
                        value;
                    return;


                case "bool":
                case "Bool":
                    if (
                        !bool.TryParse(
                            value,
                            out bool boolValue))
                    {
                        throw new Exception(
                            "Gunakan true atau false."
                        );
                    }


                    field.Value.AsBool =
                        boolValue;

                    return;


                case "SInt32":
                case "int":
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

                    return;


                case "UInt32":
                case "unsigned int":
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

                    return;


                case "SInt64":
                case "long":
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

                    return;


                case "UInt64":
                case "unsigned long":
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

                    return;


                case "SInt16":
                case "short":
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

                    return;


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

                    return;


                case "UInt8":
                case "byte":
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

                    return;


                case "SInt8":
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

                    return;


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

                    return;


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

                    return;


                default:
                    throw new Exception(
                        "Tipe field belum didukung: " +
                        type
                    );
            }
        }


        // =========================================================
        // BACK TO ASSET LIST
        // =========================================================

        private void AddBackToAssetListButton()
        {
            Button back =
                new Button(this);


            back.Text =
                "← KEMBALI KE ASSET LIST";


            back.Click += delegate
            {
                ReturnToAssetList();
            };


            if (assetList != null)
            {
                assetList.AddView(back);
            }
        }


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

                showingSerializedFiles =
                    true;


                ShowSerializedFiles(
                    0,
                    "AssetBundle"
                );

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
        // CLEAR
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
