using System;
using OpenTK;
using OpenTK.Graphics.ES30;

using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;

//using SQLite;
//using CC.Core.DataModel;

namespace CC.Core
{
	public class CCContext
	{
		public delegate void SetupImageTextureDelegate( uint texName, out Size size);
		public delegate void RequestRenderDelegate();

		Size imageSize = Size.Empty;
		Matrix4 modelViewMatrix = Matrix4.Scale(0.3f);
		Matrix4 projectionMatrix = Matrix4.Identity;
		SizeF viewSize;
		uint imageTexName;
		bool bSetupGL = true;
		bool bUpdateImage = false;
//		Database db;

		Size zoomSize;
		int zoomOffset;
		int selectRadius;
		int tagSize;
		uint zoomTexName;
		IntPtr zoomBuffer;
//		float density;

		CountLayerContext countLayerContext; 

		public CountLayerContext CountLayerContext {
			get { return countLayerContext; }
		}

		public Matrix4 ModelViewMatrix {
			get { return modelViewMatrix; }
			set { 
				modelViewMatrix = value;
				RequestRender();
			}
		}

		public Matrix4 ProjectionMatrix
		{
			get { return projectionMatrix; }
			set { 
				projectionMatrix = value;
				RequestRender();
			}
		}
	
		public SizeF ViewSize
		{
			get { return viewSize; }
			set { 
				viewSize = value;
				ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, viewSize.Width, viewSize.Height, 0 , -1, 1);
			}
		}

		public SetupImageTextureDelegate SetupImageTexture
		{
			get;
			set;
		}

		public RequestRenderDelegate RequestRender
		{
			get;
			set;
		}

		public Size ImageSize {
			get { return imageSize; }
		}

		public uint ImageTexName {
			get { 
				return imageTexName; 
			} 
		}

		public int SelectRadius {
			get { return selectRadius; }
		}

		public int DrawRadius {
			get { 
				return (int)((float)selectRadius*ViewToModel (1.0f,ModelViewMatrix));
			}
		}

		public int TagSize {
			get { return tagSize; }
		}

		//public DataModel.Database Database {
		//	get { return db; }
		//}

		public CCContext(int zoomWidth, int zoomOffset, int selectRadius, int tagSize, string dbPath)
		{
			this.selectRadius = selectRadius;
			this.zoomOffset = zoomOffset;
			this.tagSize = tagSize;
			countLayerContext = new CountLayerContext(this);

			zoomSize = new Size (zoomWidth, zoomWidth);
			int length = zoomSize.Width*zoomSize.Height;
			zoomBuffer = Marshal.AllocHGlobal(length * 4);

//			db = new Database(dbPath);
		}

		public void SetGLContextChanged()
		{
			bSetupGL = true;
		}

		public void SetImageChanged()
		{	
			bUpdateImage = true;
			RequestRender();
		}

		// Should be called from GL thread
		public void RenderUpdate (RectangleF rect)
		{

			if (bSetupGL) {
				bSetupGL = false;
				SetupGL();
				countLayerContext.SetupGL();
			}

			if (bUpdateImage) {
				bUpdateImage = false;
				if (SetupImageTexture != null) {
					SetupImageTexture (imageTexName, out imageSize);
				}
				countLayerContext.OnImageUpdated();
			}

			countLayerContext.RenderUpdate(rect);

			if (countLayerContext.Selection != null) {

				Vector2 selectCenter = CountLayerContext.Selection.GetCenter();
				Vector4 selectPos = Vector4.Transform(new Vector4(selectCenter.X, selectCenter.Y, 0, 1), modelViewMatrix);
				ShowZoom(zoomTexName, zoomBuffer, selectPos, rect.Height, projectionMatrix, zoomOffset);
			}

			GLHelper.GetError();
		}

		public void CenterView (Size viewSize)
		{
			float fScaleW = (float)viewSize.Width / imageSize.Width;
			float fScaleH = (float)viewSize.Height / imageSize.Height;
			
			float fScale;
			Vector3 vTranslate = Vector3.Zero;
			if (fScaleW < fScaleH) {
				fScale = fScaleW;
				vTranslate.Y = (viewSize.Height - fScale*imageSize.Height)*0.5f;
			} else {
				fScale = fScaleH;
				vTranslate.X = (viewSize.Width - fScale*imageSize.Width)*0.5f;
			}
			
			ModelViewMatrix = Matrix4.Scale(fScale)*Matrix4.CreateTranslation(vTranslate);

			RequestRender();
		}


		public static Vector2 ViewToModel (Vector2 vIn, Matrix4 modelViewMatrix)
		{
			Matrix4 invModelView = Matrix4.Invert (modelViewMatrix);
			Vector4 v = Vector4.Transform (new Vector4 (vIn.X, vIn.Y, 0,1), invModelView);
			return new Vector2(v.X, v.Y);
		}
		
		public static float ViewToModel (float val,Matrix4 modelViewMatrix)
		{
			Matrix4 invModelView = Matrix4.Invert (modelViewMatrix);
			return val*invModelView.M11;
		}

		public static Matrix4 CreateScaleAroundPoint(Vector3 pos, float scale)
		{
			Matrix4 mat = Matrix4.CreateTranslation(-pos)*Matrix4.Scale(scale)*Matrix4.CreateTranslation(pos);
			return mat;
		}

		
		// **************************************************************************
		//							PRIVATE
		// **************************************************************************

		private void SetupGL ()
		{
			// get an id from OpenGL
			GL.GenTextures(1, out imageTexName);
			GL.BindTexture(TextureTarget.Texture2D, imageTexName);
			SetDefaultTexParameters();

			GL.GenTextures(1, out zoomTexName);
			GL.BindTexture(TextureTarget.Texture2D, zoomTexName);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) All.Linear);

			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public static void setupTexture(uint texName, SizeF size, IntPtr data)
		{
			GL.BindTexture(TextureTarget.Texture2D, texName);
            GLHelper.GetError();

            SetDefaultTexParameters();
            GLHelper.GetError();

            // generate the OpenGL texture
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)size.Width, (int)size.Height, 0,
                          PixelFormat.Rgba, PixelType.UnsignedByte, data);
            GLHelper.GetError();

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GLHelper.GetError();
        }

		// Apply default tex parameters on current texture
		public static void SetDefaultTexParameters ()
		{
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) All.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) All.ClampToEdge);
		}

		private void ShowZoom(uint texName, IntPtr buffer, Vector4 selectPos, float viewHeight, Matrix4 projMatrix, int offsetY)
		{

			// GL has inverted y axis
			Vector2 invertSelectPos = new Vector2(selectPos.X, viewHeight - selectPos.Y);
			Rectangle readRect = new Rectangle ((int)selectPos.X - zoomSize.Width/2, (int)invertSelectPos.Y - zoomSize.Height/2, zoomSize.Width, zoomSize.Height);
			
			Rectangle drawRect = new Rectangle ((int)selectPos.X - zoomSize.Width/2, (int)selectPos.Y - zoomSize.Height/2 + offsetY + zoomSize.Height, zoomSize.Width, -zoomSize.Height);

			readPixelsToTexture(readRect, texName, buffer);

			GLHelper.DrawSprite (texName, drawRect, projMatrix, null, Vector4.One);

			// Draw edge
			
			float[] Vertices = {
				drawRect.X, drawRect.Y,
				drawRect.X + drawRect.Width, drawRect.Y,
				drawRect.X + drawRect.Width, drawRect.Y + drawRect.Height,
				drawRect.X, drawRect.Y + drawRect.Height};

			GL.LineWidth(2);
			GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.SrcColor);

			AmbientShader shader = AmbientShader.Singleton;
			shader.Use ();
			shader.SetColor (new Vector4 (0.2f, 0.2f, 0.2f, 0.7f));
			shader.SetMVPMatrix (projMatrix);
			shader.EnableVertices (2, VertexAttribPointerType.Float, false, 0, Vertices);

			GL.DrawArrays(BeginMode.LineLoop, 0, 4);

			shader.DisableVertices ();

			GL.LineWidth(1);
		}

		private static void readPixelsToTexture(Rectangle rect,uint texName, IntPtr buffer)
		{

			GL.ReadPixels(rect.X, rect.Y, rect.Width, rect.Height, PixelFormat.Rgba, PixelType.UnsignedByte,buffer);
			
			GLHelper.GetError();
			
			GL.BindTexture(TextureTarget.Texture2D, texName);
			
			GLHelper.GetError();

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, rect.Width, rect.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, buffer);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			GLHelper.GetError();
		}
	}
}

