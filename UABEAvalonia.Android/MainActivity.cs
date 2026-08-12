using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;

using AssetsTools.NET;
using AssetsTools.NET.Extra;

using System;
using System.IO;
using System.Linq;
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
        // Inisialisasi AssetsManager
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
        // Tombol Open Unity3D
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
                "Pilih file Unity3D.";
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
        // Layout
        // =====================================================

        layout.AddView(title);

        layout.AddView(button);


        LinearLayout.LayoutParams scrollParams =
            new LinearLayout.LayoutParams(
                -1,
                0
            );

        scrollParams.Weight =
            1;


        layout.AddView(
            scroll,
            scrollParams
        );


        // =====================================================
        // Tampilkan
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
            Intent intent =
                new Intent(
                    Intent
