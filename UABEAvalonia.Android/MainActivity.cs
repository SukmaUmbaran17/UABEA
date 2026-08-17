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
                AssetTypeValueField baseField = assetsManager.GetBaseField(f, asset);
                if (baseField == null) throw new Exception("GetBaseField() mengembalikan NULL.");
                TextureFile tex = TextureFile.ReadTextureFile(baseField);
                if (tex == null) throw new Exception("TextureFile gagal dibaca.");
                int width = tex.m_Width, height = tex.m_Height;
                TextureFormat format = (TextureFormat)tex.m_TextureFormat;
                if (width <= 0 || height <= 0) throw new Exception("Ukuran texture tidak valid: " + width + "x" + height);
                byte[] data = tex.GetTextureData(f);
                if (data == null || data.Length == 0) throw new Exception("GetTextureData() menghasilkan data kosong.");

                if (IsAstcFormat(format))
                {
                    ShowAstcDiagnostic(baseField, tex, data, format, f, asset);
                    return;
                }

                byte[] bgra = TextureFile.DecodeManaged(data, format, width, height, true);
                if (bgra == null || bgra.Length < width * height * 4) throw new Exception("DecodeManaged() menghasilkan data tidak valid.");
                Bitmap bitmap = CreateBitmapFromBgra(bgra, width, height);
                ShowTexturePreview(bitmap, width, height, format, data.Length, bgra.Length, f, asset);
                SetStatus("✓ Texture berhasil didecode.\n" + width + "x" + height + " / " + format);
            }
            catch (Exception ex) { ShowTextureError(asset, ex); }
        }

        private bool IsAstcFormat(TextureFormat format) => format.ToString().IndexOf("ASTC", StringComparison.OrdinalIgnoreCase) >= 0;

        private void GetAstcBlockSize(TextureFormat format, out int bw, out int bh)
        {
            bw = bh = 0; string n = format.ToString().ToUpperInvariant();
            string[] sizes = { "4X4", "5X4", "5X5", "6X5", "6X6", "8X5", "8X6", "8X8", "10X5", "10X6", "10X8", "10X10", "12X10", "12X12" };
            foreach (string s in sizes) if (n.Contains(s)) { string[] p = s.Split('X'); bw = int.Parse(p[0]); bh = int.Parse(p[1]); return; }
        }

        private int GetAstcMipSize(int width, int height, int bw, int bh)
        {
            if (bw <= 0 || bh <= 0) return 0;
            long bx = (width + bw - 1) / bw, by = (height + bh - 1) / bh;
            long size = bx * by * 16L; if (size > int.MaxValue) throw new Exception("Ukuran ASTC terlalu besar."); return (int)size;
        }

        private AssetTypeValueField? FindField(AssetTypeValueField root, string name)
        {
            if (root == null) return null;
            if (string.Equals(SafeFieldName(root), name, StringComparison.Ordinal)) return root;
            for (int i = 0; i < GetChildCount(root); i++) { var c = GetChild(root, i); if (c != null) { var r = FindField(c, name); if (r != null) return r; } }
            return null;
        }

        private string GetTextureStorageInfo(AssetTypeValueField baseField)
        {
            string s = "";
            try
            {
                var stream = FindField(baseField, "m_StreamData");
                s += "\n\n=== m_StreamData ===\n";
                if (stream == null) s += "Field tidak ditemukan.\n";
                else
                {
                    s += "offset : " + DisplayOrUnknown(FindField(stream, "offset")) + "\n";
                    s += "size   : " + DisplayOrUnknown(FindField(stream, "size")) + "\n";
                    s += "path   : " + DisplayOrUnknown(FindField(stream, "path")) + "\n";
                }
            }
            catch (Exception ex) { s += "\nm_StreamData error: " + ex.Message + "\n"; }
            s += "\nm_CompleteImageSize: " + DisplayOrUnknown(FindField(baseField, "m_CompleteImageSize"));
            s += "\n\nm_TextureData: " + (FindField(baseField, "m_TextureData") == null ? "tidak ditemukan" : "ditemukan");
            return s;
        }

        private string DisplayOrUnknown(AssetTypeValueField? f)
        {
            if (f == null) return "(tidak ditemukan)";
            string v = GetDisplayValue(f); return string.IsNullOrEmpty(v) ? "(kosong)" : v;
        }

        private string FirstBytes(byte[] data, int max)
        {
            int count = Math.Min(data.Length, max); var parts = new List<string>();
            for (int i = 0; i < count; i++) parts.Add(data[i].ToString("X2", CultureInfo.InvariantCulture) + (((i + 1) % 16 == 0) ? "\n" : " "));
            return string.Join("", parts).Trim();
        }

        private void ShowAstcDiagnostic(AssetTypeValueField baseField, TextureFile tex, byte[] data, TextureFormat format, AssetsFileInstance f, AssetFileInfo asset)
        {
            if (assetList == null) return;
            GetAstcBlockSize(format, out int bw, out int bh);
            int bx = bw > 0 ? (tex.m_Width + bw - 1) / bw : 0;
            int by = bh > 0 ? (tex.m_Height + bh - 1) / bh : 0;
            int expected = GetAstcMipSize(tex.m_Width, tex.m_Height, bw, bh);
            ClearAssetList();
            var title = new TextView(this) { Text = "=== ASTC DIAGNOSTIC ===", TextSize = 20 }; title.SetPadding(10, 20, 10, 20); assetList.AddView(title);
            var info = new TextView(this) { TextSize = 15 };
            info.Text = "Texture Information\n\n" +
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
                        "First Bytes:\n" + FirstBytes(data, 64) +
                        GetTextureStorageInfo(baseField);
            info.SetPadding(10, 10, 10, 10); assetList.AddView(info);
            var result = new TextView(this) { TextSize = 16 };
            result.Text = "\n=== HASIL ANALISIS ===\n\n" + (data.Length >= expected ? "✓ DATA MIP PERTAMA CUKUP\n\nData texture memiliki setidaknya ukuran yang dibutuhkan untuk mip pertama." : "❌ DATA MIP PERTAMA KURANG");
            result.SetPadding(10, 20, 10, 20); assetList.AddView(result);
            if (expected > 0 && data.Length >= expected)
            {
                var decode = new Button(this) { Text = "▶ COBA DECODE ASTC" };
                decode.Click += delegate { TryDecodeAstc(data, expected, tex.m_Width, tex.m_Height, format, f, asset); }; assetList.AddView(decode);
            }
            var back = new Button(this) { Text = "← KEMBALI KE INSPECTOR" }; back.Click += delegate { OpenAssetInspector(f, asset); }; assetList.AddView(back);
            SetStatus("ASTC Diagnostic selesai.\n" + format + " / " + tex.m_Width + "x" + tex.m_Height);
        }

        private void TryDecodeAstc(byte[] data, int expected, int width, int height, TextureFormat format, AssetsFileInstance f, AssetFileInfo asset)
        {
            try
            {
                SetStatus("Mencoba decode ASTC mip 0...\nMenggunakan " + expected + " bytes pertama dari " + data.Length + " bytes.");
                byte[] mip0 = data.Take(expected).ToArray();
                byte[] bgra = TextureFile.DecodeManaged(mip0, format, width, height, true);
                if (bgra == null || bgra.Length < width * height * 4) throw new Exception("DecodeManaged() menghasilkan data tidak valid. Actual: " + (bgra == null ? 0 : bgra.Length));
                Bitmap bitmap = CreateBitmapFromBgra(bgra, width, height);
                ShowTexturePreview(bitmap, width, height, format, mip0.Length, bgra.Length, f, asset);
                SetStatus("✓ ASTC mip 0 berhasil didecode.\nInput: " + mip0.Length + " bytes\nOutput: " + bgra.Length + " bytes");
            }
            catch (Exception ex) { ShowTextureMessage("ASTC DECODE ERROR", ex.ToString()); }
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
            if (assetsManager == null || currentAssetsFile == null || currentAsset == null) throw new Exception("Texture aktif tidak ditemukan.");
            var baseField = assetsManager.GetBaseField(currentAssetsFile, currentAsset); if (baseField == null) throw new Exception("GetBaseField() gagal.");
            var tex = TextureFile.ReadTextureFile(baseField); if (tex == null) throw new Exception("TextureFile gagal dibaca.");
            int width = tex.m_Width, height = tex.m_Height; TextureFormat format = (TextureFormat)tex.m_TextureFormat;
            byte[] data = tex.GetTextureData(currentAssetsFile); if (data == null || data.Length == 0) throw new Exception("GetTextureData() menghasilkan data kosong.");
            if (IsAstcFormat(format)) { GetAstcBlockSize(format, out int bw, out int bh); int expected = GetAstcMipSize(width, height, bw, bh); if (expected > 0 && data.Length >= expected) data = data.Take(expected).ToArray(); }
            byte[] bgra = TextureFile.DecodeManaged(data, format, width, height, true); if (bgra == null || bgra.Length < width * height * 4) throw new Exception("DecodeManaged() gagal. Format: " + format);
            Bitmap bitmap = CreateBitmapFromBgra(bgra, width, height);
            using Stream? output = ContentResolver.OpenOutputStream(outputUri); if (output == null) throw new Exception("Tidak dapat membuka output.");
            bool ok = bitmap.Compress(Bitmap.CompressFormat.Png, 100, output); output.Flush(); bitmap.Recycle(); if (!ok) throw new Exception("Bitmap gagal diexport ke PNG.");
            SetStatus("✓ EXPORT BERHASIL\n" + width + "x" + height + "\nFormat: " + format);
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
