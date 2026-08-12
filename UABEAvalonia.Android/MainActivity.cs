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

using AndroidUri = global::Android.Net.Uri;

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
                "AssetsTools.NET error:"
            );

            System.Diagnostics.Debug.WriteLine(
                ex.ToString()
            );
        }


        // =====================================================
        // Layout utama
        // =====================================================

        LinearLayout layout =
            new LinearLayout(this);

        layout.Orientation =
            Orientation.Vertical;

        layout.SetPadding(
            24,
            24,
            24,
            24
        );


        // =====================================================
        // Judul
        // =====================================================

        TextView title =
            new TextView(this);

        title.Text =
            "UABEA Android";

        title.TextSize =
            24;


        // =====================================================
        // Tombol OPEN UNITY3D
        // =====================================================

        Button button =
            new Button(this);

        button.Text =
            "OPEN UNITY3D";


        button.Click += delegate
        {
            OpenFilePicker();
        };


        // =====================================================
        // Status
        // =====================================================

        status =
            new TextView(this);

        status.TextSize =
            18;


        if (assetsManager != null)
        {
            status.Text =
                "AssetsTools.NET berhasil dimuat.\n\n" +
                "Silakan pilih file Unity3D.";
        }
        else
        {
            status.Text =
                "❌ AssetsTools.NET gagal dimuat.";
        }


        // =====================================================
        // ScrollView
        // =====================================================

        ScrollView scroll =
            new ScrollView(this);

        scroll.AddView(status);


        // =====================================================
        // Masukkan ke layout
        // =====================================================

        layout.AddView(title);

        layout.AddView(button);

        layout.AddView(
            scroll,
            new LinearLayout.LayoutParams(
                -1,
                0,
                1
            )
        );


        // =====================================================
        // Tampilkan
        // =====================================================

        SetContentView(layout);
    }


    // =========================================================
    // OPEN FILE PICKER
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
            ShowError(
                "Gagal membuka File Picker",
                ex
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
        {
            return;
        }


        // User membatalkan
        if (resultCode != Result.Ok)
        {
            SetStatus(
                "Pemilihan file dibatalkan."
            );

            return;
        }


        // Data kosong
        if (data == null)
        {
            SetStatus(
                "❌ Data file tidak ditemukan."
            );

            return;
        }


        // =====================================================
        // URI
        // =====================================================

        AndroidUri? uri =
            data.Data;


        if (
