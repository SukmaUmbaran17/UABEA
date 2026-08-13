using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;
using Android.Graphics;

using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;

using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

using AndroidUri = global::Android.Net.Uri;
using AndroidViewStates = global::Android.Views.ViewStates;
using IOPath = System.IO.Path;

namespace UABEAvalonia.Android
{
    [Activity(
        Label = "UABEA Android",
        MainLauncher = true
    )]
    public class MainActivity : Activity
    {
        private const int PickFileRequestCode = 1001;
        private const int ExportTextureRequestCode = 2001;

        private TextView? status;
        private LinearLayout? assetList;

        private AssetsManager? assetsManager;

        private BundleFileInstance? currentBundle;
        private AssetsFileInstance? currentAssetsFile;
        private AssetFileInfo? currentAsset;

        private bool showingSerializedFiles;
        private bool showingAssetList;
        private bool showingInspector;

        private readonly HashSet<long> modifiedAssets =
            new HashSet<long>();

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

        // ============================================================
        // MAIN UI
        // ============================================================

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
                30);

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

            LinearLayout.LayoutParams
                statusParams =
                new LinearLayout.LayoutParams(
                    -1,
                    100);

            root.AddView(title);
            root.AddView(openButton);
            root.AddView(
                status,
                statusParams);

            assetList =
                new LinearLayout(this);

            assetList.Orientation =
                Orientation.Vertical;

            ScrollView scroll =
                new ScrollView(this);

            scroll.FillViewport =
                true;

            scroll.AddView(assetList);

            LinearLayout.LayoutParams
                scrollParams =
                new LinearLayout.LayoutParams(
                    -1,
                    0);

            scrollParams.Weight =
                1;

            root.AddView(
                scroll,
                scrollParams);

            SetContentView(root);

            openButton.Click +=
                delegate
                {
                    OpenFilePicker();
                };
        }

        // ============================================================
        // FILE PICKER
        // ============================================================

        private void OpenFilePicker()
        {
            try
            {
                Intent intent =
                    new Intent(
                        Intent.ActionOpenDocument);

                intent.AddCategory(
                    Intent.CategoryOpenable);

                intent.SetType("*/*");

                StartActivityForResult(
                    intent,
                    PickFileRequestCode);
            }
            catch (Exception ex)
            {
                SetStatus(
                    "File Picker gagal.\n\n" +
                    ex.Message);
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
                data);

            if (resultCode != Result.Ok ||
                data?.Data == null)
            {
                if (requestCode ==
                    PickFileRequestCode)
                {
                    SetStatus(
                        "Pemilihan file dibatalkan.");
                }

                return;
            }

            try
            {
                if (requestCode ==
                    PickFileRequestCode)
                {
                    ReadUnityBundle(
                        data.Data);
                }
                else if (requestCode ==
                    ExportTextureRequestCode)
                {
                    ExportCurrentTexture(
                        data.Data);
                }
            }
            catch (Exception ex)
            {
                SetStatus(
                    "Operasi gagal.\n\n" +
                    ex.Message);
            }
        }

        // ============================================================
        // FILE NAME
        // ============================================================

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
                            OpenableColumns.DisplayName);

                    if (index >= 0 &&
                        cursor.MoveToFirst())
                    {
                        string? detected =
                            cursor.GetString(index);

                        if (!string.IsNullOrWhiteSpace(
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
                in IOPath.GetInvalidFileNameChars())
            {
                fileName =
                    fileName.Replace(
                        invalidChar,
                        '_');
            }

            return fileName;
        }

        // ============================================================
        // COPY TO CACHE
        // ============================================================

        private string CopyToCache(
            AndroidUri uri,
            string fileName)
        {
            if (CacheDir == null)
            {
                throw new Exception(
                    "CacheDir tidak tersedia.");
            }

            string path =
                IOPath.Combine(
                    CacheDir.AbsolutePath,
                    fileName);

            using (
                Stream? input =
                    ContentResolver.OpenInputStream(uri))
            {
                if (input == null)
                {
                    throw new Exception(
                        "Tidak dapat membaca file.");
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

        // ============================================================
        // OPEN UNITY BUNDLE
        // ============================================================

        private void ReadUnityBundle(
            AndroidUri uri)
        {
            if (assetsManager == null)
            {
                throw new Exception(
                    "AssetsManager tidak tersedia.");
            }

            SetStatus(
                "Menyalin file...");

            string fileName =
                GetFileName(uri);

            string cachePath =
                CopyToCache(
                    uri,
                    fileName);

            FileInfo info =
                new FileInfo(cachePath);

            SetStatus(
                "Membuka AssetBundle...");

            BundleFileInstance bundle =
                assetsManager.LoadBundleFile(
                    cachePath);

            if (bundle == null)
            {
                throw new Exception(
                    "AssetBundle gagal dibuka.");
            }

            currentBundle =
                bundle;

            currentAssetsFile =
                null;

            currentAsset =
                null;

            modifiedAssets.Clear();

            showingSerializedFiles =
                true;

            showingAssetList =
                false;

            showingInspector =
                false;

            ShowSerializedFiles(
                info.Length,
                fileName);
        }

        // ============================================================
        // SERIALIZED FILES
        // ============================================================

        private void ShowSerializedFiles(
            long fileSize,
            string fileName)
        {
            if (currentBundle == null ||
                assetList == null)
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
                "\nUkuran: " +
                fileSize.ToString("N0") +
                " bytes\n\n" +
                "=== SERIALIZED FILE ===\n\n" +
                "Pilih file:";

            header.TextSize =
                16;

            assetList.AddView(
                header);

            var directories =
                currentBundle
                    .file
                    .BlockAndDirInfo
                    .DirectoryInfos;

            int index = 0;

            foreach (
                var directory
                in directories)
            {
                int currentIndex =
                    index;

                index++;

                Button button =
                    new Button(this);

                button.Text =
                    index +
                    ". " +
                    directory.Name;

                button.SetMinHeight(
                    80);

                button.Click +=
                    delegate
                    {
                        LoadSerializedFile(
                            currentIndex);
                    };

                assetList.AddView(
                    button);
            }

            SetStatus(
                "Bundle berhasil dibuka.\n" +
                "Pilih SerializedFile."
            );
        }

        private void LoadSerializedFile(
            int index)
        {
            if (assetsManager == null ||
                currentBundle == null)
            {
                return;
            }

            try
            {
                SetStatus(
                    "Memuat SerializedFile...");

                AssetsFileInstance assetsFile =
                    assetsManager
                        .LoadAssetsFileFromBundle(
                            currentBundle,
                            index,
                            false);

                if (assetsFile == null)
                {
                    throw new Exception(
                        "SerializedFile tidak dapat dimuat.");
                }

                currentAssetsFile =
                    assetsFile;

                currentAsset =
                    null;

                showingSerializedFiles =
                    false;

                showingAssetList =
                    true;

                showingInspector =
                    false;

                ShowAssetList(
                    assetsFile);
            }
            catch (Exception ex)
            {
                SetStatus(
                    "Gagal memuat SerializedFile.\n\n" +
                    ex.Message);
            }
        }

        // ============================================================
        // ASSET LIST
        // ============================================================

        private void ShowAssetList(
            AssetsFileInstance assetsFile)
        {
            ClearAssetList();

            if (assetList == null)
                return;

            Button back =
                new Button(this);

            back.Text =
                "← KEMBALI KE SERIALIZED FILE";

            back.Click +=
                delegate
                {
                    ReturnToSerializedFiles();
                };

            assetList.AddView(back);

            TextView header =
                new TextView(this);

            header.Text =
                "=== ASSET LIST ===\n\n" +
                "Unity Version: " +
                assetsFile.file.Metadata.UnityVersion +
                "\n\nPilih asset:";

            header.TextSize =
                16;

            assetList.AddView(header);

            int number = 0;

            foreach (
                var asset
                in assetsFile.file.AssetInfos)
            {
                number++;

                AssetFileInfo current =
                    asset;

                string typeName =
                    GetAssetTypeName(
                        current.TypeId);

                string mark =
                    modifiedAssets.Contains(
                        current.PathId)
                    ? " ✓ MODIFIED"
                    : "";

                Button button =
                    new Button(this);

                button.Text =
                    "ASSET #" +
                    number +
                    " | " +
                    typeName +
                    mark +
                    "\nTypeID: " +
                    current.TypeId +
                    "\nPathID: " +
                    current.PathId;

                button.SetMinHeight(
                    110);

                button.Click +=
                    delegate
                    {
                        OpenAssetInspector(
                            assetsFile,
                            current);
                    };

                assetList.AddView(
                    button);
            }

            SetStatus(
                "Asset List: " +
                number +
                " asset.\n" +
                "Pilih asset untuk membuka Inspector."
            );
        }

        // ============================================================
        // ASSET TYPE
        // ============================================================

        private string GetAssetTypeName(
            int typeId)
        {
            try
            {
                AssetClassID classId =
                    (AssetClassID)typeId;

                string name =
                    classId.ToString();

                if (!string.IsNullOrEmpty(name) &&
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

        // ============================================================
        // INSPECTOR
        // ============================================================

        private void OpenAssetInspector(
            AssetsFileInstance assetsFile,
            AssetFileInfo asset)
        {
            if (assetsManager == null)
                return;

            try
            {
                SetStatus(
                    "Membaca asset...");

                AssetTypeValueField baseField =
                    assetsManager.GetBaseField(
                        assetsFile,
                        asset);

                if (baseField == null)
                {
                    throw new Exception(
                        "GetBaseField mengembalikan NULL.");
                }

                currentAssetsFile =
                    assetsFile;

                currentAsset =
                    asset;

                ClearAssetList();

                if (assetList == null)
                    return;

                Button back =
                    new Button(this);

                back.Text =
                    "← KEMBALI KE ASSET LIST";

                back.Click +=
                    delegate
                    {
                        ReturnToAssetList();
                    };

                assetList.AddView(back);

                string typeName =
                    GetAssetTypeName(
                        asset.TypeId);

                string mark =
                    modifiedAssets.Contains(
                        asset.PathId)
                    ? "\n✓ MODIFIED"
                    : "";

                TextView header =
                    new TextView(this);

                header.Text =
                    "=== ASSET INSPECTOR ===\n\n" +
                    "Type: " +
                    typeName +
                    mark +
                    "\n\nTypeID: " +
                    asset.TypeId +
                    "\n\nPathID: " +
                    asset.PathId +
                    "\n\nSemua field dibuka otomatis.";

                header.TextSize =
                    16;

                assetList.AddView(
                    header);

                // Texture2D = TypeID 28
                if (asset.TypeId == 28)
                {
                    AddTextureButtons(
                        assetsFile,
                        asset);
                }

                int rootChildren =
                    GetChildCount(
                        baseField);

                TextView rootInfo =
                    new TextView(this);

                rootInfo.Text =
                    "\nRoot: " +
                    SafeFieldName(baseField) +
                    "\nChild Count: " +
                    rootChildren +
                    "\n";

                rootInfo.TextSize =
                    14;

                assetList.AddView(
                    rootInfo);

                AddFieldTree(
                    baseField,
                    0);

                showingAssetList =
                    true;

                showingInspector =
                    true;

                SetStatus(
                    "Inspector: " +
                    typeName +
                    "\nSemua field dibuka."
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

                    back.Click +=
                        delegate
                        {
                            ReturnToAssetList();
                        };

                    assetList.AddView(back);
                }

                SetStatus(
                    "Inspector gagal.\n\n" +
                    ex.Message);
            }
        }

        // ============================================================
        // TEXTURE BUTTONS
        // ============================================================

        private void AddTextureButtons(
            AssetsFileInstance assetsFile,
            AssetFileInfo asset)
        {
            if (assetList == null)
                return;

            TextView title =
                new TextView(this);

            title.Text =
                "\n=== TEXTURE2D TOOLS ===\n" +
                "Texture2D terdeteksi.";

            title.TextSize =
                16;

            assetList.AddView(title);

            Button view =
                new Button(this);

            view.Text =
                "🖼 VIEW TEXTURE";

            view.Click +=
                delegate
                {
                    ViewTexture(
                        assetsFile,
                        asset);
                };

            assetList.AddView(view);

            Button export =
                new Button(this);

            export.Text =
                "📤 EXPORT PNG";

            export.Click +=
                delegate
                {
                    StartTextureExport();
                };

            assetList.AddView(export);
        }

        // ============================================================
        // TEXTURE VIEWER
        // ============================================================

        // ============================================================
// TEXTURE VIEWER
// ============================================================

private void ViewTexture(
    AssetsFileInstance assetsFile,
    AssetFileInfo asset)
{
    if (assetsManager == null)
        return;

    try
    {
        ShowTextureMessage(
            "TEXTURE VIEWER",
            "Membaca Texture2D...\n\n" +
            "TypeID: " + asset.TypeId +
            "\nPathID: " + asset.PathId);

        // --------------------------------------------------------
        // GET BASE FIELD
        // --------------------------------------------------------

        AssetTypeValueField baseField =
            assetsManager.GetBaseField(
                assetsFile,
                asset);

        if (baseField == null)
        {
            throw new Exception(
                "GetBaseField() mengembalikan NULL.");
        }

        ShowTextureMessage(
            "TEXTURE VIEWER",
            "GetBaseField berhasil.\n\n" +
            "Membaca TextureFile...");

        // --------------------------------------------------------
        // READ TEXTURE FILE
        // --------------------------------------------------------

        TextureFile tex =
            TextureFile.ReadTextureFile(
                baseField);

        if (tex == null)
        {
            throw new Exception(
                "TextureFile.ReadTextureFile() " +
                "mengembalikan NULL.");
        }

        if (tex.m_Width <= 0 ||
            tex.m_Height <= 0)
        {
            throw new Exception(
                "Ukuran texture tidak valid.\n\n" +
                "Width: " +
                tex.m_Width +
                "\nHeight: " +
                tex.m_Height);
        }

        TextureFormat format =
            (TextureFormat)
            tex.m_TextureFormat;

        // --------------------------------------------------------
        // TEXTURE INFORMATION
        // --------------------------------------------------------

        ShowTextureMessage(
            "TEXTURE INFO",
            "TextureFile berhasil dibaca!\n\n" +
            "Width: " +
            tex.m_Width +
            "\nHeight: " +
            tex.m_Height +
            "\nFormat: " +
            format +
            "\nTextureFormat ID: " +
            tex.m_TextureFormat +
            "\nMipCount: " +
            tex.m_MipCount +
            "\n\nMengambil texture data...");

        // --------------------------------------------------------
        // GET TEXTURE DATA
        // --------------------------------------------------------

        byte[] encodedData =
            tex.GetTextureData(
                assetsFile);

        if (encodedData == null ||
            encodedData.Length == 0)
        {
            throw new Exception(
                "GetTextureData() menghasilkan " +
                "data kosong.\n\n" +
                "Kemungkinan texture menggunakan " +
                "resource eksternal (.resS), " +
                "atau format texture belum didukung.");
        }

        // --------------------------------------------------------
        // SHOW DATA INFORMATION
        // --------------------------------------------------------

        ShowTextureMessage(
            "TEXTURE DATA",
            "Data texture berhasil diambil!\n\n" +
            "Width: " +
            tex.m_Width +
            "\nHeight: " +
            tex.m_Height +
            "\nFormat: " +
            format +
            "\nMipCount: " +
            tex.m_MipCount +
            "\nEncoded bytes: " +
            encodedData.Length +
            "\n\nMenjalankan decoder...");

        // --------------------------------------------------------
        // DECODE
        // --------------------------------------------------------

        byte[] bgra =
            TextureFile.DecodeManaged(
                encodedData,
                format,
                tex.m_Width,
                tex.m_Height,
                true);

        if (bgra == null ||
            bgra.Length == 0)
        {
            throw new Exception(
                "DecodeManaged() menghasilkan " +
                "data kosong.");
        }

        int expected =
            tex.m_Width *
            tex.m_Height *
            4;

        if (bgra.Length < expected)
        {
            throw new Exception(
                "Ukuran hasil decoder tidak sesuai.\n\n" +
                "Expected BGRA bytes: " +
                expected +
                "\nActual: " +
                bgra.Length);
        }

        // --------------------------------------------------------
        // CREATE ANDROID BITMAP
        // --------------------------------------------------------

        Bitmap bitmap =
            CreateBitmapFromBgra(
                bgra,
                tex.m_Width,
                tex.m_Height);

        // --------------------------------------------------------
        // SHOW PREVIEW
        // --------------------------------------------------------

        ShowTexturePreview(
            bitmap,
            tex.m_Width,
            tex.m_Height,
            format);
    }
    catch (Exception ex)
    {
        ShowTextureError(
            asset,
            ex);
    }
}


// ============================================================
// TEXTURE DEBUG MESSAGE
// ============================================================

private void ShowTextureMessage(
    string title,
    string message)
{
    ClearAssetList();

    if (assetList == null)
        return;

    TextView header =
        new TextView(this);

    header.Text =
        "=== " +
        title +
        " ===";

    header.TextSize =
        20;

    header.SetPadding(
        10,
        20,
        10,
        20);

    assetList.AddView(
        header);

    TextView text =
        new TextView(this);

    text.Text =
        message;

    text.TextSize =
        16;

    text.SetPadding(
        10,
        20,
        10,
        20);

    assetList.AddView(
        text);

    Button back =
        new Button(this);

    back.Text =
        "← KEMBALI KE INSPECTOR";

    back.Click +=
        delegate
        {
            if (currentAssetsFile != null &&
                currentAsset != null)
            {
                OpenAssetInspector(
                    currentAssetsFile,
                    currentAsset);
            }
        };

    assetList.AddView(
        back);
}


// ============================================================
// TEXTURE ERROR SCREEN
// ============================================================

private void ShowTextureError(
    AssetFileInfo asset,
    Exception ex)
{
    ClearAssetList();

    if (assetList == null)
        return;

    TextView title =
        new TextView(this);

    title.Text =
        "❌ TEXTURE VIEWER GAGAL";

    title.TextSize =
        20;

    title.SetPadding(
        10,
        20,
        10,
        20);

    assetList.AddView(
        title);

    TextView error =
        new TextView(this);

    string errorText =
        "Asset Texture2D\n\n" +
        "TypeID: " +
        asset.TypeId +
        "\nPathID: " +
        asset.PathId +
        "\n\n" +
        "ERROR:\n" +
        ex.Message +
        "\n\n" +
        "DETAIL:\n" +
        ex.ToString();

    error.Text =
        errorText;

    error.TextSize =
        15;

    error.SetPadding(
        10,
        20,
        10,
        20);

    assetList.AddView(
        error);

    Button back =
        new Button(this);

    back.Text =
        "← KEMBALI KE INSPECTOR";

    back.Click +=
        delegate
        {
            if (currentAssetsFile != null &&
                currentAsset != null)
            {
                OpenAssetInspector(
                    currentAssetsFile,
                    currentAsset);
            }
        };

    assetList.AddView(
        back);

    SetStatus(
        "Texture Viewer gagal. " +
        "Detail error ditampilkan di layar.");
}

        // ============================================================
        // BGRA -> ANDROID BITMAP
        // ============================================================

        private Bitmap CreateBitmapFromBgra(
            byte[] bgra,
            int width,
            int height)
        {
            if (bgra.Length <
                width * height * 4)
            {
                throw new Exception(
                    "Ukuran data BGRA tidak sesuai ukuran texture.");
            }

            int[] pixels =
                new int[
                    width *
                    height];

            int source =
                0;

            for (int i = 0;
                 i < pixels.Length;
                 i++)
            {
                byte b =
                    bgra[source++];

                byte g =
                    bgra[source++];

                byte r =
                    bgra[source++];

                byte a =
                    bgra[source++];

                pixels[i] =
                    (a << 24) |
                    (r << 16) |
                    (g << 8) |
                    b;
            }

            Bitmap bitmap =
                Bitmap.CreateBitmap(
                    width,
                    height,
                    Bitmap.Config.Argb8888!);

            bitmap.SetPixels(
                pixels,
                0,
                width,
                0,
                0,
                width,
                height);

            return bitmap;
        }

        // ============================================================
        // TEXTURE PREVIEW
        // ============================================================

        private void ShowTexturePreview(
            Bitmap bitmap,
            int width,
            int height,
            TextureFormat format)
        {
            ClearAssetList();

            if (assetList == null)
                return;

            Button back =
                new Button(this);

            back.Text =
                "← KEMBALI KE INSPECTOR";

            back.Click +=
                delegate
                {
                    if (currentAssetsFile != null &&
                        currentAsset != null)
                    {
                        OpenAssetInspector(
                            currentAssetsFile,
                            currentAsset);
                    }
                };

            assetList.AddView(back);

            TextView info =
                new TextView(this);

            info.Text =
                "=== TEXTURE VIEWER ===\n\n" +
                "Size: " +
                width +
                " x " +
                height +
                "\nTexture Format: " +
                format +
                "\nPreview: BGRA → Android Bitmap";

            info.TextSize =
                16;

            assetList.AddView(info);

            ImageView image =
                new ImageView(this);

            image.SetImageBitmap(
                bitmap);

            image.SetAdjustViewBounds(
                true);

            image.SetScaleType(
                ImageView.ScaleType.FitCenter);

            LinearLayout.LayoutParams
                imageParams =
                new LinearLayout.LayoutParams(
                    -1,
                    -2);

            imageParams.SetMargins(
                0,
                20,
                0,
                20);

            assetList.AddView(
                image,
                imageParams);

            Button export =
                new Button(this);

            export.Text =
                "📤 EXPORT PNG";

            export.Click +=
                delegate
                {
                    StartTextureExport();
                };

            assetList.AddView(export);

            SetStatus(
                "Texture Viewer: " +
                width +
                " x " +
                height +
                "\nFormat: " +
                format);
        }

        // ============================================================
        // EXPORT PNG
        // ============================================================

        private void StartTextureExport()
        {
            if (currentAsset == null ||
                currentAssetsFile == null)
            {
                return;
            }

            try
            {
                Intent intent =
                    new Intent(
                        Intent.ActionCreateDocument);

                intent.AddCategory(
                    Intent.CategoryOpenable);

                intent.SetType(
                    "image/png");

                intent.PutExtra(
                    Intent.ExtraTitle,
                    "texture_" +
                    currentAsset.PathId +
                    ".png");

                StartActivityForResult(
                    intent,
                    ExportTextureRequestCode);
            }
            catch (Exception ex)
            {
                SetStatus(
                    "Tidak dapat membuka Save Picker.\n\n" +
                    ex.Message);
            }
        }

        private void ExportCurrentTexture(
            AndroidUri outputUri)
        {
            if (assetsManager == null ||
                currentAssetsFile == null ||
                currentAsset == null)
            {
                throw new Exception(
                    "Texture yang aktif tidak ditemukan.");
            }

            AssetTypeValueField baseField =
                assetsManager.GetBaseField(
                    currentAssetsFile,
                    currentAsset);

            if (baseField == null)
            {
                throw new Exception(
                    "Texture field tidak dapat dibaca.");
            }

            TextureFile tex =
                TextureFile.ReadTextureFile(
                    baseField);

            if (tex.m_Width <= 0 ||
                tex.m_Height <= 0)
            {
                throw new Exception(
                    "Texture berukuran 0x0.");
            }

            byte[] encodedData =
                tex.GetTextureData(
                    currentAssetsFile);

            if (encodedData == null ||
                encodedData.Length == 0)
            {
                throw new Exception(
                    "Data texture kosong.\n\n" +
                    "Kemungkinan texture menggunakan .resS.");
            }

            TextureFormat format =
                (TextureFormat)
                tex.m_TextureFormat;

            byte[] bgra =
                TextureFile.DecodeManaged(
                    encodedData,
                    format,
                    tex.m_Width,
                    tex.m_Height,
                    true);

            if (bgra == null ||
                bgra.Length == 0)
            {
                throw new Exception(
                    "Texture gagal didecode.");
            }

            Bitmap bitmap =
                CreateBitmapFromBgra(
                    bgra,
                    tex.m_Width,
                    tex.m_Height);

            using Stream? output =
                ContentResolver.OpenOutputStream(
                    outputUri);

            if (output == null)
            {
                throw new Exception(
                    "Tidak dapat membuka file output.");
            }

            bool success =
                bitmap.Compress(
                    Bitmap.CompressFormat.Png,
                    100,
                    output);

            if (!success)
            {
                throw new Exception(
                    "Bitmap gagal dikonversi menjadi PNG.");
            }

            output.Flush();

            bitmap.Recycle();

            SetStatus(
                "✓ Texture berhasil diexport ke PNG.");
        }

        // ============================================================
        // FIELD TREE
        // ============================================================

        private void AddFieldTree(
            AssetTypeValueField field,
            int depth)
        {
            if (field == null ||
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
                    "▼ " +
                    fieldName +
                    " [" +
                    childCount +
                    "]";

                container.AddView(text);

                LinearLayout children =
                    new LinearLayout(this);

                children.Orientation =
                    Orientation.Vertical;

                children.Visibility =
                    AndroidViewStates.Visible;

                for (int i = 0;
                     i < childCount;
                     i++)
                {
                    AssetTypeValueField? child =
                        GetChild(
                            field,
                            i);

                    if (child != null)
                    {
                        AddFieldToLayoutExpanded(
                            children,
                            child,
                            depth + 1);
                    }
                }

                container.AddView(
                    children);

                text.Click +=
                    delegate
                    {
                        if (children.Visibility ==
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
                text.Text =
                    string.IsNullOrEmpty(value)
                    ? indent +
                      "• " +
                      fieldName
                    : indent +
                      "• " +
                      fieldName +
                      " = " +
                      value;

                container.AddView(
                    text);

                text.Click +=
                    delegate
                    {
                        ShowFieldEditor(
                            field);
                    };
            }

            assetList.AddView(
                container);
        }

        private void AddFieldToLayoutExpanded(
            LinearLayout parent,
            AssetTypeValueField field,
            int depth)
        {
            if (field == null)
                return;

            int childCount =
                GetChildCount(field);

            string fieldName =
                SafeFieldName(field);

            string value =
                GetDisplayValue(field);

            string indent =
                MakeIndent(depth);

            TextView text =
                new TextView(this);

            text.TextSize =
                15;

            if (childCount > 0)
            {
                text.Text =
                    indent +
                    "▼ " +
                    fieldName +
                    " [" +
                    childCount +
                    "]";

                parent.AddView(text);

                LinearLayout children =
                    new LinearLayout(this);

                children.Orientation =
                    Orientation.Vertical;

                children.Visibility =
                    AndroidViewStates.Visible;

                for (int i = 0;
                     i < childCount;
                     i++)
                {
                    AssetTypeValueField? child =
                        GetChild(
                            field,
                            i);

                    if (child != null)
                    {
                        AddFieldToLayoutExpanded(
                            children,
                            child,
                            depth + 1);
                    }
                }

                parent.AddView(
                    children);

                text.Click +=
                    delegate
                    {
                        if (children.Visibility ==
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
                text.Text =
                    string.IsNullOrEmpty(value)
                    ? indent +
                      "• " +
                      fieldName
                    : indent +
                      "• " +
                      fieldName +
                      " = " +
                      value;

                parent.AddView(text);

                text.Click +=
                    delegate
                    {
                        ShowFieldEditor(
                            field);
                    };
            }
        }

        // ============================================================
        // FIELD HELPERS
        // ============================================================

        private int GetChildCount(
            AssetTypeValueField field)
        {
            try
            {
                if (field.Children == null)
                    return 0;

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
                if (field.Children == null ||
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

        private string SafeFieldName(
            AssetTypeValueField field)
        {
            try
            {
                if (!string.IsNullOrEmpty(
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

        private string GetDisplayValue(
            AssetTypeValueField field)
        {
            try
            {
                if (field.Value == null)
                    return "";
            }
            catch
            {
                return "";
            }

            try
            {
                string value =
                    field.Value.AsString;

                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            catch
            {
            }

            try
            {
                return field.Value.AsBool.ToString();
            }
            catch
            {
            }

            try
            {
                return field.Value.AsInt.ToString(
                    CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            try
            {
                return field.Value.AsUInt.ToString(
                    CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            try
            {
                return field.Value.AsLong.ToString(
                    CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            try
            {
                return field.Value.AsULong.ToString(
                    CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            try
            {
                return field.Value.AsFloat.ToString(
                    CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            try
            {
                return field.Value.AsDouble.ToString(
                    CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            return "";
        }

        // ============================================================
        // FIELD EDITOR
        // ============================================================

        private void ShowFieldEditor(
            AssetTypeValueField field)
        {
            string fieldName =
                SafeFieldName(field);

            string currentValue =
                GetDisplayValue(field);

            AlertDialog.Builder builder =
                new AlertDialog.Builder(this);

            builder.SetTitle(
                "EDIT FIELD");

            LinearLayout layout =
                new LinearLayout(this);

            layout.Orientation =
                Orientation.Vertical;

            layout.SetPadding(
                40,
                20,
                40,
                20);

            TextView info =
                new TextView(this);

            info.Text =
                "Field:\n" +
                fieldName +
                "\n\nNilai sekarang:\n" +
                currentValue +
                "\n\nNilai baru:";

            info.TextSize =
                16;

            layout.AddView(info);

            EditText input =
                new EditText(this);

            input.Text =
                currentValue;

            input.SetSingleLine(
                true);

            layout.AddView(input);

            builder.SetView(
                layout);

            builder.SetNegativeButton(
                "CANCEL",
                (sender, args) =>
                {
                });

            builder.SetPositiveButton(
                "APPLY",
                (sender, args) =>
                {
                    try
                    {
                        ApplyFieldValue(
                            field,
                            input.Text ?? "");

                        if (currentAsset != null)
                        {
                            modifiedAssets.Add(
                                currentAsset.PathId);
                        }

                        SetStatus(
                            "✓ Field berhasil diubah:\n" +
                            fieldName +
                            "\nAsset ditandai MODIFIED.");

                        if (currentAssetsFile != null &&
                            currentAsset != null)
                        {
                            OpenAssetInspector(
                                currentAssetsFile,
                                currentAsset);
                        }
                    }
                    catch (Exception ex)
                    {
                        SetStatus(
                            "Gagal mengubah field:\n\n" +
                            ex.Message);
                    }
                });

            builder.Show();
        }

        private void ApplyFieldValue(
            AssetTypeValueField field,
            string value)
        {
            try
            {
                field.Value.AsString =
                    value;

                return;
            }
            catch
            {
            }

            if (bool.TryParse(
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

            if (int.TryParse(
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

            if (uint.TryParse(
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

            if (long.TryParse(
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

            if (ulong.TryParse(
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

            if (float.TryParse(
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

            if (double.TryParse(
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
                "Tipe field tidak dapat diubah.");
        }

        // ============================================================
        // NAVIGATION
        // ============================================================

        private string MakeIndent(
            int depth)
        {
            if (depth <= 0)
                return "";

            return new string(
                ' ',
                depth * 4);
        }

        private void ReturnToAssetList()
        {
            if (currentAssetsFile == null)
                return;

            showingInspector =
                false;

            showingAssetList =
                true;

            ShowAssetList(
                currentAssetsFile);
        }

        private void ReturnToSerializedFiles()
        {
            if (currentBundle == null)
                return;

            showingInspector =
                false;

            showingAssetList =
                false;

            showingSerializedFiles =
                true;

            ClearAssetList();

            // Nama file tidak disimpan lagi di state.
            // UI tetap dapat kembali ke daftar.
            ShowSerializedFiles(
                0,
                "AssetBundle");
        }

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
                    "Tekan OPEN UNITY3D.");

                return;
            }

            base.OnBackPressed();
        }

        // ============================================================
        // UI HELPERS
        // ============================================================

        private void ClearAssetList()
        {
            if (assetList == null)
                return;

            assetList.RemoveAllViews();
        }

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
                });
        }
    }
}
