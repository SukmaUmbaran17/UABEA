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
        private LinearLayout? assetList;

        private AssetsManager? assetsManager;

        private AssetsFileInstance? currentAssetsFile;
        private BundleFileInstance? currentBundle;


        // =====================================================
        // ON CREATE
        // =====================================================

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            LinearLayout root =
                new LinearLayout(this);

            root.Orientation =
                Orientation.Vertical;

            root.SetPadding(
                30,
                30,
                30,
                30
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


            // =================================================
            // STATUS
            // =================================================

            status =
                new TextView(this);

            status.TextSize =
                16;

            status.Text =
                "Menyiapkan UABEA Android...\n";


            // =================================================
            // ASSET LIST
            // =================================================

            assetList =
                new LinearLayout(this);

            assetList.Orientation =
                Orientation.Vertical;


            // =================================================
            // SCROLL
            // =================================================

            ScrollView scrollView =
                new ScrollView(this);

            scrollView.AddView(
                assetList
            );


            // =================================================
            // ROOT
            // =================================================

            root.AddView(title);

            root.AddView(openButton);

            root.AddView(status);


            LinearLayout.LayoutParams scrollParams =
