using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;

using OpenTK;
using OpenTK.Graphics.ES30;

namespace CC.Core
{
	public class CountLayerHelper
	{
		public struct SCalcRegionTask {
			public uint[] pixelData;
			public Rectangle roiRect;
			public MutableTexture debugTexture;
			public Tag[] tags;
			public bool bDebugDrawEDM;
			public bool bDebugDrawRegions;
			public event EventHandler regionAdded;
			public DateTime startTime;
			public List<Region> regions;
			public void SendRegionAdded(Region region) {

				if(regionAdded != null)
					regionAdded(region,null);
			}
		}

		private struct SCalcRegionBuffers {
			public ushort[] EDMMap;
			public byte[] maxMap;
			public byte[] regionMap;

			public void Prepare (int requiredSize)
			{
				if (EDMMap == null || EDMMap.Length < requiredSize) {
					int newSize = (int)((float)requiredSize * 1.2f);
					EDMMap = new ushort[newSize];
					maxMap = new byte[newSize];
					regionMap = new byte[newSize];
				} else {
					Array.Clear(EDMMap,0,EDMMap.Length);
					Array.Clear(maxMap,0,maxMap.Length);
					Array.Clear(regionMap,0,regionMap.Length);
				}
			}
		}

		public static void updateROIMask(Rectangle roiRect, Size imageSize, uint frameBuffer, uint roiMaskTexName, uint maskTexName)
		{
			GLHelper.GetError();

			// Setup viewport
			int[] oldViewPort = new int[4];
			GL.GetInteger(GetPName.Viewport, oldViewPort);
			GL.Viewport(0, 0, roiRect.Size.Width, roiRect.Size.Height);
			int fboOld = GLHelper.bindFrameBuffer(frameBuffer, roiMaskTexName);
			
			// Fill with 1.1.1.1
			GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
			GL.Clear(ClearBufferMask.ColorBufferBit);

			GLHelper.GetError();

			// Draw crop
			if(true)
			{
				float[] vertices = GLHelper.CreateSphereVertices(new Vector2((float)roiRect.Size.Width*0.5f,(float)roiRect.Size.Height*0.5f), (float)roiRect.Size.Height*0.5f, true);

				// Set Model view
				Matrix4 MVPMatrix = Matrix4.CreateOrthographicOffCenter(0, (float)roiRect.Size.Width, 0, (float)roiRect.Size.Height, -1.0f, 1.0f);

				AmbientShader shader = AmbientShader.Singleton;
				shader.Use();
				shader.SetColor(new Vector4(0,0,0,0));
				shader.SetMVPMatrix(MVPMatrix);
				shader.EnableVertices(2, VertexAttribPointerType.Float, false, 0, vertices);

				GLHelper.GetError();

				GL.DrawArrays(BeginMode.TriangleFan, 0, vertices.Length/2);

				shader.DisableVertices();
			}

			// Draw mask
			if(true)
			{
				// Render to source texture
				float left = (float)roiRect.Location.X / (float)imageSize.Width;
				float right = (float)(roiRect.Location.X + roiRect.Size.Width) / (float)imageSize.Width;
				float top = (float)roiRect.Location.Y / (float)imageSize.Height;
				float button = (float)(roiRect.Location.Y + roiRect.Size.Height) / (float)imageSize.Height;
				
				// NB: texture coords are mirrored vertically
				float[] texCoords = { right, top, right, button, left,
					top, left, button };

				Matrix4 mat = Matrix4.CreateOrthographicOffCenter(0, (float)roiRect.Size.Width, 0, (float)roiRect.Size.Height, -1.0f, 1.0f);
				Rectangle r = new Rectangle(0,0,roiRect.Width, roiRect.Height);

				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
				GLHelper.DrawSprite(maskTexName, r, mat, texCoords, Vector4.One);

				GL.Disable(EnableCap.Blend);
			}


			
			// unbind framebuffer
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboOld);
			
			// Cleanup viewport
			GL.Viewport(oldViewPort[0], oldViewPort[1], oldViewPort[2], oldViewPort[3]);

			GLHelper.GetError();
		}

		public static void updateROIImage(Rectangle roiRect, uint frameBuffer, uint imageTexName, Size imageSize, uint roiTexName, uint roiMaskTexName)
		{
			GLHelper.GetError();

			// Setup viewport
			int[] oldViewPort = new int[4];
			GL.GetInteger(GetPName.Viewport, oldViewPort);
			GL.Viewport(0, 0, roiRect.Size.Width, roiRect.Size.Height);
			int fboOld = GLHelper.bindFrameBuffer(frameBuffer, roiTexName);

			// Render to source texture
			float left = (float)roiRect.Location.X / (float)imageSize.Width;
			float right = (float)(roiRect.Location.X + roiRect.Size.Width) / (float)imageSize.Width;
			float top = (float)roiRect.Location.Y / (float)imageSize.Height;
			float button = (float)(roiRect.Location.Y + roiRect.Size.Height) / (float)imageSize.Height;
			
			// NB: texture coords are mirrored vertically
			float[] texCoords = { right, top, right, button, left,
				top, left, button };
			
			// Render source
			Matrix4 mat = Matrix4.CreateOrthographicOffCenter(0, (float)roiRect.Size.Width, 0, (float)roiRect.Size.Height, -1.0f, 1.0f);
			Rectangle r = new Rectangle(0,0,roiRect.Width, roiRect.Height);
			GLHelper.DrawSprite(imageTexName, r, mat, texCoords, Vector4.One);
			
			// Render mask onto source
			GL.Enable(EnableCap.Blend);

			GL.BlendFunc(BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcAlpha);
			GLHelper.DrawSprite(roiMaskTexName, r, mat);

			GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.Zero);

			GL.Disable(EnableCap.Blend);
			
			// unbind framebuffer
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboOld);
			GL.Viewport(oldViewPort[0], oldViewPort[1], oldViewPort[2], oldViewPort[3]);

			GLHelper.GetError();
		}

		public static void GetROIThreshold (Rectangle rect, uint[] pixelBuffer, int[] min, int[] max, uint frameBuffer, uint roiTexName)
		{
			// Setup viewport
			int[] oldViewPort = new int[4];
			GL.GetInteger(GetPName.Viewport, oldViewPort);
			GL.Viewport(0, 0, rect.Size.Width, rect.Size.Height);
			int fboOld = GLHelper.bindFrameBuffer(frameBuffer, roiTexName);

			// Update ROI histogram
			// TODO: use already allocated segmentation buffer
			{
				float[] histR = new float[256];
				float[] histG = new float[256];
				float[] histB = new float[256];

				int pixelCount = rect.Width * rect.Height;
				byte[] pixels = new byte[pixelCount * 4];
				GL.ReadPixels (0, 0, rect.Width, rect.Height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
				int count = pixelCount * 4;
				int j = 0;
				int sumR = 0,sumG = 0,sumB = 0;
				int sampleCount = 0;
				while (j < count) {
					int iR = pixels [j ++] & 0xff;
					int iG = pixels [j ++] & 0xff;
					int iB = pixels [j ++] & 0xff;
					int iA = pixels [j ++] & 0xff;
					
					if (iA > 0) {
						sampleCount ++;
						sumR += iR;
						sumG += iG;
						sumB += iB;
						histR [iR] ++;
						histG [iG] ++;
						histB [iB] ++;
					}
				}
				float avrR = (float)sumR/(float)sampleCount;
//				float avrG = (float)sumG/(float)sampleCount;
//				float avrB = (float)sumB/(float)sampleCount;

				Histogram.FillGaps (histR);
				Histogram.FillGaps (histG);
				Histogram.FillGaps (histB);
				
				// Find threshold
				int thresholdR = OtsuThreshold.getThreshold (histR);
				int thresholdG = OtsuThreshold.getThreshold (histG);
				int thresholdB = OtsuThreshold.getThreshold (histB);


				if(avrR > thresholdR) {
					min[0] = 0;
					min[1] = 0;
					min[2] = 0;
					max[0] = thresholdR;
					max[1] = thresholdG;
					max[2] = thresholdB;
				} else {
					min[0] = thresholdR;
					min[1] = thresholdG;
					min[2] = thresholdB;
					max[0] = 1;
					max[1] = 1;
					max[2] = 1;
				}
			}

			// unbind framebuffer
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboOld);
			GL.Viewport(oldViewPort[0], oldViewPort[1], oldViewPort[2], oldViewPort[3]);
		}

		public static void UpdateThresholds (ICollection<Tag> tags, Rectangle roiRect, uint frameBuffer, uint roiTexName)
		{
			// Setup resultTexName framebuffer
			int[] oldViewPort = new int[4];
			GL.GetInteger (GetPName.Viewport, oldViewPort);
			GL.Viewport (0, 0, roiRect.Size.Width, roiRect.Size.Height);
			int fboOld = GLHelper.bindFrameBuffer (frameBuffer, roiTexName);
			
			float[] histR = new float[256];
			float[] histG = new float[256];
			float[] histB = new float[256];

			// TODO: cache tag thresholds and only recalculate when dirty (otherwise many tags will impeede performance)

			if (tags.Count > 0) {
//				Rectangle localRoiRect = new Rectangle (0, 0, roiRect.Width, roiRect.Height);
				int sampleWidth = 100;
				int pixelCount = sampleWidth * sampleWidth;
				byte[] pixels = new byte[pixelCount * 4];
				foreach (Tag tag in tags) {

					if(tag.calculateThreshold) {
						tag.calculateThreshold = false;

						Rectangle sampleRect = tag.ThresholdSampleRect;
						Point loc = sampleRect.Location;
						loc.Offset (-roiRect.Location.X, -roiRect.Location.Y);
						sampleRect.Location = loc;
						
						//				sampleRect = Rectangle.Intersect(tagRect, localRoiRect);
						
						// Skip rects outside roi
						if (sampleRect.Width == 0 || sampleRect.Height == 0) {
							continue;
						}
						
						// Clear histograms
						for (int i=0; i<256; i++) {
							histR [i] = 0;
							histG [i] = 0;
							histB [i] = 0;
						}
						
						// Read pixels
						GL.ReadPixels (sampleRect.Left, sampleRect.Top, sampleRect.Width, sampleRect.Height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
						
						// Get color of tag (center of sample)
						int pxIdx = pixelCount * 2 + sampleRect.Width * 2; 
						tag.Color [0] = pixels [pxIdx] & 0xff;
						tag.Color [1] = pixels [pxIdx + 1] & 0xff;
						tag.Color [2] = pixels [pxIdx + 2] & 0xff;
						
						// Get histogram for pixels around tag
						int count = pixelCount * 4;
						int j = 0;
						while (j < count) {
							int iR = pixels [j ++] & 0xff;
							int iG = pixels [j ++] & 0xff;
							int iB = pixels [j ++] & 0xff;
							int iA = pixels [j ++] & 0xff;
							
							if (iA > 0) {
								histR [iR] ++;
								histG [iG] ++;
								histB [iB] ++;
							}
						}
						
						// Fill gaps in threshold to prepare it for threshold finding
						Histogram.FillGaps (histR);
						Histogram.FillGaps (histG);
						Histogram.FillGaps (histB);
						
						// Find threshold
						tag.Threshold [0] = OtsuThreshold.getThreshold (histR);
						tag.Threshold [1] = OtsuThreshold.getThreshold (histG);
						tag.Threshold [2] = OtsuThreshold.getThreshold (histB);
					}
				}
			} 



			// unbind framebuffer and cleanup viewport
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboOld);
			GL.Viewport(oldViewPort[0], oldViewPort[1], oldViewPort[2], oldViewPort[3]);
		}

		static public void UpdateSegmentation (ICollection<Tag> tags, Rectangle roiRect, uint frameBuffer, 
		                                       uint thresholdTexName, uint roiTexName, uint[] segmentationPixelData,
		                                       int[] roiThresholdMin, int[] roiThresholdMax)
		{

			float[] thresholdMin = { 1, 1, 1 };
			float[] thresholdMax = { 0, 0, 0 };

			if(tags.Count == 0) {
				thresholdMin = new float[] { (float)roiThresholdMin[0]/255.0f,(float)roiThresholdMin[1]/255.0f,(float)roiThresholdMin[2]/255.0f };
				thresholdMax = new float[] { (float)roiThresholdMax[0]/255.0f,(float)roiThresholdMax[1]/255.0f,(float)roiThresholdMax[2]/255.0f };
			} else  {
				// TODO: use ALL thresholds
				foreach(Tag tag in tags) {
					float[] min = new float[3];
					float[] max = new float[3];
					tag.GetNormalizedMaxMin(ref min, ref max);

					thresholdMin[0] = Math.Min(thresholdMin[0], min[0]);
					thresholdMin[1] = Math.Min(thresholdMin[1], min[1]);
					thresholdMin[2] = Math.Min(thresholdMin[2], min[2]);
					thresholdMax[0] = Math.Max(thresholdMax[0], max[0]);
					thresholdMax[1] = Math.Max(thresholdMax[1], max[1]);
					thresholdMax[2] = Math.Max(thresholdMax[2], max[2]);
//					thresholdMin = new float[3] { min[0], min[1], min[2]};
//					thresholdMax = new float[3] { max[0], max[1], max[2]};
					
					Console.WriteLine("color:"+ tag.Color[0] + ","+ tag.Color[1] + ","+ tag.Color[2]);
				}
			}
			
			// Setup thresholdTexName as framebuffer
			int[] oldViewPort = new int[4];
			GL.GetInteger(GetPName.Viewport, oldViewPort);
			GL.Viewport(0, 0, roiRect.Size.Width, roiRect.Size.Height);
			int fboOld = GLHelper.bindFrameBuffer(frameBuffer, thresholdTexName);
			
			// Clear
			GL.ClearColor(1.0f, 0.0f, 0.0f, 1.0f);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			
			
			float x = 0;
			float y = 0;
			float width = roiRect.Size.Width;
			float height = roiRect.Size.Height;
			float[] Vertices = {
				x + width, y,
				x + width, y + height,
				x, y,
				x, y + height};
			
			float[] Texture = {1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f};
			
			Vector4 color = new Vector4(1, 1, 1, 1 );
			
			Matrix4 mat = Matrix4.CreateOrthographicOffCenter(0, (float)roiRect.Size.Width, 0, (float)roiRect.Size.Height, -1.0f, 1.0f);
			
			ThresholdShader shader = ThresholdShader.Singleton;
			// Use shader
			shader.Use();
			
			// Set texture
			shader.SetTexture(roiTexName);

			// Set threshold
			Vector4 vMin = new Vector4(thresholdMin[0],thresholdMin[2],thresholdMin[2],1);
			Vector4 vMax = new Vector4(thresholdMax[0],thresholdMax[2],thresholdMax[2],1);
			shader.SetThreshold(vMin,vMax);
			shader.setColor(color);
			
			// Set Model view
			GL.UniformMatrix4(shader.muMVPMatrixHandle, 1, false, ref mat.Row0.X);
			
			// Set vertex and texture coords
			GL.EnableVertexAttribArray(shader.maPositionHandle);
			GL.EnableVertexAttribArray(shader.maTexCoordHandle);
			
			GL.VertexAttribPointer(shader.maPositionHandle, 2, VertexAttribPointerType.Float, false, 0, Vertices);
			GL.VertexAttribPointer(shader.maTexCoordHandle, 2, VertexAttribPointerType.Float, false, 0, Texture);
			
			GL.DrawArrays(BeginMode.TriangleStrip, 0, 4);
			
			GL.DisableVertexAttribArray(shader.maPositionHandle);
			GL.DisableVertexAttribArray(shader.maTexCoordHandle);
			
			// Read pixel data
			{
				GL.ReadPixels(0, 0, roiRect.Size.Width, roiRect.Size.Height, PixelFormat.Rgba, PixelType.UnsignedByte, segmentationPixelData);
//				GL.ReadPixels<uint>(0, 0, roiRect.Size.Width, roiRect.Size.Height, All.Rgba, All.UnsignedByte, segmentationPixelData);
			}
			
			// unbind framebuffer and cleanup viewport
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboOld);
			GL.Viewport(oldViewPort[0], oldViewPort[1], oldViewPort[2], oldViewPort[3]);
		}

		public static void drawTags (ICollection<Tag> tags, Tag selectedTag, bool layerActive, Matrix4 modelViewMatrix, Matrix4 projectionMatrix, float size)
		{
			float offsetX = modelViewMatrix.M41;
			float offsetY = modelViewMatrix.M42;
			float scale = modelViewMatrix.M11;
			
			float[] Vertices = {
				0.0f, 0.0f + size,
				0.0f - size, 0,
				0.0f + size, 0,
				0.0f, 0.0f - size};
			
//			bool bShowThreshold = true;
			
			AmbientShader shader = AmbientShader.Singleton;
			
			// Use shader
			GL.UseProgram (shader.mProgram);
			
			// Set vertex and texture coords
			GL.EnableVertexAttribArray (shader.maPositionHandle);
			GL.VertexAttribPointer (shader.maPositionHandle, 2, VertexAttribPointerType.Float, false, 0, Vertices);
			
			foreach (Tag tag in tags) {
				float x = (tag.Pos.X) * scale + offsetX;
				float y = (tag.Pos.Y) * scale + offsetY;
				
				Vector4 color;
				if (tag == selectedTag) {
					color = ColorHelper.TagColorSelected;
				} else {
					color = layerActive ? ColorHelper.TagColorActive : ColorHelper.TagColor;
				}
				GL.Uniform4 (shader.muColorHandle, 1, ref color.X);
				
				Matrix4 mat = Matrix4.CreateTranslation (x, y, 0) * projectionMatrix;
				GL.UniformMatrix4 (shader.muMVPMatrixHandle, 1, false, ref mat.Row0.X);
				
				GL.DrawArrays (BeginMode.TriangleStrip, 0, 4);
			}

            // Draw threshold rect
            //			foreach (Tag tag in tags) {
            //				Rectangle thresholdRect = tag.ThresholdSampleRect;
            //				float[] v = {
            //					thresholdRect.Right, thresholdRect.Bottom,
            //					thresholdRect.Right, thresholdRect.Top,
            //					thresholdRect.Left, thresholdRect.Top,
            //					thresholdRect.Left, thresholdRect.Bottom};
            //				GL.VertexAttribPointer (shader.maPositionHandle, 2, VertexAttribPointerType.Float, false, 0, v);
            //				
            //				Matrix4 mat = modelViewMatrix * projectionMatrix;
            //				GL.UniformMatrix4 (shader.muMVPMatrixHandle, 1, false, ref mat.Row0.X);
            //				
            //				GL.DrawArrays (All.LineLoop, 0, 4);
            //			}

            GL.DisableVertexAttribArray(shader.maPositionHandle);
		}

		private static bool FlattenImage(CancellationToken cToken, Rectangle roiRect, byte[] pixelData) {
			// Flatten RGA to 1 byte pixel buffer with 1 indicating segment
			{
				int length = roiRect.Size.Width * roiRect.Size.Height;
				int iSource = 0;
				int iTarget = 0;
				while (iTarget < length)
				{
					int iR = pixelData[iSource++] & 0xff;
					int iG = pixelData[iSource++] & 0xff;
					int iB = pixelData[iSource++] & 0xff;
					//int iA = pixelData[iSource++] & 0xff;
					iSource ++; // Aplha
					
					pixelData[iTarget++] = iR == 255 && iG == 255 && iB == 255  ? (byte)255 : (byte)0;
					
					if(cToken.IsCancellationRequested)
						return false;
				}
				return true;
			}
		}

		private static void DebugDragEDM(Rectangle regionBounds, SCalcRegionBuffers buffers, SCalcRegionTask data) {

			Rectangle roiRect = data.roiRect;

			int max = 0;
			int count = regionBounds.Width*regionBounds.Height;
			for(int i=0;i<count;i++)
			{
				int edm = buffers.EDMMap[i];
				max = edm > max ? edm : max;
			}
			
			float factor = 255.0f/(float)max;
			int offsetX = regionBounds.X - roiRect.X;
			int offsetY = regionBounds.Y - roiRect.Y;
			int ri = 0;
			for(int ry=0;ry<regionBounds.Height;ry++) {
				for(int rx=0;rx<regionBounds.Width;rx++) {
					int scaledEDM = (int)((float)buffers.EDMMap[ri]*factor);
					data.debugTexture.SetPixel(rx + offsetX, ry + offsetY, scaledEDM,scaledEDM,scaledEDM,1);
					ri ++;
				}
			}
		}

		private static void DebugDragMaxima(Rectangle regionBounds, SCalcRegionBuffers buffers, SCalcRegionTask data,
		                                    Segmentation.MaxPoint[] maxPoints , int iMaxPointCount) {
			Rectangle roiRect = data.roiRect;

			int regionROIOffsetX = regionBounds.X - roiRect.X;
			int regionROIOffsetY = regionBounds.Y - roiRect.Y;
			
			if(iMaxPointCount > 1) {
				Console.WriteLine(" ----- Max debug -----");
				Console.WriteLine(" roi: pos({0},{1}) size({2},{3})", regionBounds.X, regionBounds.Y, regionBounds.Width, regionBounds.Height);
				Console.WriteLine(" max count:" + iMaxPointCount);
			}
			int ri = 0;
			for(int ry=0;ry<regionBounds.Height;ry++) {
				for(int rx=0;rx<regionBounds.Width;rx++) {
					
					int iVal = buffers.maxMap[ri];
					if(iVal > 0) {
						if((iVal & Segmentation.MAX_POINT) > 0) {
							data.debugTexture.SetPixel(rx+ regionROIOffsetX,ry+ regionROIOffsetY, 0,255,0,1);
						} else {
							data.debugTexture.SetPixel(rx+ regionROIOffsetX,ry+ regionROIOffsetY, 255,0,0,1);
						}
						if(iMaxPointCount > 1) {
							Console.WriteLine("maxMap({0},{1})={2}", rx,ry,Segmentation.GetMaxMapString(iVal));
						}
					}
					ri ++;
				}
			}
			if(iMaxPointCount > 1) {
				foreach(Segmentation.MaxPoint maxPoint in maxPoints) {
					ri = maxPoint.y*regionBounds.Width + maxPoint.x;
					int iVal = buffers.maxMap[ri];
					Console.WriteLine("maxPoint({0},{1})={2} : {3}",maxPoint.x,maxPoint.y,maxPoint.value,Segmentation.GetMaxMapString(iVal));
				}
			}
		}


		public static SCalcRegionTask calcRegions(CancellationToken cToken, SCalcRegionTask data)
		{
			Rectangle roiRect = data.roiRect;

			// Flatten RGA to 1 byte pixel buffer with 1 indicating segment
//			if(!FlattenImage(cToken, roiRect, data.pixelData)) {
//				return data;
//			}

			// ********************************************************
			// DETECT REGIONS
			// ********************************************************


			// Allocate stuff
			List<Point> points = new List<Point>(128*128);
			SCalcRegionBuffers buffers = new SCalcRegionBuffers();
			buffers.Prepare(256*256);
			bool isEDM = true;


			// Get regions at tags
			bool propertiesSet = false;
			int test = 0;
			foreach(Tag tag in data.tags) {
				Point posROI = new Point((int)tag.Pos.X - roiRect.Location.X, (int)tag.Pos.Y - roiRect.Location.Y);
				int index = posROI.Y*roiRect.Size.Width + posROI.X;
				uint val = data.pixelData[index];
				if(val == 0x00FFFFFF) {

					points.Clear();
					QuickFill<uint>.FloodFill(posROI, data.pixelData, roiRect.Size, val, 0, points);

					if(!propertiesSet) {
						propertiesSet = true;

						// Get bounds
						Rectangle regionBounds;
						Region.Transform (points, Point.Empty, out regionBounds);

						if(cToken.IsCancellationRequested)
							return data;

						// Prepare buffers
						int regionsize = regionBounds.Width * regionBounds.Height;
						buffers.Prepare(regionsize);
						
						// Load binary map
						Region.loadBinaryMap(points, new Point(-regionBounds.X, -regionBounds.Y), buffers.EDMMap, regionBounds.Width);
						
						if(cToken.IsCancellationRequested)
							return data;
						
						// Create EDM
						EDM.createEDM(buffers.EDMMap, regionBounds.Width, regionBounds.Height);

						int[] globalMinMax;
						Segmentation.FindGlobalMinMax(buffers.EDMMap, regionBounds.Width, regionBounds.Height, out globalMinMax);

						test = globalMinMax[1];
					}




				}

				if(cToken.IsCancellationRequested)
					return data;
			}



			// Create regions from segmentation map
			int height = roiRect.Size.Height;
			int width = roiRect.Size.Width;
			int count = height*width;
			float invWidth = 1.0f/(float)width;
			for(int index = 0; index < count; index ++) {
				uint val = data.pixelData[index];
				if(val == 0x00FFFFFF) 
				{
					int x2 = index % width - 1;
					int y2 = (int)((float)(index - x2)*invWidth);
					Point pos = new Point(x2, y2);

					points.Clear();
//					QuickFill<uint>.FloodFill(pos, data.pixelData, roiRect.Size, val, 0, points);
					QuickFillUint.FloodFill(pos, data.pixelData, roiRect.Size, val, 0, points);

					if(cToken.IsCancellationRequested)
						return data;

					// Transform region points to image space
					Rectangle regionBounds;
					Region.Transform (points, roiRect.Location, out regionBounds);

					// Prepare buffers
					int regionsize = regionBounds.Width * regionBounds.Height;
					buffers.Prepare(regionsize);

					// Load binary map
					Region.loadBinaryMap(points, new Point(-regionBounds.X, -regionBounds.Y), buffers.EDMMap, regionBounds.Width);

					if(cToken.IsCancellationRequested)
						return data;

					// Create EDM
					EDM.createEDM(buffers.EDMMap, regionBounds.Width, regionBounds.Height);

					if(cToken.IsCancellationRequested)
						return data;

					// Debug render EDM
					if(data.bDebugDrawEDM) {
						DebugDragEDM(regionBounds, buffers, data);
					}

					if(cToken.IsCancellationRequested)
						return data;

					// Find maxima
					int minSize = Math.Min(regionBounds.Width,regionBounds.Height);
					double tolerance = 20;

//					double findMaxThreshold = 4*EDM.ONE; 
					double findMaxThreshold = (float)test*0.5f; 

					Segmentation.MaxPoint[] maxPoints;
					int[] globalMinMax;
					Segmentation.FindGlobalMinMax(buffers.EDMMap, regionBounds.Width, regionBounds.Height, out globalMinMax);
					Segmentation.findMaxima(buffers.EDMMap, regionBounds.Width, regionBounds.Height, tolerance,
					                        findMaxThreshold, buffers.maxMap, isEDM, globalMinMax, out maxPoints);

					// Count max points
					int iMaxPointCount = 0;
					foreach(Segmentation.MaxPoint maxPoint in maxPoints) {
						int ri = maxPoint.y*regionBounds.Width + maxPoint.x;
						int iVal = buffers.maxMap[ri];
						if((iVal & Segmentation.MAX_POINT) > 0) {
							iMaxPointCount ++;
						}
					}


					// Skip all regions with no max
					if(iMaxPointCount == 0)
						continue;

					// Debug Render maxima
					if(data.bDebugDrawEDM)
					{
						DebugDragMaxima(regionBounds, buffers, data, maxPoints, iMaxPointCount);
					}

				
					if(cToken.IsCancellationRequested)
						return data;

					if(iMaxPointCount == 1) {
						Region region = new Region(points, regionBounds);
						data.regions.Add(region);
						data.SendRegionAdded(region);
					}
					else {

						// TODO: could watershed write region map in different colors (so we dont have to quickfill)
						double watershedThreshold = 1;
						Segmentation.performWatershed(buffers.EDMMap, regionBounds.Width, regionBounds.Height, watershedThreshold, 
						                              buffers.maxMap, isEDM, buffers.regionMap, maxPoints, globalMinMax);

						// Debug render region map
						if(data.bDebugDrawRegions) {
							int offsetX = regionBounds.X - roiRect.X;
							int offsetY = regionBounds.Y - roiRect.Y;
							int ri = 0;
							for(int ry=0;ry<regionBounds.Height;ry++) {
								for(int rx=0;rx<regionBounds.Width;rx++) {
									int iVal = buffers.regionMap[ri];
									data.debugTexture.SetPixel(rx + offsetX, ry + offsetY, iVal,0,0,1);
									ri ++;
								}
							}
						}

						if(cToken.IsCancellationRequested)
							return data;

						for (int ry = 0; ry < regionBounds.Height; ry++) {
							for (int rx = 0; rx < regionBounds.Width; rx++) {
								int ri = (regionBounds.Width * ry) + rx;
								uint iVal = (uint)(buffers.regionMap[ri] & 0xFF);

								if (iVal == 255) {
									points.Clear();
//									QuickFill<byte>.FloodFill(new Point(rx, ry), buffers.regionMap, regionBounds.Size, (byte)iVal, 0, points);
									QuickFillByte.FloodFill(new Point(rx, ry), buffers.regionMap, regionBounds.Size, (byte)iVal, 0, points);

									if(cToken.IsCancellationRequested)
										return data;
									
									Rectangle bounds;
									Region.Transform (points, regionBounds.Location, out bounds);

									Region region = new Region(points, bounds);
									data.regions.Add(region);
									data.SendRegionAdded(region);
								}
							}
						}
					}
					
					if(cToken.IsCancellationRequested)
						return data;
				}
			}
			return data;
		}

		public static void DrawRegions (ICollection<Region> regions, Matrix4 modelViewMatrix, Matrix4 projectionMatrix)
		{
			float offsetX = modelViewMatrix.M41;
			float offsetY = modelViewMatrix.M42;
			float scale = modelViewMatrix.M11;
			
			AmbientShader shader = AmbientShader.Singleton;
			shader.Use ();
			
			bool bDrawRegionData = false;
			if (bDrawRegionData) {
				shader.SetColor(new Vector4(1,0,0,1));
				shader.SetMVPMatrix(projectionMatrix);
				
				foreach (Region region in regions) {
					int count = region.Points.Count; 
					if(count > 0)
					{
						float[] vertices = new float[count*2];
						int iSource = 0;
						int iTarget = 0;
						while(iSource < count)
						{
							vertices[iTarget ++] = region.Points[iSource].X*scale + offsetX;
							vertices[iTarget ++] = region.Points[iSource].Y*scale + offsetY;
							iSource ++;
						}
						
						shader.EnableVertices(2, VertexAttribPointerType.Float, false, 0, vertices);
						
						GL.DrawArrays(BeginMode.Points, 0, count);
						
						shader.DisableVertices();
					}
				}
			}

			Vector4 color = ColorHelper.Region;
			shader.SetColor(color);
			shader.SetMVPMatrix(modelViewMatrix*projectionMatrix);
			foreach (Region region in regions) {
				
				
				int x = region.Bounds.X;
				int y = region.Bounds.Y;
				int width = region.Bounds.Width;
				int height = region.Bounds.Height;
				float[] Vertices = {
					x + width, y,
					x + width, y + height,
					x, y + height,
					x, y,
				};
				
				shader.EnableVertices(2, VertexAttribPointerType.Float, false, 0, Vertices);
				
				GL.DrawArrays(BeginMode.LineLoop, 0, 4);
				
				shader.DisableVertices();
			}
		}

		public static void DrawROI (RectangleF viewRect, Vector2 vROICenter, float fROIRadius, Rectangle roiRect, uint roiMaskTexName, 
		                            Vector4 outlineColor, float outlineWidth, Vector4 bgColor, Matrix4 MVMatrix, Matrix4 PMatrix)
		{
			
			Matrix4 MVPMatrix = MVMatrix * PMatrix;
			
			GL.Enable(EnableCap.Blend);
			{
				GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
//				GL.BlendFunc(All.Zero, All.OneMinusSrcAlpha);
				GLHelper.DrawSprite(roiMaskTexName, roiRect, MVPMatrix, null, bgColor);
			}
			
			// Draw 4 areas outside ROI
			{
				Vector4 vTopLeft = Vector4.Transform (new Vector4 (roiRect.X, roiRect.Y, 0, 1), MVMatrix);
				Vector4 vButtomRight = Vector4.Transform (new Vector4 (roiRect.Right, roiRect.Bottom, 0, 1), MVMatrix);
				RectangleF boxRect;
				
				boxRect = RectangleF.Intersect (viewRect, new RectangleF (0, 0, viewRect.Width, vTopLeft.Y));
				if (boxRect != RectangleF.Empty) {
					GLHelper.drawRect (boxRect, bgColor, PMatrix);
				}
				boxRect = RectangleF.Intersect (viewRect, new RectangleF (0, vButtomRight.Y, viewRect.Width, viewRect.Height - vButtomRight.Y));
				if (boxRect != RectangleF.Empty) {
					GLHelper.drawRect (boxRect, bgColor, PMatrix);
				}
				boxRect = RectangleF.Intersect (viewRect, new RectangleF (0, vTopLeft.Y, vTopLeft.X, vButtomRight.Y - vTopLeft.Y));
				if (boxRect != RectangleF.Empty) {
					GLHelper.drawRect (boxRect, bgColor, PMatrix);
				}
				boxRect = RectangleF.Intersect (viewRect, new RectangleF (vButtomRight.X, vTopLeft.Y, viewRect.Width - vButtomRight.X, vButtomRight.Y - vTopLeft.Y));
				if (boxRect != RectangleF.Empty) {
					GLHelper.drawRect (boxRect, bgColor, PMatrix);
				}
			}
			GL.Disable (EnableCap.Blend);
			
			
			// ROI sphere 
			{
				float r = fROIRadius;
				Vector2 pos = vROICenter;
				float[] vertices = GLHelper.CreateSphereVertices(pos,r,false);

				float oldLineWidth = 1;
				GL.GetFloat(GetPName.LineWidth, out oldLineWidth);
				GL.LineWidth(outlineWidth);

				AmbientShader shader = AmbientShader.Singleton;
				shader.Use();
				shader.SetColor(outlineColor);
				shader.SetMVPMatrix(MVPMatrix);
				shader.EnableVertices(2, VertexAttribPointerType.Float, false, 0, vertices);
				
				GL.DrawArrays(BeginMode.LineLoop, 0, vertices.Length/2);
				
				shader.DisableVertices();

				GL.LineWidth(oldLineWidth);
			}
		}

		public static void MaskDraw(Vector2 pos, Vector2 prevPos, bool bForeground, uint frameBuffer, 
		                            uint texName, Size texSize, float[] sphereVertices, int scale) {
			
			// FIXME: radius is in image space and needs to be dps adjusted
			
			// Setup viewport
			int[] oldViewPort = new int[4];
			GL.GetInteger(GetPName.Viewport, oldViewPort);
			GL.Viewport(0, 0, texSize.Width, texSize.Height);
			int fboOld = GLHelper.bindFrameBuffer(frameBuffer, texName);
			
			Vector4 foreground = new Vector4( 1, 1, 1, 1 );
			Vector4 background = new Vector4( 0, 0, 0, 0 );
			Vector4 color = bForeground ? foreground : background;
			
			AmbientShader shader = AmbientShader.Singleton;
			shader.Use();
			shader.SetColor(color);

			Matrix4 ProjMatrix = Matrix4.CreateOrthographicOffCenter(0, (float)texSize.Width, 0, (float)texSize.Height, -1.0f, 1.0f);

			// Draw sphere at pos
			{

				// Set Model view
				Matrix4 MVPMatrix = Matrix4.Scale(scale)*Matrix4.CreateTranslation(new Vector3(pos))*ProjMatrix;

				shader.SetMVPMatrix(MVPMatrix);
				shader.EnableVertices(2, VertexAttribPointerType.Float, false, 0, sphereVertices);
				
				GLHelper.GetError();
				
				GL.DrawArrays(BeginMode.TriangleFan, 0, sphereVertices.Length/2);
				
				shader.DisableVertices ();
			}
			
			// Connect prev to current
			if (!pos.Equals (prevPos)) 
			{
				shader.SetMVPMatrix(ProjMatrix);


				//				shader.SetMVPMatrix(projMatrix);
				
				//				Vector2 v0 = CCContext.ViewToModel(prevPos, modelViewMatrix);
				//				Vector2 v1 = CCContext.ViewToModel(pos, modelViewMatrix);
				Vector2 v0 = prevPos;
				Vector2 v1 = pos;
				
				Vector3 up = new Vector3( 0, 0, 1 );
				Vector3 dir = new Vector3(v1) - new Vector3(v0);
				dir = Vector3.Normalize(dir);
				
				Vector3 cross = Vector3.Cross(up,dir)*scale;
				
				float[] vertices = new float[4*2];
				int i = 0;
				
				vertices[i++] = v0.X - cross.X;
				vertices[i++] = v0.Y - cross.Y;
				
				vertices[i++] = v0.X + cross.X;
				vertices[i++] = v0.Y + cross.Y;
				
				vertices[i++] = v1.X - cross.X;
				vertices[i++] = v1.Y - cross.Y;
				
				vertices[i++] = v1.X + cross.X;
				vertices[i++] = v1.Y + cross.Y;
				
				shader.EnableVertices(2, VertexAttribPointerType.Float, false, 0, vertices);
				
				GL.DrawArrays(BeginMode.TriangleStrip, 0, 4);
				
				//				GLES20.glDrawArrays(GLES20.GL_LINE_STRIP, 0, 4);
				
				shader.DisableVertices ();
			}	
			
			// unbind framebuffer
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboOld);
			GL.Viewport(oldViewPort[0], oldViewPort[1], oldViewPort[2], oldViewPort[3]);
		}

	}
}

