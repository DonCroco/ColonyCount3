using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

using OpenTK;
using OpenTK.Graphics.ES30;

namespace CC.Core
{
	public class CountLayerContext
	{
		public delegate void CountLayerUpdateDelegate(int tagCount, int regionCount);
		public delegate void SaveCountResultDelegate(byte[] data, Size size, int tagCount, int regionCount);

		public enum Tool
		{
			TagCreate,
			TagDelete,
			ROIEdit,
			MaskDraw,
			MaskErase,
		}

		public CountLayerUpdateDelegate CountLayerUpdated;
		public SaveCountResultDelegate SaveCountResult;

		CCContext ccContext;
		Tool activeTool;
		Rectangle roiRect = Rectangle.Empty;
		List<Tag> tags = new List<Tag>();
		Sphere roiSphere = new Sphere();

		PrimSelector selectedPrim = null;

		int[] roiThresholdMin = new int[3];
		int[] roiThresholdMax = new int[3];
		uint roiTexName;
		uint maskTexName;
		uint roiMaskTexName;
		uint thresholdTexName;
		uint debugTexName;
		bool bUpdateImage = false;
		bool bUpdateROI = false;
		bool bUpdateSeqmentation = false;
		bool bLateUpdateSegmentation = false; 
		bool bRequestSaveImage = false;
		bool bUpdateROIThreshold = false;
		MutableTexture debugTexture;
		uint frameBuffer;
		uint[] segmentationPixelData;
		List<Region> regions = new List<Region>();
		List<Region> incommingRegions = new List<Region>();
		CancellationTokenSource cCalcRegionTokenSource;
		Task<CountLayerHelper.SCalcRegionTask > calcRegionTask;
		Vector2 lastMaskDrawPos;
		float[] unitSphereVertices;

		public Tool ActiveTool {
			get { return activeTool; }
			set {
				if (activeTool != value) {
					this.activeTool = value;
					selectedPrim = null;
					ccContext.RequestRender ();
				}
			}
		}

		public bool IsSelectionValid {
			get { return selectedPrim != null; }
		}

		public PrimSelector Selection {
			get { return selectedPrim; }
		}

		public CountLayerContext (CCContext ccContext)
		{
			this.ccContext = ccContext;

			this.unitSphereVertices = GLHelper.CreateSphereVertices(Vector2.Zero, 1.0f, true);
		}

		public void OnImageUpdated ()
		{
			bUpdateImage = true;
		}

		public void ReqestSaveImage ()
		{
			bRequestSaveImage = true;
			ccContext.RequestRender();
		}

		public void SetROI (Vector2 center, float radius)
		{
			roiSphere.center = center;
			roiSphere.radius = radius;
			
			bUpdateROI = true;
			
			ccContext.RequestRender();
		}

		// Should be called from GL thread
		public void RenderUpdate (RectangleF rect)
		{	
			//if (bUpdateImage) {
			//	bUpdateImage = false;

			//	Console.WriteLine("CountLayerContext UPDATE IMAGE");

			//	CCContext.setupTexture (maskTexName, ccContext.ImageSize, IntPtr.Zero);

			//	int[] oldViewPort = new int[4];
			//	GL.GetInteger (GetPName.Viewport, oldViewPort);
			//	GL.Viewport (0, 0, ccContext.ImageSize.Width, ccContext.ImageSize.Height);
			//	int fboOld = GLHelper.bindFrameBuffer (frameBuffer, maskTexName);
				
			//	// Clear texture
			//	GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
			//	GL.Clear (ClearBufferMask.ColorBufferBit);

			//	// unbind framebuffer
			//	GL.BindFramebuffer (FramebufferTarget.Framebuffer, fboOld);
			//	GL.Viewport (oldViewPort [0], oldViewPort [1], oldViewPort [2], oldViewPort [3]);

			//	bUpdateROI = true;
			//	bUpdateROIThreshold = true;
			//	bUpdateSeqmentation = true;
			//}


			//if (bUpdateROI) {
			//	bUpdateROI = false;
			//	Console.WriteLine("CountLayerContext UPDATE ROI");
			//	UpdateROI();
			//	bUpdateROIThreshold = true;
			//}

			List<Tag> tagsCopy = null;
			lock(tags) {
				tagsCopy = new List<Tag>(tags);
			}

			//if (bLateUpdateSegmentation) {
				
			//	bLateUpdateSegmentation = false;
			//	Console.WriteLine("CountLayerContext LATE UPDATE SEGMENTATION");

			//	// End running calc region task 
			//	if (cCalcRegionTokenSource != null) {
			//		cCalcRegionTokenSource.Cancel ();
			//		calcRegionTask = null;
			//	}

			//	if(bUpdateROIThreshold && tagsCopy.Count == 0)
			//	{
			//		CountLayerHelper.GetROIThreshold(roiRect, segmentationPixelData, roiThresholdMin, roiThresholdMax, frameBuffer, roiTexName);
			//		bUpdateROIThreshold = false;
			//	}

			//	CountLayerHelper.UpdateThresholds (tagsCopy, roiRect, frameBuffer, roiTexName);

			//	GLHelper.GetError ();

			//	// Stop region task
			//	CountLayerHelper.UpdateSegmentation (tagsCopy, roiRect, frameBuffer, thresholdTexName, roiTexName, 
			//	                             segmentationPixelData, roiThresholdMin, roiThresholdMax);

			//	GLHelper.GetError ();
			
			//	// Start calc region task
			//	regions.Clear();
			//	incommingRegions.Clear();
			//	CountLayerHelper.SCalcRegionTask calcRegionTaskData = new CountLayerHelper.SCalcRegionTask ();
			//	calcRegionTaskData.pixelData = segmentationPixelData;
			//	calcRegionTaskData.roiRect = roiRect;
			//	calcRegionTaskData.tags = tagsCopy.ToArray();
			//	calcRegionTaskData.debugTexture = debugTexture;
			//	calcRegionTaskData.debugTexture.Clear ();
			//	calcRegionTaskData.bDebugDrawEDM = false;
			//	calcRegionTaskData.bDebugDrawRegions = false;
			//	calcRegionTaskData.startTime = DateTime.Now;
			//	calcRegionTaskData.regions = new List<Region>();
			//	calcRegionTaskData.regionAdded += delegate(object sender, EventArgs e) {

			//		lock(incommingRegions) {
			//			incommingRegions.Add (sender as Region);
			//		};
			//		ccContext.RequestRender ();
			//		//					if (CountLayerUpdated != null) {
			//		//						CountLayerUpdated (tags.Count, regions.Count);
			//		//					}
			//	};

			//	cCalcRegionTokenSource = new CancellationTokenSource ();
			//	CancellationToken cToken = cCalcRegionTokenSource.Token; 
				
			//	calcRegionTask = Task.Factory.StartNew<CountLayerHelper.SCalcRegionTask> (() => CountLayerHelper.calcRegions (cToken, calcRegionTaskData), cToken);
			//	calcRegionTask.ContinueWith (task => { 
			//		// Request render so we can get task result in render thread
			//		ccContext.RequestRender ();
			//	}); 
			//}

			//// Handle incomming regions
			//lock(incommingRegions) {
			//	if(incommingRegions.Count > 0) {
			//		regions.AddRange(incommingRegions);
			//		incommingRegions.Clear();
			//	}
			//};


			//// Trigger segmentation update next frame
			//if(bUpdateSeqmentation) {
			//	bUpdateSeqmentation = false;
			//	bLateUpdateSegmentation = true;
			//	Console.WriteLine("CountLayerContext UPDATE SEGMENTATION");
			//	ccContext.RequestRender ();
			//}
			
			//// Check calc region task
			//if (calcRegionTask != null && calcRegionTask.IsCompleted) { //calcRegionTaskComplete)
			//	CountLayerHelper.SCalcRegionTask taskData = calcRegionTask.Result;

			//	ICollection<Region> result = taskData.regions;

			//	TimeSpan span = DateTime.Now.Subtract(taskData.startTime);
			//	Console.WriteLine("CountLayerContext REGION RESULT (duration:"+span.TotalSeconds+")");
				
			//	regions.Clear ();
			//	regions.AddRange (result);
			//	calcRegionTask = null;

			//	if (CountLayerUpdated != null) {
			//		CountLayerUpdated (tagsCopy.Count, regions.Count);
			//	}
			//}

            // RENDERING !!!

            // Clear background
            GLHelper.GetError();
            GL.ClearColor(1.0f, 0.0f, 0.0f, 1);
            GLHelper.GetError();
            GL.Clear (ClearBufferMask.ColorBufferBit);
            GLHelper.GetError();

            Matrix4 mat = ccContext.ModelViewMatrix * ccContext.ProjectionMatrix;

			// Draw image
			{
				Rectangle imageRect = new Rectangle (0, 0, (int)ccContext.ImageSize.Width, (int)ccContext.ImageSize.Height);
				GLHelper.DrawSprite (ccContext.ImageTexName, imageRect, mat);
			}
		
			//// Draw ROI
			//{
			//	Vector4 outlineColor = ColorHelper.ColorToVec(ColorHelper.ROIEdge);
			//	Vector4 bgColor = ColorHelper.ColorToVec(ColorHelper.ROIBackground);
			//	float outlineWidth = 1.0f;
			//	if (activeTool == Tool.ROIEdit) {
			//		bool selected = selectedPrim != null && selectedPrim.GetPrimitive () == roiSphere;
			//		outlineColor = selected ? ColorHelper.ColorToVec(ColorHelper.ROIEdgeSelected) : ColorHelper.ColorToVec(ColorHelper.ROIEdgeActive);
			//		bgColor = ColorHelper.ColorToVec(ColorHelper.ROIBackgroundActive);
			//		outlineWidth = 2;
			//	}
			//	CountLayerHelper.DrawROI (rect, roiSphere.center, roiSphere.radius, roiRect, roiMaskTexName, outlineColor, outlineWidth, bgColor, ccContext.ModelViewMatrix, ccContext.ProjectionMatrix);
			//}

//			// Draw debug texture
//			bool bDragDebugTexture = false;
//			if (bDragDebugTexture) {
//				debugTexture.prepareForRender ();
////				GLHelper.DrawSprite(debugTexName, roiRect, mat);
////				GLHelper.DrawSprite(roiTexName, roiRect, mat);
//				GLHelper.DrawSprite (roiMaskTexName, roiRect, mat);
//			}

			//{
			//	CountLayerHelper.DrawRegions (regions, ccContext.ModelViewMatrix, ccContext.ProjectionMatrix);
			//}

			// Draw tags
			{
				Tag selectedTag = selectedPrim != null ? selectedPrim.GetPrimitive () as Tag : null;
				bool layerActive = activeTool == Tool.TagCreate || activeTool == Tool.TagDelete;
				float tagSize = ccContext.TagSize;
				CountLayerHelper.drawTags (tagsCopy, selectedTag, layerActive, ccContext.ModelViewMatrix, ccContext.ProjectionMatrix, tagSize);
			}

			if (bRequestSaveImage) {
				bRequestSaveImage = false;
				CreateSaveImage(tagsCopy);
			}
		}

		public void SetupGL ()
		{
            // get an id from OpenGL
            GLHelper.GetError();
            GL.GenTextures(1, out roiTexName);
			GL.GenTextures(1, out roiMaskTexName);
			GL.GenTextures(1, out maskTexName);
			GL.GenTextures(1, out thresholdTexName);
			GL.GenTextures(1, out debugTexName);
			GL.GenFramebuffers( 1, out frameBuffer );
            GLHelper.GetError();

            debugTexture = new MutableTexture(debugTexName);
		}

		public void OnTapGesture (Vector2 posView)
		{
			float touchRange = ccContext.SelectRadius;

			Vector2 pos = CCContext.ViewToModel (posView, ccContext.ModelViewMatrix);

			// Mask draw and erase
			if (activeTool == Tool.MaskDraw || activeTool == Tool.MaskErase) {
				bool foreground = activeTool == Tool.MaskDraw;
				CountLayerHelper.MaskDraw(pos, pos, foreground, frameBuffer, maskTexName, ccContext.ImageSize, 
				                          unitSphereVertices, ccContext.DrawRadius);

				bUpdateROI = true;
				bUpdateSeqmentation = true;
				ccContext.RequestRender();
				return;
			}

			// Tap pos in count view spacew
			float radius = CCContext.ViewToModel (touchRange ,ccContext.ModelViewMatrix);
			switch (ccContext.CountLayerContext.ActiveTool) {
			case CountLayerContext.Tool.TagCreate:
			{
				// Make sure tag is inside ROI
				if((pos - roiSphere.center).Length < roiSphere.radius) {
					tags.Add(new Tag(pos));
					bUpdateSeqmentation = true;
					ccContext.RequestRender();
				}

				break;
			}
			case CountLayerContext.Tool.TagDelete:
			{
				TagSelector selector = TagSelector.Select(tags, pos, radius);

				if(selector != null)
				{
					Tag tag = selector.GetPrimitive() as Tag;
					tags.Remove(tag);
					bUpdateSeqmentation = true;
					ccContext.RequestRender();
				}
				break;
			}
			}
		}

		public bool OnSelect (Vector2 posView)
		{
			float touchRange = ccContext.SelectRadius;

			Vector2 p = CCContext.ViewToModel (posView, ccContext.ModelViewMatrix);
			float radius = CCContext.ViewToModel (touchRange, ccContext.ModelViewMatrix);

			switch (activeTool) {
			case Tool.ROIEdit:
			{
				selectedPrim = SphereSelector.Select(roiSphere, p, radius, true, true);
				if(selectedPrim != null) {
					ccContext.RequestRender();
					return true;
				}
				break;
			}
			case Tool.TagCreate:
			case Tool.TagDelete:
			{
				selectedPrim = TagSelector.Select(tags, p, radius);
				if(selectedPrim != null)
				{
					ccContext.RequestRender();
					Console.WriteLine("selected:" + selectedPrim.GetPrimitive());
					return true;
				}
				break;
			}
			}

			return false;
		}

		public void OnUnselect ()
		{
			if (selectedPrim != null) {
				selectedPrim = null;

				switch (activeTool) {
				case Tool.MaskDraw:
				case Tool.MaskErase:
				case Tool.ROIEdit:
				{
					bUpdateROI = true;
					break;
				}
				case Tool.TagCreate:
				case Tool.TagDelete:
				{
					bUpdateSeqmentation = true;
					break;
				}
				}

				ccContext.RequestRender();
				return;
			}
		}
	
		public void OnDragStarted(Vector2 endPos, Vector2 translation) 
		{
			Vector2 vPos = CCContext.ViewToModel(endPos, ccContext.ModelViewMatrix);
			Vector2 vDelta;
			vDelta.X = CCContext.ViewToModel (translation.X, ccContext.ModelViewMatrix);
			vDelta.Y = CCContext.ViewToModel (translation.Y, ccContext.ModelViewMatrix);

			// Mask draw and erase
			if (activeTool == Tool.MaskDraw || activeTool == Tool.MaskErase) {

				lastMaskDrawPos = vPos - vDelta;
				bool foreground = activeTool == Tool.MaskDraw;
				CountLayerHelper.MaskDraw(vPos, lastMaskDrawPos, foreground, frameBuffer, maskTexName, ccContext.ImageSize
				                          , unitSphereVertices, ccContext.DrawRadius);
				lastMaskDrawPos = vPos;
				return;
			}

			OnDrag (endPos, translation);
		}

		public void OnDrag (Vector2 endPos, Vector2 translation)
		{
			Vector2 vPos = CCContext.ViewToModel(endPos, ccContext.ModelViewMatrix);

			// Mask draw and erase
			if (activeTool == Tool.MaskDraw || activeTool == Tool.MaskErase) {
				bool foreground = activeTool == Tool.MaskDraw;
				CountLayerHelper.MaskDraw(vPos, lastMaskDrawPos, foreground, frameBuffer, maskTexName, ccContext.ImageSize, 
				                          unitSphereVertices, ccContext.DrawRadius);
				lastMaskDrawPos = vPos;

				bUpdateROI = true;
				ccContext.RequestRender();

				return;
			}

			// Drag primitive 
			if (selectedPrim != null) {

				Vector2 vDelta;
				vDelta.X = CCContext.ViewToModel (translation.X, ccContext.ModelViewMatrix);
				vDelta.Y = CCContext.ViewToModel (translation.Y, ccContext.ModelViewMatrix);

				selectedPrim.DragTo(vPos, vDelta);
				ccContext.RequestRender();

				return;
			}

			// Pan view
			{
				Matrix4 trans = Matrix4.CreateTranslation (translation.X, translation.Y, 0);
				ccContext.ModelViewMatrix = ccContext.ModelViewMatrix * trans;
			}
		}	

		public void OnDragDone() 
		{
			if (activeTool == Tool.MaskDraw || activeTool == Tool.MaskErase ||  activeTool == Tool.ROIEdit)  {
				bUpdateROI = true;
				bUpdateSeqmentation = true;
				ccContext.RequestRender();
			}
		}

		public void OnPinch (Vector2 center, Vector2 translation, float scale)
		{
			Matrix4 mat = CCContext.CreateScaleAroundPoint(new Vector3(center.X, center.Y, 0), scale);
			ccContext.ModelViewMatrix = ccContext.ModelViewMatrix*mat;


			// Pan view
			{
				Matrix4 trans = Matrix4.CreateTranslation (translation.X, translation.Y, 0);
				ccContext.ModelViewMatrix = ccContext.ModelViewMatrix * trans;
			}

		}

		// **************************************************************************


		// Filter out everything that isnt in region of interest
		private void UpdateROI()
		{
			Vector2 center = roiSphere.center;
			float radius = roiSphere.radius;
			Rectangle newRect = new Rectangle((int)(center.X - radius),(int)(center.Y - radius), (int)( radius*2), (int)(radius*2));
			
			CCContext.setupTexture(roiMaskTexName, newRect.Size, IntPtr.Zero);
			CCContext.setupTexture(roiTexName, newRect.Size, IntPtr.Zero);
			CCContext.setupTexture(thresholdTexName, newRect.Size, IntPtr.Zero);
			
//			int channels = 4;
//				segmentationPixelData = new byte[(int)(newRect.Size.Width*newRect.Size.Height*channels)]; 
			segmentationPixelData = new uint[(int)(newRect.Size.Width*newRect.Size.Height)]; 

			debugTexture.Size = newRect.Size;

			roiRect = newRect;
			
			CountLayerHelper.updateROIMask(roiRect, ccContext.ImageSize, frameBuffer, roiMaskTexName, maskTexName);
			CountLayerHelper.updateROIImage(roiRect, frameBuffer, ccContext.ImageTexName, ccContext.ImageSize, roiTexName, roiMaskTexName);
		}

		private void CreateSaveImage(ICollection<Tag> tagArray) 
		{
			if (SaveCountResult == null) {
				return;
			}

			Size size = ccContext.ImageSize;
			uint outputTexName = 0;

			// Generate texture
			GL.GenTextures(1, out outputTexName);
			CCContext.setupTexture(outputTexName, size, IntPtr.Zero);

			// Setup viewport
			int[] oldViewPort = new int[4];
			GL.GetInteger(GetPName.Viewport, oldViewPort);
			GL.Viewport(0, 0, size.Width, size.Height);
			int fboOld = GLHelper.bindFrameBuffer(frameBuffer, outputTexName);

			GL.Disable (EnableCap.Blend);

			Matrix4 projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, (float)size.Width, 0, (float)size.Height, -1.0f, 1.0f);
//			Matrix4 projectionMatrix = ccContext.ProjectionMatrix;
			Matrix4 modelViewMatrix = Matrix4.Identity;

			// Draw image
			{
				Matrix4 mat = modelViewMatrix * projectionMatrix;
				Rectangle imageRect = new Rectangle (0, 0, (int)size.Width, (int)size.Height);
				GLHelper.DrawSprite (ccContext.ImageTexName, imageRect, mat);
			}
			
			// Draw ROI
			Vector4 outlineColor = ColorHelper.ColorToVec(ColorHelper.ROIEdge);
			Vector4 bgColor = ColorHelper.ColorToVec(ColorHelper.ROIBackground);
			float outlineWidth = 1.0f;
			RectangleF rect = new RectangleF (PointF.Empty, new SizeF(size));
			CountLayerHelper.DrawROI (rect, roiSphere.center, roiSphere.radius, roiRect, roiMaskTexName, outlineColor, outlineWidth, bgColor, modelViewMatrix, projectionMatrix);
			
			// Regions
			CountLayerHelper.DrawRegions (regions, modelViewMatrix, projectionMatrix);
			
			// Draw tags
			{
				float tagSize = 16;
				CountLayerHelper.drawTags (tagArray, null, true, modelViewMatrix, projectionMatrix, tagSize);
			}
			 
			// allocate array and read pixels into it.
			int myDataLength = size.Width * size.Height * 4;
			byte[] buffer = new byte[myDataLength];
			GL.ReadPixels(0, 0, size.Width, size.Height, PixelFormat.Rgba, PixelType.UnsignedByte, buffer);

			// Call save image event
			SaveCountResult(buffer, size, tags.Count, regions.Count);
			
			buffer = null;

			GL.DeleteTextures(1, ref outputTexName);
			
			// unbind framebuffer
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboOld);
			GL.Viewport(oldViewPort[0], oldViewPort[1], oldViewPort[2], oldViewPort[3]);
		}
	}
}

