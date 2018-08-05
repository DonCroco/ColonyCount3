
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Graphics;
using Android.Opengl;

using OpenTK;
using OpenTK.Graphics.ES30;

using Javax.Microedition.Khronos.Opengles;
using Javax.Microedition.Khronos.Egl;

using CC.Core;

namespace AppDroid
{
    class CountViewRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
    {
        Size viewSize;
        CCContext ccContext;
        Context context;
        //		bool bImageLoaded = false;

        global::Android.Net.Uri bitmapFile = null;
        int bitmapResource = -1;

        public CountViewRenderer(Context context)
        {
            this.context = context;
        }

        public void SetCCContext(CCContext ccContext)
        {
            this.ccContext = ccContext;
            ccContext.SetupImageTexture = HandleSetupImageTexture;
            ccContext.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, viewSize.Width, viewSize.Height, 0, -1, 1);
        }

        public void SetBitmapResource(int resource)
        {
            bitmapFile = global::Android.Net.Uri.Empty;
            bitmapResource = resource;
            ccContext.SetImageChanged();
        }

        public void SetBitmapFile(global::Android.Net.Uri file)
        {
            bitmapFile = file;
            bitmapResource = -1;
            ccContext.SetImageChanged();
        }

        #region IRenderer implementation
        public void OnDrawFrame(IGL10 gl)
        {
            RectangleF rect = new RectangleF(System.Drawing.Point.Empty, viewSize);

            ccContext.RenderUpdate(rect);

            GLHelper.GetError();

        }
        public void OnSurfaceChanged(IGL10 gl, int width, int height)
        {
            viewSize = new Size(width, height);
            if (ccContext != null)
            {
                ccContext.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, viewSize.Width, viewSize.Height, 0, -1, 1);
            }
        }
        public void OnSurfaceCreated(IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
        }
        #endregion

        private void HandleSetupImageTexture(uint texName, out Size size)
        {
            // create bitmap
            Bitmap bitmap;
            if (!bitmapFile.Equals(global::Android.Net.Uri.Empty))
            {
                var f = new Java.IO.File(bitmapFile.Path);
                bitmap = BitmapFactory.DecodeFile(f.Path);
            }
            else
            {
                bitmap = BitmapFactory.DecodeResource(context.Resources, bitmapResource);
            }

            // set context image texture with bitmap
            GLHelper.GetError();
            GL.BindTexture(TextureTarget.Texture2D, ccContext.ImageTexName);
            GLHelper.GetError();
            GLUtils.TexImage2D(GLES30.GlTexture2d, 0, GLES30.GlRgba, bitmap, 0);
            GLHelper.GetError();
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GLHelper.GetError();

            // Set size
            size = new Size(bitmap.Width, bitmap.Height);

            float radius = Math.Min(size.Width, size.Height) * 0.5f * 0.8f;
            ccContext.CountLayerContext.SetROI(new Vector2(size.Width * 0.5f, size.Height * 0.5f), radius);

            // Trigger center view
            ccContext.CenterView(viewSize);

            // recycle bitmap
            bitmap.Recycle();
        }
    }
}

