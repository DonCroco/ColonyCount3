using System;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Views;

namespace CCAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if(resultCode == Result.Ok)
            {
                var extra = data.GetByteArrayExtra("data");

                Intent intent = new Intent(this, typeof(CountActivity));
                StartActivity(intent);
            }
        }
    }
}

