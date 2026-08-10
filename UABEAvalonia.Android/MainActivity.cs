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

namespace UABEAvalonia.Android;

[Activity(
    Label = "UABEA Android",
    MainLauncher = true
)]
public class MainActivity : Activity
{
    private const int PickFileRequestCode = 1001;

    private TextView? status;

    // =========================================================
    // AssetsTools.NET
    // =========================================================

    private AssetsManager? assetsManager;


    // =========================================================
    // ON CREATE
    // =========================================================

    protected override void OnCreate(
        Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);


        // =====================================================
        // Inisialisasi AssetsManager
        // =====================================================

        try
        {
            assetsManager = new AssetsManager();
        }
        catch (Exception ex)
        {
            Toast.MakeText(
                this,
                "Gagal memuat AssetsTools.NET",
                ToastLength.Long
            )?.Show();

            System.Diagnostics.Debug.WriteLine(
                "AssetsTools.NET error: " + ex
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
        // Masukkan komponen
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

            // Untuk sementara semua file.
            // Nanti kita bisa batasi ke Unity.
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


        // URI tidak tersedia
        if (data?.Data == null)
        {
            ShowStatus(
                "❌ File tidak ditemukan."
            );

            return;
        }


        try
        {
            ProcessSelected
