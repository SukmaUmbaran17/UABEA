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
    [Activity(Label = "UABEA Android", MainLauncher = true)]
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

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            BuildMainInterface();

            try
            {
                assetsManager = new AssetsManager();

                SetStatus(
                    "AssetsTools.NET siap.\n\n" +
                    "Tekan OPEN UNITY3D.");
            }
            catch (Exception ex)
            {
                SetStatus(
                    "AssetsTools.NET gagal dimuat.\n\n" +
                    ex.Message);
            }
        }

        // ============================================================
        // MAIN UI
        // ============================================================

        private void BuildMainInterface()
        {
            var root =
                new LinearLayout(this)
                {
                    Orientation = Orientation.Vertical
                };

            root.SetPadding(30, 30, 30, 30);

            var title =
                new TextView(this)
                {
                    Text = "UABEA Android",
                    TextSize = 24
                };

            var open =
                new Button(this)
                {
                    Text = "OPEN UNITY3D"
                };

            status =
                new TextView(this)
                {
                    Text = "Menyiapkan UABEA Android...",
                    TextSize = 14
                };

            root.AddView(title);
            root.AddView(open);

            root.AddView(
                status,
                new LinearLayout.LayoutParams(
                    -1,
                    100));

            assetList =
                new LinearLayout(this)
                {
                    Orientation = Orientation.Vertical
                };

            var scroll =
                new ScrollView(this);

            scroll.FillViewport = true;
            scroll.AddView(assetList);

            var sp =
                new LinearLayout.LayoutParams(
                    -1,
                    0);

            sp.Weight = 1;

            root.AddView(scroll, sp);

            SetContentView(root);

            open.Click += delegate
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
                var intent =
                    new Intent(Intent.ActionOpenDocument);

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
                    ReadUnityBundle(data.Data);
                }
                else if (requestCode ==
                         ExportTextureRequestCode)
                {
                    ExportCurrentTexture(data.Data);
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
        // FILE HELPERS
        // ============================================================

        private string GetFileName(AndroidUri uri)
        {
            string name = "temp.unity3d";

            using (var c =
                   ContentResolver.Query(
                       uri,
                       null,
                       null,
                       null,
                       null))
            {
                if (c != null)
                {
                    int i =
                        c.GetColumnIndex(
                            OpenableColumns.DisplayName);

                    if (i >= 0 &&
                        c.MoveToFirst())
                    {
                        string? n =
                            c.GetString(i);

                        if (!string.IsNullOrWhiteSpace(n))
                            name = n;
                    }
                }
            }

            foreach (
                char ch in
                IOPath.GetInvalidFileNameChars())
            {
                name =
                    name.Replace(
                        ch,
                        '_');
            }

            return name;
        }

        private string CopyToCache(
            AndroidUri uri,
            string fileName)
        {
            if (CacheDir == null)
                throw new Exception(
                    "CacheDir tidak tersedia.");

            string path =
                IOPath.Combine(
                    CacheDir.AbsolutePath,
                    fileName);

            using (Stream? input =
                   ContentResolver.OpenInputStream(uri))
            {
                if (input == null)
                    throw new Exception(
                        "Tidak dapat membaca file.");

                using FileStream output =
                    File.Create(path);

                input.CopyTo(output);
            }

            return path;
        }

        // ============================================================
        // UNITY BUNDLE
        // ============================================================

        private void ReadUnityBundle(AndroidUri uri)
        {
            if (assetsManager == null)
                throw new Exception(
                    "AssetsManager tidak tersedia.");

            SetStatus(
                "Menyalin file...");

            string name =
                GetFileName(uri);

            string path =
                CopyToCache(
                    uri,
                    name);

            var info =
                new FileInfo(path);

            SetStatus(
                "Membuka AssetBundle...");

            BundleFileInstance bundle =
                assetsManager.LoadBundleFile(path);

            if (bundle == null)
                throw new Exception(
                    "AssetBundle gagal dibuka.");

            currentBundle = bundle;
            currentAssetsFile = null;
            currentAsset = null;

            modifiedAssets.Clear();

            showingSerializedFiles = true;
            showingAssetList = false;
            showingInspector = false;

            ShowSerializedFiles(
                info.Length,
                name);
        }

        private void ShowSerializedFiles(
            long fileSize,
            string fileName)
        {
            if (currentBundle == null ||
                assetList == null)
                return;

            ClearAssetList();

            var header =
                new TextView(this)
                {
                    TextSize = 16
                };

            header.Text =
                "ASSETBUNDLE BERHASIL DIBUKA!\n\n" +
                "Nama: " + fileName +
                "\nUkuran: " +
                fileSize.ToString("N0") +
                " bytes\n\n" +
                "=== SERIALIZED FILE ===\n\n" +
                "Pilih file:";

            assetList.AddView(header);

            int index = 0;

            foreach (
                var directory
                in currentBundle.file.BlockAndDirInfo.DirectoryInfos)
            {
                int selected = index++;

                var b =
                    new Button(this)
                    {
                        Text =
                            (index) +
                            ". " +
                            directory.Name
                    };

                b.SetMinHeight(80);

                b.Click += delegate
                {
                    LoadSerializedFile(selected);
                };

                assetList.AddView(b);
            }

            SetStatus(
                "Bundle berhasil dibuka.\n" +
                "Pilih SerializedFile.");
        }

        private void LoadSerializedFile(int index)
        {
            if (assetsManager == null ||
                currentBundle == null)
                return;

            try
            {
                SetStatus(
                    "Memuat SerializedFile...");

                AssetsFileInstance f =
                    assetsManager.LoadAssetsFileFromBundle(
                        currentBundle,
                        index,
                        false);

                if (f == null)
                    throw new Exception(
                        "SerializedFile tidak dapat dimuat.");

                currentAssetsFile = f;
                currentAsset = null;

                showingSerializedFiles = false;
                showingAssetList = true;
                showingInspector = false;

                ShowAssetList(f);
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
            AssetsFileInstance f)
        {
            ClearAssetList();

            if (assetList == null)
                return;

            var back =
                new Button(this)
                {
                    Text =
                        "← KEMBALI KE SERIALIZED FILE"
                };

            back.Click += delegate
            {
                ReturnToSerializedFiles();
            };

            assetList.AddView(back);

            var h =
                new TextView(this)
                {
                    TextSize = 16
                };

            h.Text =
                "=== ASSET LIST ===\n\n" +
                "Unity Version: " +
                f.file.Metadata.UnityVersion +
                "\n\nPilih asset:";

            assetList.AddView(h);

            int number = 0;

            foreach (var a in f.file.AssetInfos)
            {
                number++;

                AssetFileInfo current = a;

                string type =
                    GetAssetTypeName(
                        current.TypeId);

                string mark =
                    modifiedAssets.Contains(
                        current.PathId)
                        ? " ✓ MODIFIED"
                        : "";

                var b =
                    new Button(this)
                    {
                        Text =
                            "ASSET #" +
                            number +
                            " | " +
                            type +
                            mark +
                            "\nTypeID: " +
                            current.TypeId +
                            "\nPathID: " +
                            current.PathId
                    };

                b.SetMinHeight(110);

                b.Click += delegate
                {
                    OpenAssetInspector(
                        f,
                        current);
                };

                assetList.AddView(b);
            }

            SetStatus(
                "Asset List: " +
                number +
                " asset.\n" +
                "Pilih asset untuk membuka Inspector.");
        }

        private string GetAssetTypeName(int typeId)
        {
            try
            {
                var id =
                    (AssetClassID)typeId;

                string n =
                    id.ToString();

                if (!string.IsNullOrEmpty(n) &&
                    n != typeId.ToString())
                {
                    return n;
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
            AssetsFileInstance f,
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
                        f,
                        asset);

                if (baseField == null)
                    throw new Exception(
                        "GetBaseField mengembalikan NULL.");

                currentAssetsFile = f;
                currentAsset = asset;

                ClearAssetList();

                if (assetList == null)
                    return;

                var back =
                    new Button(this)
                    {
                        Text =
                            "← KEMBALI KE ASSET LIST"
                    };

                back.Click += delegate
                {
                    ReturnToAssetList();
                };

                assetList.AddView(back);

                string type =
                    GetAssetTypeName(
                        asset.TypeId);

                string mark =
                    modifiedAssets.Contains(
                        asset.PathId)
                        ? "\n✓ MODIFIED"
                        : "";

                var header =
                    new TextView(this)
                    {
                        TextSize = 16
                    };

                header.Text =
                    "=== ASSET INSPECTOR ===\n\n" +
                    "Type: " +
                    type +
                    mark +
                    "\n\nTypeID: " +
                    asset.TypeId +
                    "\n\nPathID: " +
                    asset.PathId +
                    "\n\nSemua field dibuka otomatis.";

                assetList.AddView(header);

                if (asset.TypeId == 28)
                {
                    AddTextureButtons(
                        f,
                        asset);
                }

                var rootInfo =
                    new TextView(this)
                    {
                        TextSize = 14
                    };

                rootInfo.Text =
                    "\nRoot: " +
                    SafeFieldName(baseField) +
                    "\nChild Count: " +
                    GetChildCount(baseField) +
                    "\n";

                assetList.AddView(rootInfo);

                AddFieldTree(
                    baseField,
                    0);

                showingAssetList = true;
                showingInspector = true;

                SetStatus(
                    "Inspector: " +
                    type +
                    "\nSemua field dibuka.");
            }
            catch (Exception ex)
            {
                ClearAssetList();

                if (assetList != null)
                {
                    var b =
                        new Button(this)
                        {
                            Text =
                                "← KEMBALI KE ASSET LIST"
                        };

                    b.Click += delegate
                    {
                        ReturnToAssetList();
                    };

                    assetList.AddView(b);
                }

                SetStatus(
                    "Inspector gagal.\n\n" +
                    ex.Message);
            }
        }

        private void AddTextureButtons(
            AssetsFileInstance f,
            AssetFileInfo asset)
        {
            if (assetList == null)
                return;

            var t =
                new TextView(this)
                {
                    Text =
                        "\n=== UNIVERSAL TEXTURE2D TOOLS ===\n" +
                        "Texture2D terdeteksi.",
                    TextSize = 16
                };

            assetList.AddView(t);

            var view =
                new Button(this)
                {
                    Text =
                        "🖼 VIEW TEXTURE"
                };

            view.Click += delegate
            {
                ViewTexture(
                    f,
                    asset);
            };

            assetList.AddView(view);

            var export =
                new Button(this)
                {
                    Text =
                        "📤 EXPORT PNG"
                };

            export.Click += delegate
            {
                StartTextureExport();
            };

            assetList.AddView(export);
        }

        // ============================================================
        // ============================================================
        // UNIVERSAL TEXTURE VIEWER
        // ============================================================
        // ============================================================

        private void ViewTexture(
            AssetsFileInstance f,
            AssetFileInfo asset)
        {
            if (assetsManager == null ||
                assetList == null)
                return;

            try
            {
                SetStatus(
                    "Menganalisis Texture2D...\n" +
                    "Mencari data texture...");

                AssetTypeValueField baseField =
                    assetsManager.GetBaseField(
                        f,
                        asset);

                if (baseField == null)
                    throw new Exception(
                        "GetBaseField() mengembalikan NULL.");

                TextureFile tex =
                    TextureFile.ReadTextureFile(
                        baseField);

                if (tex == null)
                    throw new Exception(
                        "TextureFile gagal dibaca.");

                int width =
                    tex.m_Width;

                int height =
                    tex.m_Height;

                TextureFormat format =
                    (TextureFormat)
                    tex.m_TextureFormat;

                if (width <= 0 ||
                    height <= 0)
                {
                    throw new Exception(
                        "Ukuran texture tidak valid: " +
                        width +
                        "x" +
                        height);
                }

                string source;

                byte[]? streamData =
                    ReadTextureStreamData(
                        f,
                        baseField,
                        out source);

                byte[] data;

                if (streamData != null &&
                    streamData.Length > 0)
                {
                    data = streamData;
                }
                else
                {
                    source =
                        "m_TextureData / GetTextureData()";

                    data =
                        tex.GetTextureData(f);
                }

                if (data == null ||
                    data.Length == 0)
                {
                    throw new Exception(
                        "Data texture kosong.\n\n" +
                        "Format: " +
                        format +
                        "\nSource: " +
                        source);
                }

                UniversalDecodeResult result =
                    DecodeTextureUniversal(
                        data,
                        format,
                        width,
                        height);

                if (result.Bgra == null ||
                    result.Bgra.Length <
                    width * height * 4)
                {
                    throw new Exception(
                        "Universal decoder menghasilkan data " +
                        "BGRA yang tidak valid.\n\n" +
                        result.Diagnostic);
                }

                Bitmap bitmap =
                    CreateBitmapFromBgra(
                        result.Bgra,
                        width,
                        height);

                ShowTexturePreview(
                    bitmap,
                    width,
                    height,
                    format,
                    result.EncodedBytesUsed,
                    result.Bgra.Length,
                    f,
                    asset,
                    source,
                    result.Diagnostic);

                SetStatus(
                    "✓ UNIVERSAL TEXTURE DECODE BERHASIL\n\n" +
                    width +
                    "x" +
                    height +
                    "\nFormat: " +
                    format +
                    "\nDecoder: " +
                    result.DecoderName);
            }
            catch (Exception ex)
            {
                ShowTextureError(
                    asset,
                    ex);
            }
        }

        // ============================================================
        // UNIVERSAL DECODER RESULT
        // ============================================================

        private class UniversalDecodeResult
        {
            public byte[] Bgra { get; set; } =
                Array.Empty<byte>();

            public int EncodedBytesUsed { get; set; }

            public string DecoderName { get; set; } =
                "";

            public string Diagnostic { get; set; } =
                "";
        }

        // ============================================================
        // UNIVERSAL DECODER
        // ============================================================

        private UniversalDecodeResult DecodeTextureUniversal(
            byte[] originalData,
            TextureFormat format,
            int width,
            int height)
        {
            if (originalData == null ||
                originalData.Length == 0)
            {
                throw new Exception(
                    "Input texture kosong.");
            }

            string formatName =
                format.ToString();

            int expected =
                GetMip0Size(
                    format,
                    width,
                    height);

            var attempts =
                new List<TextureFormat>();

            // Primary format selalu dicoba pertama.
            attempts.Add(format);

            // --------------------------------------------------------
            // FALLBACK UNIVERSAL
            // --------------------------------------------------------
            //
            // Beberapa Unity texture/asset lama bisa mempunyai metadata
            // format yang tidak sepenuhnya cocok dengan payload compressed.
            //
            // Kita TIDAK mengubah format secara sembarangan.
            // Fallback hanya digunakan jika decoder primary gagal.
            // --------------------------------------------------------

            AddCompatibleFallbacks(
                format,
                attempts);

            var errors =
                new List<string>();

            foreach (TextureFormat candidate
                     in attempts.Distinct())
            {
                try
                {
                    int candidateExpected =
                        GetMip0Size(
                            candidate,
                            width,
                            height);

                    byte[] input;

                    if (candidateExpected > 0)
                    {
                        if (originalData.Length <
                            candidateExpected)
                        {
                            errors.Add(
                                candidate +
                                ": data kurang (" +
                                originalData.Length +
                                " < " +
                                candidateExpected +
                                ")");

                            continue;
                        }

                        input =
                            new byte[candidateExpected];

                        Buffer.BlockCopy(
                            originalData,
                            0,
                            input,
                            0,
                            candidateExpected);
                    }
                    else
                    {
                        input =
                            originalData;
                    }

                    byte[] bgra =
                        TextureFile.DecodeManaged(
                            input,
                            candidate,
                            width,
                            height,
                            true);

                    if (bgra == null ||
                        bgra.Length <
                        width * height * 4)
                    {
                        errors.Add(
                            candidate +
                            ": output BGRA tidak valid.");

                        continue;
                    }

                    string decoder =
                        candidate == format
                            ? "AssetRipper Managed / Primary"
                            : "AssetRipper Managed / Fallback: " +
                              candidate;

                    return new UniversalDecodeResult
                    {
                        Bgra = bgra,
                        EncodedBytesUsed =
                            input.Length,
                        DecoderName = decoder,
                        Diagnostic =
                            "Primary format: " +
                            formatName +
                            "\nSelected format: " +
                            candidate +
                            "\nWidth: " +
                            width +
                            "\nHeight: " +
                            height +
                            "\nExpected mip0: " +
                            candidateExpected +
                            "\nInput used: " +
                            input.Length +
                            "\n\nDecoder berhasil."
                    };
                }
                catch (Exception ex)
                {
                    errors.Add(
                        candidate +
                        ": " +
                        ex.Message);
                }
            }

            string expectedText =
                expected > 0
                    ? expected.ToString()
                    : "unknown";

            throw new Exception(
                "SEMUA UNIVERSAL DECODER GAGAL.\n\n" +
                "Format metadata: " +
                formatName +
                "\n" +
                "Size: " +
                width +
                " x " +
                height +
                "\n" +
                "Input: " +
                originalData.Length +
                " bytes\n" +
                "Expected mip0: " +
                expectedText +
                " bytes\n\n" +
                "=== ATTEMPTS ===\n" +
                string.Join(
                    "\n",
                    errors));
        }

        // ============================================================
        // COMPATIBLE FORMAT FALLBACKS
        // ============================================================

        private void AddCompatibleFallbacks(
            TextureFormat format,
            List<TextureFormat> attempts)
        {
            string n =
                format.ToString()
                    .ToUpperInvariant();

            // ETC / ETC2 family.
            //
            // Ini penting untuk kasus seperti:
            //
            // input 32768
            // required 65536
            //
            // Decoder ETC2_RGBA8 membutuhkan 16 byte/block,
            // sedangkan ETC RGB membutuhkan 8 byte/block.
            if (n.Contains("ETC2_RGBA8") ||
                n.Contains("ETC2A8"))
            {
                TryAddFormat(
                    attempts,
                    "ETC2_RGB");

                TryAddFormat(
                    attempts,
                    "ETC_RGB4");
            }

            if (n.Contains("ETC2_RGB"))
            {
                TryAddFormat(
                    attempts,
                    "ETC_RGB4");
            }

            if (n.Contains("ETC_RGB4"))
            {
                TryAddFormat(
                    attempts,
                    "ETC2_RGB");
            }

            // ETC2 RGBA1.
            if (n.Contains("ETC2_RGBA1"))
            {
                TryAddFormat(
                    attempts,
                    "ETC2_RGB");
            }

            // ASTC: fallback hanya antar format ASTC.
            // Tidak mencampurkan ASTC dengan ETC.
            if (n.Contains("ASTC"))
            {
                AddAstcSiblingFallbacks(
                    format,
                    attempts);
            }

            // DXT / BC family.
            if (n.Contains("DXT1") ||
                n.Contains("BC1"))
            {
                TryAddFormat(
                    attempts,
                    "BC1");
                TryAddFormat(
                    attempts,
                    "DXT1");
            }

            if (n.Contains("DXT5") ||
                n.Contains("BC3"))
            {
                TryAddFormat(
                    attempts,
                    "BC3");
                TryAddFormat(
                    attempts,
                    "DXT5");
            }
        }

        private void TryAddFormat(
            List<TextureFormat> attempts,
            string name)
        {
            try
            {
                if (Enum.TryParse<TextureFormat>(
                    name,
                    true,
                    out TextureFormat result))
                {
                    attempts.Add(result);
                }
            }
            catch
            {
            }
        }

        private void AddAstcSiblingFallbacks(
            TextureFormat format,
            List<TextureFormat> attempts)
        {
            // Kita tidak menebak block size ASTC lain.
            // Format ASTC yang dibaca dari metadata tetap menjadi
            // prioritas utama.
            //
            // Fungsi ini sengaja kosong untuk menjaga fallback ASTC
            // tidak menghasilkan gambar palsu dengan block size salah.
        }

        // ============================================================
        // MIP 0 SIZE
        // ============================================================

        private int GetMip0Size(
            TextureFormat format,
            int width,
            int height)
        {
            if (width <= 0 ||
                height <= 0)
            {
                return 0;
            }

            string n =
                format.ToString()
                    .ToUpperInvariant();

            // --------------------------------------------------------
            // ASTC
            // --------------------------------------------------------

            if (n.Contains("ASTC"))
            {
                GetAstcBlockSize(
                    format,
                    out int bw,
                    out int bh);

                if (bw <= 0 ||
                    bh <= 0)
                {
                    return 0;
                }

                long bx =
                    (width + bw - 1L) /
                    bw;

                long by =
                    (height + bh - 1L) /
                    bh;

                long size =
                    bx * by * 16L;

                return CheckedSize(
                    size,
                    "ASTC mip 0");
            }

            // --------------------------------------------------------
            // DXT / BC
            // --------------------------------------------------------

            if (n.Contains("DXT1") ||
                n.Contains("BC1"))
            {
                return GetBlockCompressedSize(
                    width,
                    height,
                    4,
                    4,
                    8);
            }

            if (n.Contains("DXT3") ||
                n.Contains("DXT5") ||
                n.Contains("BC2") ||
                n.Contains("BC3") ||
                n.Contains("BC4") ||
                n.Contains("BC5") ||
                n.Contains("BC6") ||
                n.Contains("BC7"))
            {
                return GetBlockCompressedSize(
                    width,
                    height,
                    4,
                    4,
                    16);
            }

            // --------------------------------------------------------
            // ETC
            // --------------------------------------------------------

            if (n.Contains("ETC_RGB4"))
            {
                return GetBlockCompressedSize(
                    width,
                    height,
                    4,
                    4,
                    8);
            }

            if (n.Contains("ETC2_RGB") ||
                n.Contains("ETC2_RGBA1"))
            {
                return GetBlockCompressedSize(
                    width,
                    height,
                    4,
                    4,
                    8);
            }

            if (n.Contains("ETC2_RGBA8") ||
                n.Contains("ETC2A8"))
            {
                return GetBlockCompressedSize(
                    width,
                    height,
                    4,
                    4,
                    16);
            }

            // --------------------------------------------------------
            // EAC
            // --------------------------------------------------------

            if (n.Contains("EAC_R_SIGNED") ||
                n.Contains("EAC_R"))
            {
                return GetBlockCompressedSize(
                    width,
                    height,
                    4,
                    4,
                    8);
            }

            if (n.Contains("EAC_RG_SIGNED") ||
                n.Contains("EAC_RG"))
            {
                return GetBlockCompressedSize(
                    width,
                    height,
                    4,
                    4,
                    16);
            }

            // --------------------------------------------------------
            // PVRTC
            // --------------------------------------------------------

            if (n.Contains("PVRTC_RGB4") ||
                n.Contains("PVRTC_RGBA4"))
            {
                long pixels =
                    (long)Math.Max(width, 8) *
                    Math.Max(height, 8);

                long size =
                    (pixels + 1L) / 2L;

                return CheckedSize(
                    size,
                    "PVRTC 4bpp mip 0");
            }

            if (n.Contains("PVRTC_RGB2") ||
                n.Contains("PVRTC_RGBA2"))
            {
                long pixels =
                    (long)Math.Max(width, 16) *
                    Math.Max(height, 8);

                long size =
                    (pixels + 3L) / 4L;

                return CheckedSize(
                    size,
                    "PVRTC 2bpp mip 0");
            }

            // --------------------------------------------------------
            // UNCOMPRESSED
            // --------------------------------------------------------

            int bpp =
                GetUncompressedBytesPerPixel(n);

            if (bpp > 0)
            {
                long size =
                    (long)width *
                    height *
                    bpp;

                return CheckedSize(
                    size,
                    "uncompressed mip 0");
            }

            return 0;
        }

        private int GetBlockCompressedSize(
            int width,
            int height,
            int bw,
            int bh,
            int bytesPerBlock)
        {
            long blocksX =
                (width + bw - 1L) /
                bw;

            long blocksY =
                (height + bh - 1L) /
                bh;

            long size =
                blocksX *
                blocksY *
                bytesPerBlock;

            return CheckedSize(
                size,
                "block compressed mip 0");
        }

        private int GetUncompressedBytesPerPixel(
            string n)
        {
            if (n.Contains("RGBA32") ||
                n.Contains("BGRA32") ||
                n == "ARGB32")
            {
                return 4;
            }

            if (n == "RGB24")
                return 3;

            if (n.Contains("RGB565") ||
                n.Contains("ARGB4444") ||
                n.Contains("RGBA4444"))
            {
                return 2;
            }

            if (n == "ALPHA8" ||
                n == "R8")
            {
                return 1;
            }

            if (n.Contains("R16") ||
                n.Contains("RHALF") ||
                n.Contains("RG16") ||
                n.Contains("RGHALF"))
            {
                return 2;
            }

            if (n.Contains("RFLOAT") ||
                n == "R32" ||
                n.Contains("R32_SFLOAT"))
            {
                return 4;
            }

            if (n.Contains("RGBAHALF") ||
                n.Contains("RGBAFLOAT"))
            {
                return 8;
            }

            return 0;
        }

        private int CheckedSize(
            long size,
            string label)
        {
            if (size <= 0 ||
                size > int.MaxValue)
            {
                throw new Exception(
                    "Ukuran " +
                    label +
                    " tidak valid: " +
                    size);
            }

            return (int)size;
        }

        // ============================================================
        // ASTC HELPERS
        // ============================================================

        private bool IsAstcFormat(
            TextureFormat format)
        {
            return
                format.ToString()
                    .IndexOf(
                        "ASTC",
                        StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void GetAstcBlockSize(
            TextureFormat format,
            out int bw,
            out int bh)
        {
            bw = 0;
            bh = 0;

            string n =
                format.ToString()
                    .ToUpperInvariant();

            string[] sizes =
            {
                "4X4",
                "5X4",
                "5X5",
                "6X5",
                "6X6",
                "8X5",
                "8X6",
                "8X8",
                "10X5",
                "10X6",
                "10X8",
                "10X10",
                "12X10",
                "12X12"
            };

            foreach (string size in sizes)
            {
                if (!n.Contains(size))
                    continue;

                string[] parts =
                    size.Split('X');

                bw =
                    int.Parse(
                        parts[0]);

                bh =
                    int.Parse(
                        parts[1]);

                return;
            }
        }

        // ============================================================
        // TEXTURE STREAM / .resS
        // ============================================================

        private AssetTypeValueField? FindField(
            AssetTypeValueField root,
            string name)
        {
            if (root == null)
                return null;

            if (string.Equals(
                SafeFieldName(root),
                name,
                StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0;
                 i < GetChildCount(root);
                 i++)
            {
                AssetTypeValueField? child =
                    GetChild(root, i);

                if (child == null)
                    continue;

                AssetTypeValueField? result =
                    FindField(
                        child,
                        name);

                if (result != null)
                    return result;
            }

            return null;
        }

        private bool TryGetFieldLong(
            AssetTypeValueField? field,
            out long value)
        {
            value = 0;

            if (field == null)
                return false;

            try
            {
                value =
                    field.Value.AsLong;

                return true;
            }
            catch
            {
            }

            try
            {
                value =
                    (long)
                    field.Value.AsULong;

                return true;
            }
            catch
            {
            }

            try
            {
                value =
                    field.Value.AsInt;

                return true;
            }
            catch
            {
            }

            try
            {
                value =
                    field.Value.AsUInt;

                return true;
            }
            catch
            {
            }

            string text =
                GetDisplayValue(field);

            return long.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);
        }

        private string GetTextureStreamPath(
            AssetTypeValueField baseField)
        {
            AssetTypeValueField? stream =
                FindField(
                    baseField,
                    "m_StreamData");

            if (stream == null)
                return "";

            return GetDisplayValue(
                FindField(
                    stream,
                    "path"));
        }

        private byte[]? ReadTextureStreamData(
            AssetsFileInstance assetsFile,
            AssetTypeValueField baseField,
            out string source)
        {
            source = "";

            AssetTypeValueField? stream =
                FindField(
                    baseField,
                    "m_StreamData");

            if (stream == null)
                return null;

            AssetTypeValueField? offsetField =
                FindField(
                    stream,
                    "offset");

            AssetTypeValueField? sizeField =
                FindField(
                    stream,
                    "size");

            AssetTypeValueField? pathField =
                FindField(
                    stream,
                    "path");

            if (!TryGetFieldLong(
                    offsetField,
                    out long offset) ||
                !TryGetFieldLong(
                    sizeField,
                    out long size))
            {
                return null;
            }

            string path =
                GetDisplayValue(
                    pathField);

            if (size <= 0)
                return null;

            if (offset < 0)
            {
                throw new Exception(
                    "m_StreamData.offset tidak valid: " +
                    offset);
            }

            if (size > int.MaxValue)
            {
                throw new Exception(
                    "m_StreamData.size terlalu besar: " +
                    size);
            }

            // --------------------------------------------------------
            // archive:/... di dalam AssetBundle
            // --------------------------------------------------------

            if (path.StartsWith(
                "archive:/",
                StringComparison.OrdinalIgnoreCase))
            {
                BundleFileInstance? bundle =
                    assetsFile.parentBundle ??
                    currentBundle;

                if (bundle == null)
                {
                    throw new Exception(
                        "Texture memakai archive:/ tetapi " +
                        "parent bundle tidak ditemukan.");
                }

                string archivePath =
                    path.Substring(
                        "archive:/".Length)
                        .Replace(
                            '\\',
                            '/');

                int slash =
                    archivePath.LastIndexOf('/');

                string entryName =
                    slash >= 0
                        ? archivePath.Substring(
                            slash + 1)
                        : archivePath;

                int index =
                    bundle.file.GetFileIndex(
                        entryName);

                if (index < 0)
                {
                    for (
                        int i = 0;
                        i <
                        bundle.file.BlockAndDirInfo
                            .DirectoryInfos.Count();
                        i++)
                    {
                        string dirName =
                            bundle.file
                                .BlockAndDirInfo
                                .DirectoryInfos[i]
                                .Name;

                        if (
                            string.Equals(
                                dirName,
                                entryName,
                                StringComparison.OrdinalIgnoreCase) ||
                            dirName.EndsWith(
                                entryName,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            index = i;
                            break;
                        }
                    }
                }

                if (index < 0)
                {
                    throw new Exception(
                        "Entry .resS tidak ditemukan " +
                        "di AssetBundle.\n\n" +
                        "Path:\n" +
                        path +
                        "\n\nEntry:\n" +
                        entryName);
                }

                byte[] entryData =
                    BundleHelper.LoadAssetDataFromBundle(
                        bundle.file,
                        index);

                if (entryData == null ||
                    entryData.Length == 0)
                {
                    throw new Exception(
                        "Entry .resS ditemukan tetapi " +
                        "datanya kosong.\n" +
                        "Entry: " +
                        entryName);
                }

                if (offset + size >
                    entryData.LongLength)
                {
                    throw new Exception(
                        "m_StreamData berada di luar " +
                        "ukuran entry .resS.\n\n" +
                        "Offset: " +
                        offset +
                        "\nSize: " +
                        size +
                        "\nEntry Size: " +
                        entryData.Length);
                }

                byte[] result =
                    new byte[(int)size];

                Buffer.BlockCopy(
                    entryData,
                    (int)offset,
                    result,
                    0,
                    (int)size);

                source =
                    "m_StreamData → AssetBundle .resS\n" +
                    entryName +
                    "\noffset=" +
                    offset +
                    ", size=" +
                    size;

                return result;
            }

            // --------------------------------------------------------
            // external .resS
            // --------------------------------------------------------

            string cleanPath =
                path.Replace(
                    '\\',
                    IOPath.DirectorySeparatorChar)
                    .TrimStart(
                        IOPath.DirectorySeparatorChar,
                        IOPath.AltDirectorySeparatorChar);

            string? baseDirectory =
                IOPath.GetDirectoryName(
                    assetsFile.path);

            if (string.IsNullOrEmpty(
                    baseDirectory))
            {
                baseDirectory =
                    CacheDir?.AbsolutePath;
            }

            if (!string.IsNullOrEmpty(
                    baseDirectory))
            {
                string externalPath =
                    IOPath.Combine(
                        baseDirectory,
                        cleanPath);

                if (File.Exists(
                        externalPath))
                {
                    using FileStream fs =
                        File.OpenRead(
                            externalPath);

                    if (offset + size >
                        fs.Length)
                    {
                        throw new Exception(
                            "m_StreamData berada di luar " +
                            "file .resS.\n\n" +
                            "File: " +
                            externalPath +
                            "\nOffset: " +
                            offset +
                            "\nSize: " +
                            size +
                            "\nFile Size: " +
                            fs.Length);
                    }

                    fs.Position = offset;

                    byte[] result =
                        new byte[(int)size];

                    int total = 0;

                    while (
                        total <
                        result.Length)
                    {
                        int read =
                            fs.Read(
                                result,
                                total,
                                result.Length -
                                total);

                        if (read <= 0)
                            break;

                        total += read;
                    }

                    if (total !=
                        result.Length)
                    {
                        throw new Exception(
                            "Gagal membaca seluruh " +
                            "data .resS.\n" +
                            "Expected: " +
                            result.Length +
                            "\nActual: " +
                            total);
                    }

                    source =
                        "m_StreamData → external .resS\n" +
                        externalPath +
                        "\noffset=" +
                        offset +
                        ", size=" +
                        size;

                    return result;
                }
            }

            return null;
        }

        // ============================================================
        // TEXTURE STORAGE DIAGNOSTIC
        // ============================================================

        private string GetTextureStorageInfo(
            AssetTypeValueField baseField)
        {
            string s = "";

            try
            {
                AssetTypeValueField? stream =
                    FindField(
                        baseField,
                        "m_StreamData");

                s +=
                    "\n\n=== m_StreamData ===\n";

                if (stream == null)
                {
                    s +=
                        "Field tidak ditemukan.\n";
                }
                else
                {
                    s +=
                        "offset : " +
                        DisplayOrUnknown(
                            FindField(
                                stream,
                                "offset")) +
                        "\n";

                    s +=
                        "size   : " +
                        DisplayOrUnknown(
                            FindField(
                                stream,
                                "size")) +
                        "\n";

                    s +=
                        "path   : " +
                        DisplayOrUnknown(
                            FindField(
                                stream,
                                "path")) +
                        "\n";
                }
            }
            catch (Exception ex)
            {
                s +=
                    "\nm_StreamData error: " +
                    ex.Message +
                    "\n";
            }

            s +=
                "\nm_CompleteImageSize: " +
                DisplayOrUnknown(
                    FindField(
                        baseField,
                        "m_CompleteImageSize"));

            s +=
                "\n\nm_TextureData: " +
                (
                    FindField(
                        baseField,
                        "m_TextureData") == null
                    ? "tidak ditemukan"
                    : "ditemukan"
                );

            return s;
        }

        private string DisplayOrUnknown(
            AssetTypeValueField? field)
        {
            if (field == null)
                return "(tidak ditemukan)";

            string value =
                GetDisplayValue(field);

            return string.IsNullOrEmpty(value)
                ? "(kosong)"
                : value;
        }

        // ============================================================
        // TEXTURE PREVIEW
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
                    "Ukuran data BGRA tidak sesuai " +
                    "ukuran texture.");
            }

            int[] pixels =
                new int[width * height];

            int p = 0;

            for (
                int i = 0;
                i < pixels.Length;
                i++)
            {
                byte b = bgra[p++];
                byte g = bgra[p++];
                byte r = bgra[p++];
                byte a = bgra[p++];

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

        private void ShowTexturePreview(
            Bitmap bitmap,
            int width,
            int height,
            TextureFormat format,
            int encodedSize,
            int decodedSize,
            AssetsFileInstance f,
            AssetFileInfo asset,
            string source,
            string diagnostic)
        {
            ClearAssetList();

            if (assetList == null)
                return;

            var back =
                new Button(this)
                {
                    Text =
                        "← KEMBALI KE INSPECTOR"
                };

            back.Click += delegate
            {
                OpenAssetInspector(
                    f,
                    asset);
            };

            assetList.AddView(back);

            var info =
                new TextView(this)
                {
                    TextSize = 16
                };

            info.Text =
                "=== UNIVERSAL TEXTURE VIEWER ===\n\n" +
                "Size: " +
                width +
                " x " +
                height +
                "\n" +
                "Texture Format: " +
                format +
                "\n" +
                "Encoded Used: " +
                encodedSize +
                " bytes\n" +
                "Decoded: " +
                decodedSize +
                " bytes\n" +
                "Preview: BGRA → Android Bitmap\n\n" +
                "DATA SOURCE:\n" +
                source +
                "\n\n" +
                "DECODER:\n" +
                diagnostic;

            assetList.AddView(info);

            var image =
                new ImageView(this);

            image.SetImageBitmap(
                bitmap);

            image.SetAdjustViewBounds(
                true);

            image.SetScaleType(
                ImageView.ScaleType.FitCenter);

            var ip =
                new LinearLayout.LayoutParams(
                    -1,
                    -2);

            ip.SetMargins(
                0,
                20,
                0,
                20);

            assetList.AddView(
                image,
                ip);

            var export =
                new Button(this)
                {
                    Text =
                        "📤 EXPORT PNG"
                };

            export.Click += delegate
            {
                StartTextureExport();
            };

            assetList.AddView(export);
        }

        // ============================================================
        // TEXTURE ERROR
        // ============================================================

        private void ShowTextureError(
            AssetFileInfo asset,
            Exception ex)
        {
            ClearAssetList();

            if (assetList == null)
                return;

            var title =
                new TextView(this)
                {
                    Text =
                        "❌ UNIVERSAL TEXTURE VIEWER GAGAL",
                    TextSize = 20
                };

            assetList.AddView(title);

            var error =
                new TextView(this)
                {
                    TextSize = 15
                };

            error.Text =
                "Asset Texture2D\n\n" +
                "TypeID: " +
                asset.TypeId +
                "\n" +
                "PathID: " +
                asset.PathId +
                "\n\n" +
                "ERROR:\n" +
                ex.Message +
                "\n\nDETAIL:\n" +
                ex;

            assetList.AddView(error);

            var back =
                new Button(this)
                {
                    Text =
                        "← KEMBALI KE INSPECTOR"
                };

            back.Click += delegate
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

            SetStatus(
                "Universal Texture Viewer gagal.\n" +
                "Detail error ditampilkan.");
        }

        private void ShowTextureMessage(
            string title,
            string message)
        {
            ClearAssetList();

            if (assetList == null)
                return;

            var h =
                new TextView(this)
                {
                    Text =
                        "=== " +
                        title +
                        " ===",
                    TextSize = 20
                };

            assetList.AddView(h);

            var t =
                new TextView(this)
                {
                    Text =
                        message,
                    TextSize = 16
                };

            assetList.AddView(t);

            var b =
                new Button(this)
                {
                    Text =
                        "← KEMBALI KE INSPECTOR"
                };

            b.Click += delegate
            {
                if (currentAssetsFile != null &&
                    currentAsset != null)
                {
                    OpenAssetInspector(
                        currentAssetsFile,
                        currentAsset);
                }
            };

            assetList.AddView(b);
        }

        // ============================================================
        // EXPORT PNG
        // ============================================================

        private void StartTextureExport()
        {
            try
            {
                if (currentAssetsFile == null ||
                    currentAsset == null)
                {
                    throw new Exception(
                        "Texture aktif tidak ditemukan.");
                }

                var intent =
                    new Intent(
                        Intent.ActionCreateDocument);

                intent.AddCategory(
                    Intent.CategoryOpenable);

                intent.SetType(
                    "image/png");

                intent.PutExtra(
                    Intent.ExtraTitle,
                    "texture.png");

                StartActivityForResult(
                    intent,
                    ExportTextureRequestCode);
            }
            catch (Exception ex)
            {
                SetStatus(
                    "Export gagal dimulai.\n\n" +
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
                    "Texture aktif tidak ditemukan.");
            }

            AssetTypeValueField baseField =
                assetsManager.GetBaseField(
                    currentAssetsFile,
                    currentAsset);

            if (baseField == null)
                throw new Exception(
                    "GetBaseField() gagal.");

            TextureFile tex =
                TextureFile.ReadTextureFile(
                    baseField);

            if (tex == null)
                throw new Exception(
                    "TextureFile gagal dibaca.");

            int width =
                tex.m_Width;

            int height =
                tex.m_Height;

            TextureFormat format =
                (TextureFormat)
                tex.m_TextureFormat;

            string source;

            byte[]? streamData =
                ReadTextureStreamData(
                    currentAssetsFile,
                    baseField,
                    out source);

            byte[] data;

            if (streamData != null &&
                streamData.Length > 0)
            {
                data = streamData;
            }
            else
            {
                source =
                    "m_TextureData / GetTextureData()";

                data =
                    tex.GetTextureData(
                        currentAssetsFile);
            }

            if (data == null ||
                data.Length == 0)
            {
                throw new Exception(
                    "Data texture kosong.\n" +
                    "Sumber: " +
                    source);
            }

            UniversalDecodeResult result =
                DecodeTextureUniversal(
                    data,
                    format,
                    width,
                    height);

            Bitmap bitmap =
                CreateBitmapFromBgra(
                    result.Bgra,
                    width,
                    height);

            using Stream? output =
                ContentResolver.OpenOutputStream(
                    outputUri);

            if (output == null)
                throw new Exception(
                    "Tidak dapat membuka output.");

            bool ok =
                bitmap.Compress(
                    Bitmap.CompressFormat.Png,
                    100,
                    output);

            output.Flush();

            bitmap.Recycle();

            if (!ok)
                throw new Exception(
                    "Bitmap gagal diexport ke PNG.");

            SetStatus(
                "✓ EXPORT PNG BERHASIL\n\n" +
                width +
                "x" +
                height +
                "\nFormat: " +
                format +
                "\nDecoder: " +
                result.DecoderName +
                "\nSource: " +
                source);
        }

        // ============================================================
        // FIELD TREE / EDITOR
        // ============================================================

        private void AddFieldTree(
            AssetTypeValueField field,
            int depth)
        {
            if (field == null ||
                assetList == null)
                return;

            int count =
                GetChildCount(field);

            string name =
                SafeFieldName(field);

            string value =
                GetDisplayValue(field);

            string indent =
                MakeIndent(depth);

            var container =
                new LinearLayout(this)
                {
                    Orientation =
                        Orientation.Vertical
                };

            var text =
                new TextView(this)
                {
                    TextSize = 15
                };

            if (count > 0)
            {
                text.Text =
                    indent +
                    "▼ " +
                    name +
                    " [" +
                    count +
                    "]";

                container.AddView(text);

                var children =
                    new LinearLayout(this)
                    {
                        Orientation =
                            Orientation.Vertical
                    };

                children.Visibility =
                    AndroidViewStates.Visible;

                for (
                    int i = 0;
                    i < count;
                    i++)
                {
                    var child =
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

                text.Click += delegate
                {
                    children.Visibility =
                        children.Visibility ==
                        AndroidViewStates.Gone
                            ? AndroidViewStates.Visible
                            : AndroidViewStates.Gone;

                    text.Text =
                        indent +
                        (
                            children.Visibility ==
                            AndroidViewStates.Visible
                                ? "▼ "
                                : "▶ "
                        ) +
                        name +
                        " [" +
                        count +
                        "]";
                };
            }
            else
            {
                text.Text =
                    string.IsNullOrEmpty(value)
                        ? indent +
                          "• " +
                          name
                        : indent +
                          "• " +
                          name +
                          " = " +
                          value;

                container.AddView(
                    text);

                text.Click += delegate
                {
                    ShowFieldEditor(field);
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

            int count =
                GetChildCount(field);

            string name =
                SafeFieldName(field);

            string value =
                GetDisplayValue(field);

            string indent =
                MakeIndent(depth);

            var text =
                new TextView(this)
                {
                    TextSize = 15
                };

            if (count > 0)
            {
                text.Text =
                    indent +
                    "▼ " +
                    name +
                    " [" +
                    count +
                    "]";

                parent.AddView(text);

                var children =
                    new LinearLayout(this)
                    {
                        Orientation =
                            Orientation.Vertical
                    };

                children.Visibility =
                    AndroidViewStates.Visible;

                for (
                    int i = 0;
                    i < count;
                    i++)
                {
                    var child =
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

                text.Click += delegate
                {
                    children.Visibility =
                        children.Visibility ==
                        AndroidViewStates.Gone
                            ? AndroidViewStates.Visible
                            : AndroidViewStates.Gone;

                    text.Text =
                        indent +
                        (
                            children.Visibility ==
                            AndroidViewStates.Visible
                                ? "▼ "
                                : "▶ "
                        ) +
                        name +
                        " [" +
                        count +
                        "]";
                };
            }
            else
            {
                text.Text =
                    string.IsNullOrEmpty(value)
                        ? indent +
                          "• " +
                          name
                        : indent +
                          "• " +
                          name +
                          " = " +
                          value;

                parent.AddView(text);

                text.Click += delegate
                {
                    ShowFieldEditor(field);
                };
            }
        }

        private int GetChildCount(
            AssetTypeValueField field)
        {
            try
            {
                return field.Children == null
                    ? 0
                    : field.Children.Count;
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
                string v =
                    field.Value.AsString;

                if (!string.IsNullOrEmpty(v))
                    return v;
            }
            catch
            {
            }

            try
            {
                return field.Value.AsBool
                    .ToString();
            }
            catch
            {
            }

            try
            {
                return field.Value.AsInt
                    .ToString(
                        CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            try
            {
                return field.Value.AsUInt
                    .ToString(
                        CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            try
            {
                return field.Value.AsLong
                    .ToString(
                        CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            try
            {
                return field.Value.AsULong
                    .ToString(
                        CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            try
            {
                return field.Value.AsFloat
                    .ToString(
                        CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            try
            {
                return field.Value.AsDouble
                    .ToString(
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
            string name =
                SafeFieldName(field);

            string current =
                GetDisplayValue(field);

            var builder =
                new AlertDialog.Builder(this);

            builder.SetTitle(
                "EDIT FIELD");

            var layout =
                new LinearLayout(this)
                {
                    Orientation =
                        Orientation.Vertical
                };

            layout.SetPadding(
                40,
                20,
                40,
                20);

            var info =
                new TextView(this)
                {
                    Text =
                        "Field:\n" +
                        name +
                        "\n\nNilai sekarang:\n" +
                        current +
                        "\n\nNilai baru:",
                    TextSize = 16
                };

            layout.AddView(info);

            var input =
                new EditText(this)
                {
                    Text = current
                };

            input.SetSingleLine(true);

            layout.AddView(input);

            builder.SetView(layout);

            builder.SetNegativeButton(
                "CANCEL",
                (s, e) => { });

            builder.SetPositiveButton(
                "APPLY",
                (s, e) =>
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
                            name +
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
                    out bool b))
            {
                try
                {
                    field.Value.AsBool = b;
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
                    out int i))
            {
                try
                {
                    field.Value.AsInt = i;
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
                    out uint ui))
            {
                try
                {
                    field.Value.AsUInt = ui;
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
                    out long l))
            {
                try
                {
                    field.Value.AsLong = l;
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
                    out ulong ul))
            {
                try
                {
                    field.Value.AsULong = ul;
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
                    out float fl))
            {
                try
                {
                    field.Value.AsFloat = fl;
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
                    out double d))
            {
                try
                {
                    field.Value.AsDouble = d;
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
            return depth <= 0
                ? ""
                : new string(
                    ' ',
                    depth * 4);
        }

        private void ReturnToAssetList()
        {
            if (currentAssetsFile == null)
                return;

            showingInspector = false;
            showingAssetList = true;

            ShowAssetList(
                currentAssetsFile);
        }

        private void ReturnToSerializedFiles()
        {
            if (currentBundle == null)
                return;

            showingInspector = false;
            showingAssetList = false;
            showingSerializedFiles = true;

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
                showingSerializedFiles = false;

                ClearAssetList();

                SetStatus(
                    "Tekan OPEN UNITY3D.");

                return;
            }

            base.OnBackPressed();
        }

        private void ClearAssetList()
        {
            assetList?.RemoveAllViews();
        }

        private void SetStatus(
            string message)
        {
            RunOnUiThread(
                delegate
                {
                    if (status != null)
                        status.Text = message;
                });
        }
    }
}
