using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Content;
using Android.Provider;
using Java.IO;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;
using Android.Content.PM;
using System.Collections.Generic;
using Android.Support.V4.Content;
using Android;
using Android.Support.V4.App;

namespace AppDroid
{
    public static class Global
    {
        //        public static Uri cameraIntentUri;
        public static File _file;
        public static File _dir;
    }

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const int REQUEST_CAMERA = 1;
        private const int REQUEST_WRITE = 2;
        private const int REQUEST_READ = 3;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Something to get URI work properly
            StrictMode.VmPolicy.Builder builder = new StrictMode.VmPolicy.Builder();
            StrictMode.SetVmPolicy(builder.Build());


            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            AppAndroid.Initialize(this);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            //Build.VERSION.SDK_INT >= 23 &&

            ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.Camera, Manifest.Permission.ReadExternalStorage, Manifest.Permission.ReadExternalStorage  }, REQUEST_CAMERA);
            //ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.WriteExternalStorage }, REQUEST_WRITE);
            //ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.WriteExternalStorage }, REQUEST_READ);

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == (int)Permission.Granted)
            {
                // We have permission, go ahead and use the camera.
                Android.Util.Log.WriteLine(Android.Util.LogPriority.Debug, "ColonyCount", "PERMISSION");
            }
            else
            {
                // Camera permission is not granted. If necessary display rationale & request.
                Android.Util.Log.WriteLine(Android.Util.LogPriority.Debug, "ColonyCount", "huh?");
            }


            if (IsThereAnAppToTakePictures())
            {
                CreateDirectoryForPictures();

                Button cameraButton = FindViewById<Button>(Resource.Id.btn_camera);
                cameraButton.Click += CameraButton_Click;
            }

        }

        private void CreateDirectoryForPictures()
        {
            Global._dir = new File(
                Environment.GetExternalStoragePublicDirectory(
                    Environment.DirectoryPictures), "ColonyCount");
            if (!Global._dir.Exists())
            {
                Global._dir.Mkdirs();
            }
        }

        private bool IsThereAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }

        private void CameraButton_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            Global._file = new File(Global._dir, String.Format("myPhoto_{0}.jpg", Guid.NewGuid())); // TODO proper name
            intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(Global._file));
            StartActivityForResult(intent, REQUEST_CAMERA);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            //View view = (View) sender;
            //Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
            //    .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            switch (requestCode)
            {
                case REQUEST_CAMERA:

                    if (resultCode == Result.Ok)
                    {
                        // Make it available in the gallery
                        Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
                        Uri contentUri = Uri.FromFile(Global._file);
                        mediaScanIntent.SetData(contentUri);
                        SendBroadcast(mediaScanIntent);

                        if (!Global._file.Exists())
                        {
                            Android.Util.Log.WriteLine(Android.Util.LogPriority.Debug, "ColonyCount", "huh?");
                        }


                        Android.Graphics.Bitmap resizedBitmap = Android.Graphics.BitmapFactory.DecodeFile(Global._file.Path);

                        // Start count activity
                        CountActivity.StartActivity(this, -1, contentUri);
                    }
                    else
                    {
                        // TODO delete temp image ?
                    }


                    break;
            }



        }

        public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            switch (requestCode)
            {
                case REQUEST_CAMERA:
                    {
                        if (grantResults[0] == Permission.Granted)
                        {
                        }
                        else
                        {
                        }
                    }
                    break;
            }
        }
    }
}


