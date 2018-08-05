using System;

using Android.Content;

using CC.Core;

namespace AppDroid
{
    public static class AppAndroid
    {
        private static CCContext ccContext;
        private static Context context;

        public static CCContext CCContext
        {
            get { return ccContext; }
        }

        public static void Initialize(Context context)
        {
            if (AppAndroid.context != null)
                return;

            AppAndroid.context = context;
            int zoomWidth = PixelsFromDP(128);
            int zoomOffset = -PixelsFromDP(100);
            int selectRadius = PixelsFromDP(30);
            int tagSize = PixelsFromDP(4);


            //			string dbPath = System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "database.db");


            string dbPath = System.IO.Path.Combine(context.GetExternalFilesDir(null).AbsolutePath, "database.db");

            ccContext = new CCContext(zoomWidth, zoomOffset, selectRadius, tagSize, dbPath);
        }

        public static int PixelsFromDP(int dp)
        {
            float scale = context.Resources.DisplayMetrics.Density;
            return (int)(dp * scale + 0.5f);
        }
    }
}

