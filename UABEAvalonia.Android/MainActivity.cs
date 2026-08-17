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
        private bool showingSerializedFiles, showingAssetList, showingInspector;
        private readonly HashSet<long> modifiedAssets = new HashSet<long>();

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            BuildMainInterface();
            try { assetsManager = new AssetsManager(); SetStatus("AssetsTools.NET siap.\n\nTekan OPEN UNITY3D."); }
            catch (Exception ex) { SetStatus("AssetsTools.NET gagal dimuat.\n\n" + ex.Message); }
        }

        private void BuildMainInterface()
        {
            var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
            root.SetPadding(30, 30, 30, 30);
            var title = new TextView(this) { Text = "UABEA Android", TextSize = 24 };
            var open = new Button(this) { Text = "OPEN UNITY3D" };
            status = new TextView(this) { Text = "Menyiapkan UABEA Android...", TextSize = 14 };
            root.AddView(title); root.AddView(open); root.AddView(status, new LinearLayout.LayoutParams(-1, 100));
            assetList = new LinearLayout(this) { Orientation = Orientation.Vertical };
            var scroll = new ScrollView(this); scroll.FillViewport = true; scroll.AddView(assetList);
            var sp = new LinearLayout.LayoutParams(-1, 0) { Weight = 1 };
            root.AddView(scroll, sp); SetContentView(root);
            open.Click += delegate { OpenFilePicker(); };
        }

        private void OpenFilePicker()
        {
            try
            {
                var intent = new Intent(Intent.ActionOpenDocument);
                intent.AddCategory(Intent.CategoryOpenable); intent.SetType("*/*");
                StartActivityForResult(intent, PickFileRequestCode);
            }
            catch (Exception ex) { SetStatus("File Picker gagal.\n\n" + ex.Message); }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode != Result.Ok || data?.Data == null)
            {
                if (requestCode == PickFileRequestCode) SetStatus("Pemilihan file dibatalkan.");
                return;
            }
            try
            {
                if (requestCode == PickFileRequestCode) ReadUnityBundle(data.Data);
                else if (requestCode == ExportTextureRequestCode) ExportCurrentTexture(data.Data);
            }
            catch (Exception ex) { SetStatus("Operasi gagal.\n\n" + ex.Message); }
        }

        private string GetFileName(AndroidUri uri)
        {
            string name = "temp.unity3d";
            using (var c = ContentResolver.Query(uri, null, null, null, null))
            {
                if (c != null)
                {
                    int i = c.GetColumnIndex(OpenableColumns.DisplayName);
                    if (i >= 0 && c.MoveToFirst())
                    {
                        string? n = c.GetString(i);
                        if (!string.IsNullOrWhiteSpace(n)) name = n;
                    }
                }
            }
            foreach (char ch in IOPath.GetInvalidFileNameChars()) name = name.Replace(ch, '_');
            return name;
        }

        private string CopyToCache(AndroidUri uri, string fileName)
        {
            if (CacheDir == null) throw new Exception("CacheDir tidak tersedia.");
            string path = IOPath.Combine(CacheDir.AbsolutePath, fileName);
            using (Stream? input = ContentResolver.OpenInputStream(uri))
            {
                if (input == null) throw new Exception("Tidak dapat membaca file.");
                using (FileStream output = File.Create(path)) input.CopyTo(output);
            }
            return path;
        }

        private void ReadUnityBundle(AndroidUri uri)
        {
            if (assetsManager == null) throw new Exception("AssetsManager tidak tersedia.");
            SetStatus("Menyalin file...");
            string name = GetFileName(uri);
            string path = CopyToCache(uri, name);
            var info = new FileInfo(path);
            SetStatus("Membuka AssetBundle...");
            BundleFileInstance bundle = assetsManager.LoadBundleFile(path);
            if (bundle == null) throw new Exception("AssetBundle gagal dibuka.");
            currentBundle = bundle; currentAssetsFile = null; currentAsset = null; modifiedAssets.Clear();
            showingSerializedFiles = true; showingAssetList = false; showingInspector = false;
            ShowSerializedFiles(info.Length, name);
        }

        private void ShowSerializedFiles(long fileSize, string fileName)
        {
            if (currentBundle == null || assetList == null) return;
            ClearAssetList();
            var header = new TextView(this) { TextSize = 16 };
            header.Text = "ASSETBUNDLE BERHASIL DIBUKA!\n\nNama: " + fileName + "\nUkuran: " + fileSize.ToString("N0") + " bytes\n\n=== SERIALIZED FILE ===\n\nPilih file:";
            assetList.AddView(header);
            int index = 0;
            foreach (var directory in currentBundle.file.BlockAndDirInfo.DirectoryInfos)
            {
                int selected = index++;
                var b = new Button(this) { Text = index + ". " + directory.Name };
                b.SetMinHeight(80); b.Click += delegate { LoadSerializedFile(selected); }; assetList.AddView(b);
            }
            SetStatus("Bundle berhasil dibuka.\nPilih SerializedFile.");
        }

        private void LoadSerializedFile(int index)
        {
            if (assetsManager == null || currentBundle == null) return;
            try
            {
                SetStatus("Memuat SerializedFile...");
                AssetsFileInstance f = assetsManager.LoadAssetsFileFromBundle(currentBundle, index, false);
                if (f == null) throw new Exception("SerializedFile tidak dapat dimuat.");
                currentAssetsFile = f; currentAsset = null; showingSerializedFiles = false; showingAssetList = true; showingInspector = false; ShowAssetList(f);
            }
            catch (Exception ex) { SetStatus("Gagal memuat SerializedFile.\n\n" + ex.Message); }
        }

        private void ShowAssetList(AssetsFileInstance f)
        {
            ClearAssetList(); if (assetList == null) return;
            var back = new Button(this) { Text = "← KEMBALI KE SERIALIZED FILE" }; back.Click += delegate { ReturnToSerializedFiles(); }; assetList.AddView(back);
            var h = new TextView(this) { TextSize = 16 };
            h.Text = "=== ASSET LIST ===\n\nUnity Version: " + f.file.Metadata.UnityVersion + "\n\nPilih asset:"; assetList.AddView(h);
            int number = 0;
            foreach (var a in f.file.AssetInfos)
            {
                number++; AssetFileInfo current = a; string type = GetAssetTypeName(current.TypeId); string mark = modifiedAssets.Contains(current.PathId) ? " ✓ MODIFIED" : "";
                var b = new Button(this) { Text = "ASSET #" + number + " | " + type + mark + "\nTypeID: " + current.TypeId + "\nPathID: " + current.PathId };
                b.SetMinHeight(110); b.Click += delegate { OpenAssetInspector(f, current); }; assetList.AddView(b);
            }
            SetStatus("Asset List: " + number + " asset.\nPilih asset untuk membuka Inspector.");
        }

        private string GetAssetTypeName(int typeId)
        {
            try { var id = (AssetClassID)typeId; string n = id.ToString(); if (!string.IsNullOrEmpty(n) && n != typeId.ToString()) return n; } catch { }
            return "Unknown";
        }

        private void OpenAssetInspector(AssetsFileInstance f, AssetFileInfo asset)
        {
            if (assetsManager == null) return;
            try
            {
                SetStatus("Membaca asset...");
                AssetTypeValueField baseField = assetsManager.GetBaseField(f, asset);
                if (baseField == null) throw new Exception("GetBaseField mengembalikan NULL.");
                currentAssetsFile = f; currentAsset = asset; ClearAssetList(); if (assetList == null) return;
                var back = new Button(this) { Text = "← KEMBALI KE ASSET LIST" }; back.Click += delegate { ReturnToAssetList(); }; assetList.AddView(back);
                string type = GetAssetTypeName(asset.TypeId); string mark = modifiedAssets.Contains(asset.PathId) ? "\n✓ MODIFIED" : "";
                var header = new TextView(this) { TextSize = 16 };
                header.Text = "=== ASSET INSPECTOR ===\n\nType: " + type + mark + "\n\nTypeID: " + asset.TypeId + "\n\nPathID: " + asset.PathId + "\n\nSemua field dibuka otomatis."; assetList.AddView(header);
                if (asset.TypeId == 28) AddTextureButtons(f, asset);
                var rootInfo = new TextView(this) { TextSize = 14 };
                rootInfo.Text = "\nRoot: " + SafeFieldName(baseField) + "\nChild Count: " + GetChildCount(baseField) + "\n"; assetList.AddView(rootInfo);
                AddFieldTree(baseField, 0); showingAssetList = true; showingInspector = true; SetStatus("Inspector: " + type + "\nSemua field dibuka.");
            }
            catch (Exception ex)
            {
                ClearAssetList(); if (assetList != null) { var b = new Button(this) { Text = "← KEMBALI KE ASSET LIST" }; b.Click += delegate { ReturnToAssetList(); }; assetList.AddView(b); }
                SetStatus("Inspector gagal.\n\n" + ex.Message);
            }
        }

        private void AddTextureButtons(AssetsFileInstance f, AssetFileInfo asset)
        {
            if (assetList == null) return;
            var t = new TextView(this) { Text = "\n=== TEXTURE2D TOOLS ===\nTexture2D terdeteksi.", TextSize = 16 }; assetList.AddView(t);
            var view = new Button(this) { Text = "🖼 VIEW TEXTURE" }; view.Click += delegate { ViewTexture(f, asset); }; assetList.AddView(view);
            var export = new Button(this) { Text = "📤 EXPORT PNG" }; export.Click += delegate { StartTextureExport(); }; assetList.AddView(export);
        }

        // ============================================================
        // TEXTURE VIEWER + ASTC DIAGNOSTIC
        // ============================================================

        private void ViewTexture(AssetsFileInstance f, AssetFileInfo asset)
        {
            if (assetsManager == null || assetList == null) return;

            try
            {
                AssetTypeValueField baseField =
                    assetsManager.GetBaseField(f, asset);

                if (baseField == null)
                    throw new Exception("GetBaseField() mengembalikan NULL.");

                TextureFile tex =
                    TextureFile.ReadTextureFile(baseField);

                if (tex == null)
                    throw new Exception("TextureFile gagal dibaca.");

                int width = tex.m_Width;
                int height = tex.m_Height;
                TextureFormat format = (TextureFormat)tex.m_TextureFormat;

                if (width <= 0 || height <= 0)
                    throw new Exception(
                        "Ukuran texture tidak valid: " + width + "x" + height);

                byte[] data;
                string dataSource;

                // ------------------------------------------------------------
                // PENTING:
                // Untuk texture streamed (m_StreamData.size > 0), jangan
                // memakai GetTextureData() sebagai sumber ASTC mentah.
                // Data asli berada di .resS pada offset/size m_StreamData.
                // ------------------------------------------------------------
                byte[]? streamData =
                    ReadTextureStreamData(
                        f,
                        baseField,
                        out dataSource);

                if (streamData != null && streamData.Length > 0)
                {
                    data = streamData;
                }
                else
                {
                    dataSource = "m_TextureData / GetTextureData()";
                    data = tex.GetTextureData(f);
                }

                if (data == null || data.Length == 0)
                    throw new Exception(
                        "Data texture kosong.\n\nSumber: " + dataSource);

                if (IsAstcFormat(format))
                {
                    ShowAstcDiagnostic(
                        baseField,
                        tex,
                        data,
                        format,
                        f,
                        asset,
                        dataSource);
                    return;
                }

                int expectedMip0;
                string mipNote;
                data = PrepareMip0Data(
                    data,
                    format,
                    width,
                    height,
                    dataSource,
                    out expectedMip0,
                    out mipNote);

                byte[] bgra =
                    TextureFile.DecodeManaged(
                        data,
                        format,
                        width,
                        height,
                        true);

                if (bgra == null ||
                    bgra.Length < width * height * 4)
                {
                    throw new Exception(
                        "DecodeManaged() menghasilkan data tidak valid.\n" +
                        "Format: " + format +
                        "\nSumber: " + dataSource +
                        "\nMip0: " + expectedMip0 +
                        " bytes\nActual input: " + data.Length + " bytes\n" +
                        mipNote);
                }

                Bitmap bitmap =
                    CreateBitmapFromBgra(
                        bgra,
                        width,
                        height);

                ShowTexturePreview(
                    bitmap,
                    width,
                    height,
                    format,
                    data.Length,
                    bgra.Length,
                    f,
                    asset);

                SetStatus(
                    "✓ Texture berhasil didecode.\n" +
                    width + "x" + height +
                    " / " + format +
                    "\nSumber: " + dataSource);
            }
            catch (Exception ex)
            {
                ShowTextureError(asset, ex);
            }
        }

        // ============================================================
        // EXACT MIP 0 DATA
        // ============================================================
        // Unity texture data can contain more than one mip level, or
        // padding/data that is not part of mip 0. The decoder must receive
        // exactly the bytes needed by mip 0 whenever we know that size.
        // This is especially important for ETC/ETC2/ASTC/BC/DXT/PVRTC.
        // ============================================================

        private int GetMip0Size(TextureFormat format, int width, int height)
        {
            if (width <= 0 || height <= 0)
                return 0;

            string n = format.ToString().ToUpperInvariant();

            // ASTC: one 16-byte block per compressed block.
            if (n.Contains("ASTC"))
            {
                GetAstcBlockSize(format, out int bw, out int bh);
                if (bw <= 0 || bh <= 0)
                    return 0;

                long bx = (width + bw - 1L) / bw;
                long by = (height + bh - 1L) / bh;
                long size = bx * by * 16L;
                return CheckedSize(size, "ASTC mip 0");
            }

            // DXT / BC: 4x4 blocks.
            // DXT1/BC1 = 8 bytes per block.
            // DXT3/DXT5/BC2/BC3 = 16 bytes per block.
            if (n.Contains("DXT1") || n.Contains("BC1"))
                return GetBlockCompressedSize(width, height, 4, 4, 8);

            if (n.Contains("DXT3") || n.Contains("DXT5") ||
                n.Contains("BC2") || n.Contains("BC3") ||
                n.Contains("BC4") || n.Contains("BC5") ||
                n.Contains("BC6") || n.Contains("BC7"))
                return GetBlockCompressedSize(width, height, 4, 4, 16);

            // ETC1 / ETC_RGB4 = 8 bytes per 4x4 block.
            if (n.Contains("ETC_RGB4") || n == "ETC_RGB4")
                return GetBlockCompressedSize(width, height, 4, 4, 8);

            // ETC2 RGB and ETC2 RGBA1 use 8-byte blocks.
            if (n.Contains("ETC2_RGB") || n.Contains("ETC2_RGBA1"))
                return GetBlockCompressedSize(width, height, 4, 4, 8);

            // ETC2 RGBA8 uses two 8-byte blocks = 16 bytes per 4x4 block.
            if (n.Contains("ETC2_RGBA8"))
                return GetBlockCompressedSize(width, height, 4, 4, 16);

            // EAC R/RG formats are also block compressed.
            if (n.Contains("EAC_R") || n.Contains("EAC_R_SIGNED"))
                return GetBlockCompressedSize(width, height, 4, 4, 8);

            if (n.Contains("EAC_RG") || n.Contains("EAC_RG_SIGNED"))
                return GetBlockCompressedSize(width, height, 4, 4, 16);

            // PVRTC uses a minimum footprint of 2x2 blocks.
            // PVRTC 4bpp = 4 bits/pixel, PVRTC 2bpp = 2 bits/pixel.
            if (n.Contains("PVRTC_RGB4") || n.Contains("PVRTC_RGBA4"))
            {
                long pixels = (long)Math.Max(width, 8) * Math.Max(height, 8);
                long size = (pixels + 1L) / 2L;
                return CheckedSize(size, "PVRTC 4bpp mip 0");
            }

            if (n.Contains("PVRTC_RGB2") || n.Contains("PVRTC_RGBA2"))
            {
                long pixels = (long)Math.Max(width, 16) * Math.Max(height, 8);
                long size = (pixels + 3L) / 4L;
                return CheckedSize(size, "PVRTC 2bpp mip 0");
            }

            // Common uncompressed Unity formats.
            int bytesPerPixel = GetUncompressedBytesPerPixel(n);
            if (bytesPerPixel > 0)
            {
                long size = (long)width * height * bytesPerPixel;
                return CheckedSize(size, "uncompressed mip 0");
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
            long blocksX = (width + bw - 1L) / bw;
            long blocksY = (height + bh - 1L) / bh;
            long size = blocksX * blocksY * bytesPerBlock;
            return CheckedSize(size, "block compressed mip 0");
        }

        private int GetUncompressedBytesPerPixel(string n)
        {
            if (n.Contains("RGBA32") || n.Contains("BGRA32") || n == "ARGB32")
                return 4;

            if (n == "RGB24")
                return 3;

            if (n.Contains("RGB565") || n.Contains("ARGB4444") ||
                n.Contains("RGBA4444"))
                return 2;

            if (n == "ALPHA8" || n == "R8")
                return 1;

            if (n.Contains("R16") || n.Contains("RHALF") ||
                n.Contains("RG16") || n.Contains("RGHALF"))
                return 2;

            if (n.Contains("RFloat".ToUpperInvariant()) ||
                n == "R32" || n.Contains("R32_SFLOAT"))
                return 4;

            if (n.Contains("RGBAHALF") || n.Contains("RGBAFLOAT"))
                return 8;

            return 0;
        }

        private int CheckedSize(long size, string label)
        {
            if (size <= 0 || size > int.MaxValue)
                throw new Exception("Ukuran " + label + " tidak valid: " + size);
            return (int)size;
        }

        private byte[] PrepareMip0Data(
            byte[] data,
            TextureFormat format,
            int width,
            int height,
            string source,
            out int expectedSize,
            out string note)
        {
            expectedSize = GetMip0Size(format, width, height);

            if (expectedSize <= 0)
            {
                note = "Ukuran mip 0 tidak dapat dihitung otomatis; data dipakai apa adanya.";
                return data;
            }

            if (data.Length < expectedSize)
            {
                throw new Exception(
                    "Data texture kurang untuk mip 0.\n" +
                    "Format: " + format + "\n" +
                    "Size: " + width + "x" + height + "\n" +
                    "Expected mip0: " + expectedSize + " bytes\n" +
                    "Actual: " + data.Length + " bytes\n" +
                    "Sumber: " + source);
            }

            if (data.Length == expectedSize)
            {
                note = "Data sudah tepat sebesar mip 0.";
                return data;
            }

            byte[] mip0 = new byte[expectedSize];
            Buffer.BlockCopy(data, 0, mip0, 0, expectedSize);

            note =
                "Data dipotong ke mip 0: " +
                data.Length + " → " + expectedSize + " bytes.";

            return mip0;
        }

        private bool IsAstcFormat(TextureFormat format) =>
            format.ToString().IndexOf(
                "ASTC",
                StringComparison.OrdinalIgnoreCase) >= 0;

        private void GetAstcBlockSize(
            TextureFormat format,
            out int bw,
            out int bh)
        {
            bw = bh = 0;

            string n =
                format.ToString().ToUpperInvariant();

            string[] sizes =
            {
                "4X4", "5X4", "5X5", "6X5", "6X6",
                "8X5", "8X6", "8X8", "10X5", "10X6",
                "10X8", "10X10", "12X10", "12X12"
            };

            foreach (string size in sizes)
            {
                if (!n.Contains(size))
                    continue;

                string[] parts =
                    size.Split('X');

                bw = int.Parse(parts[0]);
                bh = int.Parse(parts[1]);
                return;
            }
        }

        private int GetAstcMipSize(
            int width,
            int height,
            int bw,
            int bh)
        {
            if (bw <= 0 || bh <= 0)
                return 0;

            long blocksX =
                (width + bw - 1) / bw;

            long blocksY =
                (height + bh - 1) / bh;

            long size =
                blocksX * blocksY * 16L;

            if (size > int.MaxValue)
                throw new Exception(
                    "Ukuran ASTC terlalu besar.");

            return (int)size;
        }

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
                    FindField(child, name);

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
                value = field.Value.AsLong;
                return true;
            }
            catch
            {
            }

            try
            {
                value = (long)field.Value.AsULong;
                return true;
            }
            catch
            {
            }

            try
            {
                value = field.Value.AsInt;
                return true;
            }
            catch
            {
            }

            try
            {
                value = field.Value.AsUInt;
                return true;
            }
            catch
            {
            }

            string text =
                GetDisplayValue(field);

            if (long.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value))
            {
                return true;
            }

            return false;
        }

        private string GetTextureStreamPath(
            AssetTypeValueField baseField)
        {
            AssetTypeValueField? stream =
                FindField(baseField, "m_StreamData");

            if (stream == null)
                return "";

            return GetDisplayValue(
                FindField(stream, "path"));
        }

        private byte[]? ReadTextureStreamData(
            AssetsFileInstance assetsFile,
            AssetTypeValueField baseField,
            out string source)
        {
            source = "";

            AssetTypeValueField? stream =
                FindField(baseField, "m_StreamData");

            if (stream == null)
                return null;

            AssetTypeValueField? offsetField =
                FindField(stream, "offset");

            AssetTypeValueField? sizeField =
                FindField(stream, "size");

            AssetTypeValueField? pathField =
                FindField(stream, "path");

            if (!TryGetFieldLong(offsetField, out long offset) ||
                !TryGetFieldLong(sizeField, out long size))
            {
                return null;
            }

            string path =
                GetDisplayValue(pathField);

            if (size <= 0)
                return null;

            if (offset < 0)
                throw new Exception(
                    "m_StreamData.offset tidak valid: " + offset);

            if (size > int.MaxValue)
                throw new Exception(
                    "m_StreamData.size terlalu besar: " + size);

            // ------------------------------------------------------------
            // CASE 1: texture berada di dalam AssetBundle.
            // Contoh path:
            // archive:/CAB-xxxx/CAB-xxxx.resS
            // ------------------------------------------------------------
            if (path.StartsWith(
                "archive:/",
                StringComparison.OrdinalIgnoreCase))
            {
                BundleFileInstance? bundle =
                    assetsFile.parentBundle ?? currentBundle;

                if (bundle == null)
                {
                    throw new Exception(
                        "Texture memakai archive:/ tetapi parent bundle tidak ditemukan.");
                }

                string archivePath =
                    path.Substring("archive:/".Length)
                        .Replace('\\', '/');

                int slash =
                    archivePath.LastIndexOf('/');

                string entryName =
                    slash >= 0
                    ? archivePath.Substring(slash + 1)
                    : archivePath;

                int index =
                    bundle.file.GetFileIndex(entryName);

                // Beberapa bundle menyimpan nama entry dengan variasi
                // archive path. Kalau exact match gagal, cari berdasarkan
                // nama file terakhir / suffix.
                if (index < 0)
                {
                    for (int i = 0;
                         i < bundle.file.BlockAndDirInfo.DirectoryInfos.Count();
                         i++)
                    {
                        string dirName =
                            bundle.file.BlockAndDirInfo.DirectoryInfos[i].Name;

                        if (string.Equals(
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
                        "Entry .resS tidak ditemukan di AssetBundle.\n\n" +
                        "Path:\n" + path +
                        "\n\nEntry yang dicari:\n" + entryName);
                }

                byte[] entryData =
                    BundleHelper.LoadAssetDataFromBundle(
                        bundle.file,
                        index);

                if (entryData == null ||
                    entryData.Length == 0)
                {
                    throw new Exception(
                        "Entry .resS ditemukan tetapi datanya kosong.\n" +
                        "Entry: " + entryName);
                }

                if (offset + size > entryData.LongLength)
                {
                    throw new Exception(
                        "m_StreamData berada di luar ukuran entry .resS.\n\n" +
                        "Offset: " + offset +
                        "\nSize: " + size +
                        "\nEntry Size: " + entryData.Length);
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
                    "\noffset=" + offset +
                    ", size=" + size;

                return result;
            }

            // ------------------------------------------------------------
            // CASE 2: external .resS berada di samping .assets.
            // ------------------------------------------------------------
            string cleanPath =
                path.Replace('\\', IOPath.DirectorySeparatorChar)
                    .TrimStart(
                        IOPath.DirectorySeparatorChar,
                        IOPath.AltDirectorySeparatorChar);

            string? baseDirectory =
                IOPath.GetDirectoryName(assetsFile.path);

            if (string.IsNullOrEmpty(baseDirectory))
                baseDirectory = CacheDir?.AbsolutePath;

            if (!string.IsNullOrEmpty(baseDirectory))
            {
                string externalPath =
                    IOPath.Combine(
                        baseDirectory,
                        cleanPath);

                if (File.Exists(externalPath))
                {
                    using FileStream fs =
                        File.OpenRead(externalPath);

                    if (offset + size > fs.Length)
                    {
                        throw new Exception(
                            "m_StreamData berada di luar file .resS.\n\n" +
                            "File: " + externalPath +
                            "\nOffset: " + offset +
                            "\nSize: " + size +
                            "\nFile Size: " + fs.Length);
                    }

                    fs.Position = offset;

                    byte[] result =
                        new byte[(int)size];

                    int total = 0;
                    while (total < result.Length)
                    {
                        int read =
                            fs.Read(
                                result,
                                total,
                                result.Length - total);

                        if (read <= 0)
                            break;

                        total += read;
                    }

                    if (total != result.Length)
                        throw new Exception(
                            "Gagal membaca seluruh data .resS.\n" +
                            "Expected: " + result.Length +
                            "\nActual: " + total);

                    source =
                        "m_StreamData → external .resS\n" +
                        externalPath +
                        "\noffset=" + offset +
                        ", size=" + size;

                    return result;
                }
            }

            return null;
        }

        private string GetTextureStorageInfo(
            AssetTypeValueField baseField)
        {
            string s = "";

            try
            {
                AssetTypeValueField? stream =
                    FindField(baseField, "m_StreamData");

                s += "\n\n=== m_StreamData ===\n";

                if (stream == null)
                {
                    s += "Field tidak ditemukan.\n";
                }
                else
                {
                    s += "offset : " +
                         DisplayOrUnknown(
                             FindField(stream, "offset")) +
                         "\n";

                    s += "size   : " +
                         DisplayOrUnknown(
                             FindField(stream, "size")) +
                         "\n";

                    s += "path   : " +
                         DisplayOrUnknown(
                             FindField(stream, "path")) +
                         "\n";
                }
            }
            catch (Exception ex)
            {
                s += "\nm_StreamData error: " +
                     ex.Message +
                     "\n";
            }

            s +=
                "\nm_CompleteImageSize: " +
                DisplayOrUnknown(
                    FindField(baseField, "m_CompleteImageSize"));

            s +=
                "\n\nm_TextureData: " +
                (FindField(baseField, "m_TextureData") == null
                    ? "tidak ditemukan"
                    : "ditemukan");

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

        private string FirstBytes(
            byte[] data,
            int max)
        {
            int count =
                Math.Min(data.Length, max);

            var parts =
                new List<string>();

            for (int i = 0; i < count; i++)
            {
                parts.Add(
                    data[i].ToString(
                        "X2",
                        CultureInfo.InvariantCulture) +
                    (((i + 1) % 16 == 0)
                        ? "\n"
                        : " "));
            }

            return string.Join("", parts).Trim();
        }

        private void ShowAstcDiagnostic(
            AssetTypeValueField baseField,
            TextureFile tex,
            byte[] data,
            TextureFormat format,
            AssetsFileInstance f,
            AssetFileInfo asset,
            string dataSource)
        {
            if (assetList == null)
                return;

            GetAstcBlockSize(
                format,
                out int bw,
                out int bh);

            int bx =
                bw > 0
                ? (tex.m_Width + bw - 1) / bw
                : 0;

            int by =
                bh > 0
                ? (tex.m_Height + bh - 1) / bh
                : 0;

            int expected =
                GetAstcMipSize(
                    tex.m_Width,
                    tex.m_Height,
                    bw,
                    bh);

            ClearAssetList();

            var title =
                new TextView(this)
                {
                    Text = "=== ASTC DIAGNOSTIC ===",
                    TextSize = 20
                };

            title.SetPadding(10, 20, 10, 20);
            assetList.AddView(title);

            var info =
                new TextView(this)
                {
                    TextSize = 15
                };

            info.Text =
                "Texture Information\n\n" +
                "Width      : " + tex.m_Width + "\n" +
                "Height     : " + tex.m_Height + "\n" +
                "Format     : " + format + "\n" +
                "MipCount   : " + tex.m_MipCount + "\n\n" +
                "ASTC       : True\n" +
                "Block Width: " + bw + "\n" +
                "Block Height: " + bh + "\n\n" +
                "Blocks X   : " + bx + "\n" +
                "Blocks Y   : " + by + "\n\n" +
                "Expected Mip0: " + expected + " bytes\n" +
                "Actual Data  : " + data.Length + " bytes\n\n" +
                "DATA SOURCE:\n" + dataSource +
                "\n\nFirst Bytes:\n" +
                FirstBytes(data, 64) +
                GetTextureStorageInfo(baseField);

            info.SetPadding(10, 10, 10, 10);
            assetList.AddView(info);

            var result =
                new TextView(this)
                {
                    TextSize = 16
                };

            if (data.Length == expected)
            {
                result.Text =
                    "\n=== HASIL ANALISIS ===\n\n" +
                    "✓ DATA STREAM TEPAT\n\n" +
                    "Ukuran data sama persis dengan mip 0 ASTC.";
            }
            else if (data.Length >= expected)
            {
                result.Text =
                    "\n=== HASIL ANALISIS ===\n\n" +
                    "✓ DATA MIP PERTAMA CUKUP\n\n" +
                    "Data yang dibaca lebih besar dari mip 0. " +
                    "Decoder akan menggunakan tepat " +
                    expected + " bytes.";
            }
            else
            {
                result.Text =
                    "\n=== HASIL ANALISIS ===\n\n" +
                    "❌ DATA MIP PERTAMA KURANG\n\n" +
                    "Expected: " + expected +
                    "\nActual: " + data.Length;
            }

            result.SetPadding(10, 20, 10, 20);
            assetList.AddView(result);

            if (expected > 0 &&
                data.Length >= expected)
            {
                var decode =
                    new Button(this)
                    {
                        Text = "▶ DECODE ASTC DARI .resS"
                    };

                decode.Click += delegate
                {
                    TryDecodeAstc(
                        data,
                        expected,
                        tex.m_Width,
                        tex.m_Height,
                        format,
                        f,
                        asset);
                };

                assetList.AddView(decode);
            }

            var back =
                new Button(this)
                {
                    Text = "← KEMBALI KE INSPECTOR"
                };

            back.Click += delegate
            {
                OpenAssetInspector(f, asset);
            };

            assetList.AddView(back);

            SetStatus(
                "ASTC Diagnostic selesai.\n" +
                format +
                " / " +
                tex.m_Width +
                "x" +
                tex.m_Height +
                "\nSumber: " +
                dataSource);
        }

        private void TryDecodeAstc(
            byte[] data,
            int expected,
            int width,
            int height,
            TextureFormat format,
            AssetsFileInstance f,
            AssetFileInfo asset)
        {
            try
            {
                if (data.Length < expected)
                {
                    throw new Exception(
                        "Data ASTC kurang.\n" +
                        "Expected: " + expected +
                        "\nActual: " + data.Length);
                }

                SetStatus(
                    "Mencoba decode ASTC mip 0...\n" +
                    "Menggunakan tepat " +
                    expected +
                    " bytes dari data .resS.");

                byte[] mip0 =
                    new byte[expected];

                Buffer.BlockCopy(
                    data,
                    0,
                    mip0,
                    0,
                    expected);

                byte[] bgra =
                    TextureFile.DecodeManaged(
                        mip0,
                        format,
                        width,
                        height,
                        true);

                if (bgra == null ||
                    bgra.Length < width * height * 4)
                {
                    throw new Exception(
                        "DecodeManaged() menghasilkan data tidak valid.\n" +
                        "Expected RGBA/BGRA: " +
                        (width * height * 4) +
                        " bytes\nActual: " +
                        (bgra == null ? 0 : bgra.Length));
                }

                Bitmap bitmap =
                    CreateBitmapFromBgra(
                        bgra,
                        width,
                        height);

                ShowTexturePreview(
                    bitmap,
                    width,
                    height,
                    format,
                    mip0.Length,
                    bgra.Length,
                    f,
                    asset);

                SetStatus(
                    "✓ ASTC mip 0 berhasil didecode.\n" +
                    "Input .resS: " +
                    mip0.Length +
                    " bytes\nOutput: " +
                    bgra.Length +
                    " bytes");
            }
            catch (Exception ex)
            {
                ShowTextureMessage(
                    "ASTC DECODE ERROR",
                    ex.ToString());
            }
        }

        private void ShowTextureMessage(string title, string message)
        {
            ClearAssetList(); if (assetList == null) return;
            var h = new TextView(this) { Text = "=== " + title + " ===", TextSize = 20 }; assetList.AddView(h);
            var t = new TextView(this) { Text = message, TextSize = 16 }; assetList.AddView(t);
            var b = new Button(this) { Text = "← KEMBALI KE INSPECTOR" }; b.Click += delegate { if (currentAssetsFile != null && currentAsset != null) OpenAssetInspector(currentAssetsFile, currentAsset); }; assetList.AddView(b);
        }

        private void ShowTextureError(AssetFileInfo asset, Exception ex)
        {
            ClearAssetList(); if (assetList == null) return;
            var title = new TextView(this) { Text = "❌ TEXTURE VIEWER GAGAL", TextSize = 20 }; assetList.AddView(title);
            var error = new TextView(this) { Text = "Asset Texture2D\n\nTypeID: " + asset.TypeId + "\nPathID: " + asset.PathId + "\n\nERROR:\n" + ex.Message + "\n\nDETAIL:\n" + ex, TextSize = 15 }; assetList.AddView(error);
            var back = new Button(this) { Text = "← KEMBALI KE INSPECTOR" }; back.Click += delegate { if (currentAssetsFile != null && currentAsset != null) OpenAssetInspector(currentAssetsFile, currentAsset); }; assetList.AddView(back);
            SetStatus("Texture Viewer gagal. Detail error ditampilkan di layar.");
        }

        private Bitmap CreateBitmapFromBgra(byte[] bgra, int width, int height)
        {
            if (bgra.Length < width * height * 4) throw new Exception("Ukuran data BGRA tidak sesuai ukuran texture.");
            int[] pixels = new int[width * height]; int p = 0;
            for (int i = 0; i < pixels.Length; i++) { byte b = bgra[p++], g = bgra[p++], r = bgra[p++], a = bgra[p++]; pixels[i] = (a << 24) | (r << 16) | (g << 8) | b; }
            Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!);
            bitmap.SetPixels(pixels, 0, width, 0, 0, width, height); return bitmap;
        }

        private void ShowTexturePreview(Bitmap bitmap, int width, int height, TextureFormat format, int encodedSize, int decodedSize, AssetsFileInstance f, AssetFileInfo asset)
        {
            ClearAssetList(); if (assetList == null) return;
            var back = new Button(this) { Text = "← KEMBALI KE INSPECTOR" }; back.Click += delegate { OpenAssetInspector(f, asset); }; assetList.AddView(back);
            var info = new TextView(this) { TextSize = 16 };
            info.Text = "=== TEXTURE VIEWER ===\n\nSize: " + width + " x " + height + "\nTexture Format: " + format + "\nEncoded: " + encodedSize + " bytes\nDecoded: " + decodedSize + " bytes\nPreview: BGRA → Android Bitmap"; assetList.AddView(info);
            var image = new ImageView(this); image.SetImageBitmap(bitmap); image.SetAdjustViewBounds(true); image.SetScaleType(ImageView.ScaleType.FitCenter); var ip = new LinearLayout.LayoutParams(-1, -2); ip.SetMargins(0, 20, 0, 20); assetList.AddView(image, ip);
            var export = new Button(this) { Text = "📤 EXPORT PNG" }; export.Click += delegate { StartTextureExport(); }; assetList.AddView(export);
        }

        private void StartTextureExport()
        {
            try
            {
                if (currentAssetsFile == null || currentAsset == null) throw new Exception("Texture aktif tidak ditemukan.");
                var intent = new Intent(Intent.ActionCreateDocument); intent.AddCategory(Intent.CategoryOpenable); intent.SetType("image/png"); intent.PutExtra(Intent.ExtraTitle, "texture.png"); StartActivityForResult(intent, ExportTextureRequestCode);
            }
            catch (Exception ex) { SetStatus("Export gagal dimulai.\n\n" + ex.Message); }
        }

        private void ExportCurrentTexture(AndroidUri outputUri)
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
                TextureFile.ReadTextureFile(baseField);

            if (tex == null)
                throw new Exception(
                    "TextureFile gagal dibaca.");

            int width = tex.m_Width;
            int height = tex.m_Height;
            TextureFormat format =
                (TextureFormat)tex.m_TextureFormat;

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
                    "Sumber: " + source);
            }

            int exportExpectedMip0;
            string exportMipNote;
            data = PrepareMip0Data(
                data,
                format,
                width,
                height,
                source,
                out exportExpectedMip0,
                out exportMipNote);

            byte[] bgra =
                TextureFile.DecodeManaged(
                    data,
                    format,
                    width,
                    height,
                    true);

            if (bgra == null ||
                bgra.Length < width * height * 4)
            {
                throw new Exception(
                    "DecodeManaged() gagal.\n" +
                    "Format: " + format +
                    "\nSource: " + source);
            }

            Bitmap bitmap =
                CreateBitmapFromBgra(
                    bgra,
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
                "✓ EXPORT BERHASIL\n" +
                width + "x" + height +
                "\nFormat: " + format +
                "\nSumber: " + source +
                "\nMip0: " + exportExpectedMip0 + " bytes" +
                "\n" + exportMipNote);
        }

        // ============================================================
        // FIELD TREE / EDITOR
        // ============================================================

        private void AddFieldTree(AssetTypeValueField field, int depth)
        {
            if (field == null || assetList == null) return;
            int count = GetChildCount(field); string name = SafeFieldName(field), value = GetDisplayValue(field), indent = MakeIndent(depth);
            var container = new LinearLayout(this) { Orientation = Orientation.Vertical }; var text = new TextView(this) { TextSize = 15 };
            if (count > 0)
            {
                text.Text = indent + "▼ " + name + " [" + count + "]"; container.AddView(text); var children = new LinearLayout(this) { Orientation = Orientation.Vertical }; children.Visibility = AndroidViewStates.Visible;
                for (int i = 0; i < count; i++) { var child = GetChild(field, i); if (child != null) AddFieldToLayoutExpanded(children, child, depth + 1); }
                container.AddView(children); text.Click += delegate { children.Visibility = children.Visibility == AndroidViewStates.Gone ? AndroidViewStates.Visible : AndroidViewStates.Gone; text.Text = indent + (children.Visibility == AndroidViewStates.Visible ? "▼ " : "▶ ") + name + " [" + count + "]"; };
            }
            else
            {
                text.Text = string.IsNullOrEmpty(value) ? indent + "• " + name : indent + "• " + name + " = " + value; container.AddView(text); text.Click += delegate { ShowFieldEditor(field); };
            }
            assetList.AddView(container);
        }

        private void AddFieldToLayoutExpanded(LinearLayout parent, AssetTypeValueField field, int depth)
        {
            if (field == null) return; int count = GetChildCount(field); string name = SafeFieldName(field), value = GetDisplayValue(field), indent = MakeIndent(depth); var text = new TextView(this) { TextSize = 15 };
            if (count > 0)
            {
                text.Text = indent + "▼ " + name + " [" + count + "]"; parent.AddView(text); var children = new LinearLayout(this) { Orientation = Orientation.Vertical }; children.Visibility = AndroidViewStates.Visible;
                for (int i = 0; i < count; i++) { var child = GetChild(field, i); if (child != null) AddFieldToLayoutExpanded(children, child, depth + 1); }
                parent.AddView(children); text.Click += delegate { children.Visibility = children.Visibility == AndroidViewStates.Gone ? AndroidViewStates.Visible : AndroidViewStates.Gone; text.Text = indent + (children.Visibility == AndroidViewStates.Visible ? "▼ " : "▶ ") + name + " [" + count + "]"; };
            }
            else
            {
                text.Text = string.IsNullOrEmpty(value) ? indent + "• " + name : indent + "• " + name + " = " + value; parent.AddView(text); text.Click += delegate { ShowFieldEditor(field); };
            }
        }

        private int GetChildCount(AssetTypeValueField field) { try { return field.Children == null ? 0 : field.Children.Count; } catch { return 0; } }
        private AssetTypeValueField? GetChild(AssetTypeValueField field, int index) { try { if (field.Children == null || index < 0 || index >= field.Children.Count) return null; return field.Children[index]; } catch { return null; } }
        private string SafeFieldName(AssetTypeValueField field) { try { if (!string.IsNullOrEmpty(field.FieldName)) return field.FieldName; } catch { } return "(unnamed)"; }

        private string GetDisplayValue(AssetTypeValueField field)
        {
            try { if (field.Value == null) return ""; } catch { return ""; }
            try { string v = field.Value.AsString; if (!string.IsNullOrEmpty(v)) return v; } catch { }
            try { return field.Value.AsBool.ToString(); } catch { }
            try { return field.Value.AsInt.ToString(CultureInfo.InvariantCulture); } catch { }
            try { return field.Value.AsUInt.ToString(CultureInfo.InvariantCulture); } catch { }
            try { return field.Value.AsLong.ToString(CultureInfo.InvariantCulture); } catch { }
            try { return field.Value.AsULong.ToString(CultureInfo.InvariantCulture); } catch { }
            try { return field.Value.AsFloat.ToString(CultureInfo.InvariantCulture); } catch { }
            try { return field.Value.AsDouble.ToString(CultureInfo.InvariantCulture); } catch { }
            return "";
        }

        private void ShowFieldEditor(AssetTypeValueField field)
        {
            string name = SafeFieldName(field), current = GetDisplayValue(field); var builder = new AlertDialog.Builder(this); builder.SetTitle("EDIT FIELD"); var layout = new LinearLayout(this) { Orientation = Orientation.Vertical }; layout.SetPadding(40, 20, 40, 20);
            var info = new TextView(this) { Text = "Field:\n" + name + "\n\nNilai sekarang:\n" + current + "\n\nNilai baru:", TextSize = 16 }; layout.AddView(info); var input = new EditText(this) { Text = current }; input.SetSingleLine(true); layout.AddView(input); builder.SetView(layout);
            builder.SetNegativeButton("CANCEL", (s, e) => { }); builder.SetPositiveButton("APPLY", (s, e) => { try { ApplyFieldValue(field, input.Text ?? ""); if (currentAsset != null) modifiedAssets.Add(currentAsset.PathId); SetStatus("✓ Field berhasil diubah:\n" + name + "\nAsset ditandai MODIFIED."); if (currentAssetsFile != null && currentAsset != null) OpenAssetInspector(currentAssetsFile, currentAsset); } catch (Exception ex) { SetStatus("Gagal mengubah field:\n\n" + ex.Message); } }); builder.Show();
        }

        private void ApplyFieldValue(AssetTypeValueField field, string value)
        {
            try { field.Value.AsString = value; return; } catch { }
            if (bool.TryParse(value, out bool b)) try { field.Value.AsBool = b; return; } catch { }
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) try { field.Value.AsInt = i; return; } catch { }
            if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint ui)) try { field.Value.AsUInt = ui; return; } catch { }
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) try { field.Value.AsLong = l; return; } catch { }
            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ul)) try { field.Value.AsULong = ul; return; } catch { }
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fl)) try { field.Value.AsFloat = fl; return; } catch { }
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) try { field.Value.AsDouble = d; return; } catch { }
            throw new Exception("Tipe field tidak dapat diubah.");
        }

        // ============================================================
        // NAVIGATION / UI
        // ============================================================

        private string MakeIndent(int depth) => depth <= 0 ? "" : new string(' ', depth * 4);
        private void ReturnToAssetList() { if (currentAssetsFile == null) return; showingInspector = false; showingAssetList = true; ShowAssetList(currentAssetsFile); }
        private void ReturnToSerializedFiles() { if (currentBundle == null) return; showingInspector = false; showingAssetList = false; showingSerializedFiles = true; ShowSerializedFiles(0, "AssetBundle"); }
        public override void OnBackPressed() { if (showingInspector) { ReturnToAssetList(); return; } if (showingAssetList) { ReturnToSerializedFiles(); return; } if (showingSerializedFiles) { showingSerializedFiles = false; ClearAssetList(); SetStatus("Tekan OPEN UNITY3D."); return; } base.OnBackPressed(); }
        private void ClearAssetList() { assetList?.RemoveAllViews(); }
        private void SetStatus(string message) { RunOnUiThread(delegate { if (status != null) status.Text = message; }); }
    }
}
