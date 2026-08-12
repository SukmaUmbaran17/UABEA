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
        private LinearLayout? assetListLayout;

        private AssetsManager? assetsManager;

        private AssetsFileInstance? currentAssetsFile;
        private BundleFileInstance? currentBundle;


        // =====================================================
        // ON CREATE
        // =====================================================

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                assetsManager = new AssetsManager();
            }
            catch (Exception ex)
            {
                assetsManager = null;

                System.Diagnostics.Debug.WriteLine(
                    ex.ToString()
                );
            }


            // =================================================
            // ROOT LAYOUT
            // =================================================

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


            // =================================================
            // TITLE
            // =================================================

            TextView title =
                new TextView(this);

            title.Text =
                "UABEA Android";

            title.TextSize =
                24;


            // =================================================
            // OPEN BUTTON
            // =================================================

            Button openButton =
                new Button(this);

            openButton.Text =
                "OPEN UNITY3D";

            openButton.Click +=
                delegate
                {
                    OpenFilePicker();
                };


            // =================================================
            // STATUS
            // =================================================

            status =
                new TextView(this);

            status.TextSize =
                16;

            if (assetsManager != null)
            {
                status.Text =
                    "✅ AssetsTools.NET siap.\n\n" +
                    "Tekan OPEN UNITY3D.";
            }
            else
            {
                status.Text =
                    "❌ AssetsTools.NET gagal dimuat.";
            }


            // =================================================
            // ASSET LIST CONTAINER
            // =================================================

            assetListLayout =
                new LinearLayout(this);

            assetListLayout.Orientation =
                Orientation.Vertical;


            ScrollView scroll =
                new ScrollView(this);

            scroll.AddView(
                assetListLayout
            );


            // =================================================
            // ADD VIEW
            // =================================================

            layout.AddView(title);

            layout.AddView(openButton);

            layout.AddView(status);


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


            SetContentView(layout);
        }


        // =====================================================
        // UNITY TYPE NAME
        // =====================================================

        private string GetUnityTypeName(
            int typeId)
        {
            switch (typeId)
            {
                case 1:
                    return "GameObject";

                case 4:
                    return "Transform";

                case 21:
                    return "Material";

                case 28:
                    return "Texture2D";

                case 43:
                    return "Mesh";

                case 48:
                    return "Shader";

                case 74:
                    return "AnimationClip";

                case 83:
                    return "AudioClip";

                case 114:
                    return "MonoBehaviour";

                case 115:
                    return "MonoScript";

                case 142:
                    return "AssetBundle";

                case 198:
                    return "ParticleSystem";

                case 199:
                    return "ParticleSystemRenderer";

                default:
                    return "Unknown";
            }
        }


        // =====================================================
        // FILE PICKER
        // =====================================================

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
                    "File Picker gagal",
                    ex
                );
            }
        }


        // =====================================================
        // FILE PICKER RESULT
        // =====================================================

        protected override void OnActivityResult(
           
