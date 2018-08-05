
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Net;

using Android.Opengl;

using OpenTK;

using CC.Core;

// http://android-developers.blogspot.dk/2010/06/making-sense-of-multitouch.html

namespace AppDroid
{
    public class CountView : GLSurfaceView, GestureDetector.IOnGestureListener, ScaleGestureDetector.IOnScaleGestureListener
    {
        CCContext ccContext;
        CountViewRenderer renderer;
        GestureDetector gestureDetector;
        ScaleGestureDetector scaleGestureDetector;
        Vector2 lastDragPos;
        Vector2 lastScaleCenter;

        public CountView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        private void Initialize()
        {
            SetEGLContextClientVersion(2); // GLES 2
                                           //			SetZOrderOnTop(true);
                                           //			SetEGLConfigChooser(8, 8, 8, 8, 16, 0);
                                           //			Holder.SetFormat(Format.Rgba8888);
                                           //			//		setEGLConfigChooser(8, 8, 8, 8, 16, 0);
                                           //			//		setDebugFlags(DEBUG_CHECK_GL_ERROR | DEBUG_LOG_GL_CALLS);

            // TODO: find better way to handle shaders. Static dont work when restarting context
            AmbientShader.Reset();
            ThresholdShader.Reset();
            TextureShader.Reset();


            renderer = new CountViewRenderer(Context);
            SetRenderer(renderer);

            gestureDetector = new GestureDetector(this);
            gestureDetector.IsLongpressEnabled = true;
            scaleGestureDetector = new ScaleGestureDetector(Context, this);
        }

        private Vector2 ViewToModel(PointF point, Matrix4 modelViewMatrix)
        {
            Matrix4 invModelView = Matrix4.Invert(modelViewMatrix);
            Vector3 v = Vector3.Transform(new Vector3(point.X, point.Y, 0), invModelView);
            return new Vector2(v.X, v.Y);
        }


        public override bool OnTouchEvent(MotionEvent e)
        {
            bool handled;

            handled = scaleGestureDetector.OnTouchEvent(e);
            //			if (handled)
            //				return true;

            handled = gestureDetector.OnTouchEvent(e);
            //			if (handled)
            //				return true;


            if (ccContext.CountLayerContext.IsSelectionValid)
            {
                if (e.Action == MotionEventActions.Move)
                {

                    Vector2 pos = new Vector2(e.GetX(), e.GetY());
                    Vector2 trans = pos - lastDragPos;
                    ccContext.CountLayerContext.OnDrag(pos, trans);

                    lastDragPos = pos;
                }
                else
                {
                    ccContext.CountLayerContext.OnUnselect();
                }
            }

            return true;
        }

        public void SetContext(CCContext ccContext)
        {
            this.ccContext = ccContext;
            ccContext.RequestRender = OnReqestRender;
            renderer.SetCCContext(ccContext);
        }

        public void SetBitmapResource(int resource)
        {
            renderer.SetBitmapResource(resource);
        }

        public void SetBitmapFile(global::Android.Net.Uri file)
        {

            renderer.SetBitmapFile(file);
        }

        private void OnReqestRender()
        {
            // request might have to be forced to UI thread as it is called from worker thread
            //			activity.runOnUiThread(new Runnable() {
            //				public void run() {
            //					RequestRender();
            //				}
            //			});

            RequestRender();
        }



        #region IOnGestureListener implementation

        public bool OnDown(MotionEvent e)
        {
            return true;
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            return false;
        }

        public void OnLongPress(MotionEvent e)
        {
            Vector2 pos = new Vector2(e.GetX(), e.GetY());
            if (ccContext.CountLayerContext.OnSelect(pos))
            {
                lastDragPos = pos;
            }
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            Vector2 pos = new Vector2(e2.GetX(), e2.GetY());
            Vector2 trans = new Vector2(-distanceX, -distanceY);
            ccContext.CountLayerContext.OnDrag(pos, trans);
            return true;
        }

        public void OnShowPress(MotionEvent e)
        {
            //int i;
        }

        public bool OnSingleTapUp(MotionEvent e)
        {
            Vector2 v = new Vector2(e.GetX(), e.GetY());
            ccContext.CountLayerContext.OnTapGesture(v);
            return true;
        }

        #endregion

        #region IOnScaleGestureListener implementation

        public bool OnScale(ScaleGestureDetector detector)
        {

            Vector2 center = new Vector2(detector.FocusX, detector.FocusY);
            Vector2 translate = center - lastScaleCenter;
            float scale = detector.ScaleFactor;
            ccContext.CountLayerContext.OnPinch(center, translate, scale);

            lastScaleCenter = center;

            return true;
        }

        public bool OnScaleBegin(ScaleGestureDetector detector)
        {
            return true;
        }

        public void OnScaleEnd(ScaleGestureDetector detector)
        {
        }

        #endregion
    }
}

