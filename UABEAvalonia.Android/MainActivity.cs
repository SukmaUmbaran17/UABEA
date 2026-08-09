using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;
using System;
using System.IO;

namespace UABEAvalonia.Android;

[Activity(
    Label = "UABEA Android",
    MainLauncher = true
)]
public class MainActivity : Activity
{
    private const int PickFileRequestCode = 1001;

    private TextView? status;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // =====================================================
        // Layout utama
        // =====================================================

        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        layout.SetPadding(40, 40, 40, 40);


        // =====================================================
        // Judul
        // =====================================================

        var title = new TextView(this)
        {
            Text = "UABEA Android"
        };

        title.TextSize = 24;


        // =====================================================
        // Tombol Open Asset
        // =====================================================

        var button = new Button(this)
        {
            Text = "Open Asset"
        };


        // =====================================================
        // Status
        // =====================================================

        status = new TextView(this)
        {
            Text = "Belum ada file dipilih."
        };


        // =====================================================
        // Event tombol
        // =====================================================

        button.Click += (sender, e) =>
        {
            OpenFilePicker();
        };


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
    // Membuka Android File Picker
    // =========================================================

    private void OpenFilePicker()
    {
        try
        {
            Intent intent = new Intent(Intent.ActionOpenDocument);

            intent.AddCategory(Intent.CategoryOpenable);

            // Untuk sementara semua file diperbolehkan.
            // Nanti bisa kita batasi ke file Unity.
            intent.SetType("*/*");

            StartActivityForResult(
                intent,
                PickFileRequestCode
            );
        }
        catch (Exception ex)
        {
            if (status != null)
            {
                status.Text =
                    "❌ Gagal membuka File Picker\n\n" +
                    ex.Message;
            }
        }
    }


    // =========================================================
    // Hasil File Picker
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


        // Pastikan ini hasil dari File Picker
        if (requestCode != PickFileRequestCode)
            return;


        // User membatalkan pemilihan file
        if (resultCode != Result.Ok)
        {
            if (status != null)
            {
                status.Text =
                    "Pemilihan file dibatalkan.";
            }

            return;
        }


        // Tidak ada URI
        if (data?.Data == null)
        {
            if (status != null)
            {
                status.Text =
                    "❌ File tidak ditemukan.";
            }

            return;
        }


        try
        {
            // =================================================
            // Ambil URI file
            // =================================================

            var uri = data.Data;


            // =================================================
            // Ambil nama file
            // =================================================

            string fileName = "temp.assets";

            using (var cursor = ContentResolver.Query(
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
                        nameIndex >= 0
                    )
                    {
                        string? detectedName =
                            cursor.GetString(nameIndex);

                        if (!string.IsNullOrWhiteSpace(
                            detectedName))
                        {
                            fileName = detectedName;
                        }
                    }
                }
            }


            // =================================================
            // Bersihkan nama file
            // =================================================

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


            // =================================================
            // Buat path cache Android
            // =================================================

            string cacheDirectory =
                CacheDir?.AbsolutePath
                ?? throw new Exception(
                    "Cache directory Android tidak tersedia."
                );


            string cachePath =
                Path.Combine(
                    cacheDirectory,
                    fileName
                );


            // =================================================
            // Salin file dari URI ke cache
            // =================================================

            using (
                var input =
                    ContentResolver.OpenInputStream(uri))
            {
                if (input == null)
                {
                    throw new Exception(
                        "Tidak dapat membaca file yang dipilih."
                    );
                }

                using (
                    var output =
                        File.Create(cachePath))
                {
                    input.CopyTo(output);
                }
            }


            // =================================================
            // Cek ukuran file
            // =================================================

            long fileSize =
                new FileInfo(cachePath).Length;


            // =================================================
            // Tampilkan hasil
            // =================================================

            if (status != null)
            {
                status.Text =
                    "✅ File berhasil dibuka!\n\n" +
                    $"Nama : {fileName}\n" +
                    $"Ukuran : {fileSize:N0} bytes\n\n" +
                    $"Lokasi cache:\n{cachePath}";
            }
        }
        catch (Exception ex)
        {
            if (status != null)
            {
                status.Text =
                    "❌ Gagal membuka file\n\n" +
                    ex.Message;
            }
        }
    }
}
