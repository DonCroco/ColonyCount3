
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

//using CC.Core.DataModel;
using CC.Core;

using Uri = Android.Net.Uri;

//using SQLite;

namespace AppDroid
{
    [Activity(Label = "CountActivity")]
    public class CountActivity : Activity
    {
        CountView countView;
        ViewGroup toolbarViewGroup;
        CCContext ccContext;
        Toolbar toolbar;
        int plateId;
        global::Android.Net.Uri imageUri;

        public static void StartActivity(Context context, int plateId, global::Android.Net.Uri imageUri)
        {
            Intent intent = new Intent(context, typeof(CountActivity));
            intent.PutExtra("plateId", plateId);
            intent.PutExtra("imageUri", imageUri.ToString());
            context.StartActivity(intent);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            plateId = Intent.GetIntExtra("plateId", -1);
            imageUri = Uri.Parse(Intent.GetStringExtra("imageUri"));

            SetContentView(Resource.Layout.activity_count);

            //toolbarViewGroup = (ViewGroup)FindViewById(Resource.Id.toolbar);
            countView = (CountView)FindViewById(Resource.Id.countView);

            ccContext = AppAndroid.CCContext;
            countView.SetContext(ccContext);

            //toolbar = new Toolbar(this.BaseContext, ccContext, toolbarViewGroup);

            //toolbar.SetupCountToolbar();
        }

        protected override void OnResume()
        {
            base.OnResume();

            //// We set resource in resume to make sure all GL is setup correctly after it might have been destroyed
            ccContext.SetGLContextChanged();
            if (plateId == -1)
            {
                if (imageUri.Equals(global::Android.Net.Uri.Empty))
                {
                   // countView.SetBitmapResource(Resource.Drawable.demoimage01);   // TODO demo image
                }
                else
                {
                    countView.SetBitmapFile(imageUri);
                }
            }
            else
            {
                //TableQuery<Image> images = ccContext.Database.QueryImages(plateId);
                //Image image = images.ElementAtOrDefault(0);

                //Android.Net.Uri imageUri = Storage.GetImageFileUri(this, image.Id);

                //countView.SetBitmapFile(imageUri);
            }
        }

        public override void OnBackPressed()
        {
            if (plateId == -1 && !imageUri.Equals(Uri.Empty))
            {
                ShowSaveDialog();
            }
            else
            {
                base.OnBackPressed();
            }
        }


        private void ShowSaveDialog()
        {
            //AlertDialog.Builder builder = new AlertDialog.Builder(this);
            //// Get the layout inflater
            //LayoutInflater inflater = this.LayoutInflater;

            //Activity activity = this;

            //// Inflate and set the layout for the dialog
            //// Pass null as the parent view because its going in the dialog layout
            //AlertDialog dialog =  builder.SetView(inflater.Inflate(Resource.Layout.dialog_save, null))
            //	.SetMessage("Plate not saved in projects. Want to save?")
            //	.SetPositiveButton("Save", (sender, args) => {
            //		AlertDialog d = (AlertDialog)sender;
            //		View view = d.FindViewById(Resource.Id.name);
            //		EditText nameView = (EditText)view;
            //		String name = nameView.Text;
            //		DateTime time = DateTime.Now;
            //		int plateId = ccContext.Database.CreatePlate(time, name);
            //		int imageId = ccContext.Database.CreateImage(plateId, time);

            //		Storage.SaveImageFile(this, imageUri, imageId);

            //		activity.Finish();
            //	})
            //	.SetNeutralButton("Cancel", (sender, args) => {
            //	})
            //	.SetNegativeButton("Quit", (sender, args) => {
            //		activity.Finish();
            //	})
            //.Create();
            //dialog.Show();
        }



    }
}



