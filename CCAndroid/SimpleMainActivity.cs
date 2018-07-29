using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace CCAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = false)]
    public class SimpleMainActivity : AppCompatActivity
    {
        private const int req_Camera = 1;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_simplemain);

            Button camButton = FindViewById<Button>(Resource.Id.btn_camera);

            camButton.Click += CamButton_Click;
        }

        private void CamButton_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            StartActivityForResult(intent, req_Camera);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == req_Camera && resultCode == Result.Ok)
            {
                var extra = data.GetByteArrayExtra("data");

                //Intent intent = new Intent(this, typeof(CountActivity));
                //StartActivity(intent);
            }
        }
    }
}