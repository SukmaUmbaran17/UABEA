using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;

using AssetsTools.NET.Extra;

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace UABEAvalonia.Android;

[Activity(
    Label = "UABEA Android",
    MainLauncher = true
)]
public class MainActivity : Activity
{
    private const int PickFileRequestCode = 1001;

    private TextView? status;

    private AssetsManager? assetsManager;


    // =========================================================
    // ON CREATE
    // =========================================================

    protected override void OnCreate(
        Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);


        // =====================================================
        // Inisialisasi AssetsTools.NET
        // =====================================================

        try
        {
            assetsManager = new AssetsManager();
        }
        catch (Exception ex)
        {
            assetsManager = null;

            System.Diagnostics.Debug.WriteLine(
                "AssetsTools.NET error: " +
                ex.ToString()
            );
        }


        // =====================================================
        // Layout utama
        // =====================================================

        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        layout.SetPadding(
            40,
            40,
            40,
            40
        );


        // =====================================================
        // Judul
        // =====================================================

        var title = new TextView(this)
        {
            Text = "UABEA Android"
        };

        title.TextSize = 24;


        // =====================================================
        // Tombol Open Unity3D
        // =====================================================

        var button = new Button(this)
        {
            Text = "OPEN UNITY3D"
        };

        button.Click += (sender, e) =>
        {
            OpenFilePicker();
        };


        // =====================================================
        // Status
        // =====================================================

        status = new TextView(this)
        {
            Text = assetsManager != null
                ? "✅ AssetsTools.NET berhasil dimuat.\n\nPilih file .unity3d."
                : "❌ AssetsTools.NET gagal dimuat."
        };

        status.TextSize = 16;


        // =====================================================
        // Masukkan komponen ke layout
        // =====================================================

        layout.AddView(title);
        layout.AddView(button);
        layout.AddView(status);


        // =====================================================
        // Tampilkan layout
        // =====================================================

        SetContentView(layout);
    }


    // =========================================================
    // FILE PICKER
    // =========================================================

    private void OpenFilePicker()
    {
        try
        {
            Intent intent = new Intent(
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
            ShowStatus(
                "❌ Gagal membuka File Picker\n\n" +
                ex.Message
            );
        }
    }


    // =========================================================
    // HASIL FILE PICKER
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


        // Bukan request kita
        if (requestCode != PickFileRequestCode)
            return;


        // User membatalkan
        if (resultCode != Result.Ok)
        {
            ShowStatus(
                "Pemilihan file dibatalkan."
            );

            return;
        }


        // Tidak ada URI
        var uri = data?.Data;

        if (uri == null)
        {
            ShowStatus(
                "❌ File tidak ditemukan."
            );

            return;
        }


        try
        {
            ProcessSelectedFile(uri);
        }
        catch (Exception ex)
        {
            ShowStatus(
                "❌ Gagal memproses file\n\n" +
                ex.Message
            );

            System.Diagnostics.Debug.WriteLine(
                ex.ToString()
            );
        }
    }


    // =========================================================
    // PROSES FILE YANG DIPILIH
    // =========================================================

    private void ProcessSelectedFile(
        global::Android.Net.Uri uri)
    {
        // =====================================================
        // Pastikan AssetsManager tersedia
        // =====================================================

        AssetsManager manager =
            assetsManager
            ?? throw new Exception(
                "AssetsManager belum tersedia."
            );


        // =====================================================
        // Ambil nama file
        // =====================================================

        string fileName = "temp.unity3d";


        using (
            var cursor = ContentResolver.Query(
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


                    if (!string.IsNullOrWhiteSpace(
                        detectedName))
                    {
                        fileName = detectedName;
                    }
                }
            }
        }


        // =====================================================
        // Bersihkan nama file
        // =====================================================

        foreach (
            char invalidChar
            in Path.GetInvalidFileNameChars())
        {
            fileName =
                fileName.Replace(
                    invalidChar
