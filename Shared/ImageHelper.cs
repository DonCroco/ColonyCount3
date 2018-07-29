using System;

namespace CC.Core
{
	public class ImageHelper
	{
		public static int getPixel(byte[] image, int width, int x, int y)
		{
			int index = y*width + x;
			return image[index] & 0xFF;
		}
		
		public static int getPixel(ushort[] image, int width, int x, int y)
		{
			int index = y*width + x;
			return (short)image[index];
		}
		
		public static int getPixel(int[] image, int width, int x, int y)
		{
			int index = y*width + x;
			return image[index];
		}
	}
}

