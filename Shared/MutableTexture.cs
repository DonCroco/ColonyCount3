using System;
using System.Collections;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.ES30;

namespace CC.Core
{
	public class MutableTexture
	{
		uint texName;
		byte[] pixelData;
		int maxPixelCount;
		Size size;
		bool texLoadRequired;

		public MutableTexture(uint texName)
		{
			this.texName =texName;
			maxPixelCount = 0;
			pixelData = null;
			texLoadRequired = true;
		}

		public Size Size {
			get { return size; }
			set { 
				if(size != value) {
					size = value;
					allocPixels(size.Width*size.Height);
				}
			}
		}

		public void Clear ()
		{
			Array.Clear(pixelData,0, pixelData.Length);
		}

		public void SetPixel (int index, int R, int G, int B, int A)
		{
			pixelData[index++] = (byte)(R & 0xFF);
			pixelData[index++] = (byte)(G & 0xFF);
			pixelData[index++] = (byte)(B & 0xFF);
			pixelData[index++] = (byte)(A & 0xFF);
			
			texLoadRequired = true;
		}

		public void SetPixel (int x, int y, int R, int G, int B, int A)
		{
			int index = (y * size.Width + x) * 4;

			pixelData[index++] = (byte)(R & 0xFF);
			pixelData[index++] = (byte)(G & 0xFF);
			pixelData[index++] = (byte)(B & 0xFF);
			pixelData[index++] = (byte)(A & 0xFF);
			
			texLoadRequired = true;
		}
	
		
		public void prepareForRender()
		{
			if(texLoadRequired)
			{
				loadTexture(texName, pixelData, size);
				texLoadRequired = false;
			}
		}


		//***********************************************
		

		private void PixedDataChanged()
		{
			texLoadRequired = true;
		}

		private static void loadTexture(uint texId, byte[] data, Size size)
		{
			GL.BindTexture(TextureTarget.Texture2D, texId);
			
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) All.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) All.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) All.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) All.ClampToEdge);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)size.Width, (int)size.Height, 0,
			              PixelFormat.Rgba, PixelType.UnsignedByte, data);
		}
		
		private void allocPixels(int count)
		{
			if(count < maxPixelCount && pixelData == null)
			{
				return;
			}
			pixelData = new byte[count*4];
			maxPixelCount = count;
		}

	}
}

