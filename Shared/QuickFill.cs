
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;

namespace CC.Core
{
	public class QuickFill<T> 
	{
		// Represents a linear range to be filled and branched from.
		private class FloodFillRange
		{
			public int startX;
			public int endX;
			public int Y;
			
			public FloodFillRange(int startX, int endX, int y)
			{
				this.startX = startX;
				this.endX = endX;
				this.Y = y;
			}
		}  

		// Fills the specified point on the bitmap with the currently selected fill color.
		// int x, int y: The starting coords for the fill
		public static void FloodFill(Point pos, T[] pixels, Size size, T readColor, T writeColor, ICollection<Point> points)
		{
			Queue<FloodFillRange> ranges = new Queue<FloodFillRange>();
			
			//***Do first call to floodfill.
			LinearFill(pos.X, pos.Y , pixels, size.Width, readColor, writeColor, ranges, points);
			
			//***Call floodfill routine while floodfill ranges still exist on the queue
			FloodFillRange range;
			while (ranges.Count > 0)
			{
				//**Get Next Range Off the Queue
				range = ranges.Dequeue();
				
				//**Check Above and Below Each Pixel in the Floodfill Range
				int downPxIdx = (size.Width * (range.Y + 1)) + range.startX;
				int upPxIdx = (size.Width * (range.Y - 1)) + range.startX;
				int upY = range.Y - 1;//so we can pass the y coord by ref
				int downY = range.Y + 1;
				for (int i = range.startX; i <= range.endX; i++)
				{
					//*Start Fill Upwards
					//if we're not above the top of the bitmap and the pixel above this one is within the color tolerance
					if (range.Y > 0 && CheckPixel(pixels,upPxIdx, readColor))
						LinearFill(i,  upY, pixels, size.Width, readColor, writeColor, ranges, points);
					
					//*Start Fill Downwards
					//if we're not below the bottom of the bitmap and the pixel below this one is within the color tolerance
					if (range.Y < (size.Height - 1) && CheckPixel(pixels, downPxIdx, readColor))
						LinearFill(i,  downY, pixels, size.Width, readColor, writeColor, ranges, points);
					downPxIdx++;
					upPxIdx++;
				}
				
			}
		}

		// Finds the furthermost left and right boundaries of the fill area
		// on a given y coordinate, starting from a given x coordinate, filling as it goes.
		// Adds the resulting horizontal range to the queue of floodfill ranges,
		// to be processed in the main loop.
		//
		// int x, int y: The starting coords
		private static void LinearFill(int x, int y, T[] pixels, int width, 
		                               T readColor, T writeColor, Queue<FloodFillRange> ranges, ICollection<Point> points)
		{
			T testColor = readColor;
			
			//***Find Left Edge of Color Area
			int lFillLoc = x; //the location to check/fill on the left
			int pxIdx = (width * y) + x;
			while (true)
			{
				//**fill with the color
				if(!AddPixel(pixels, pxIdx, (short)lFillLoc,(short)y, writeColor, points))
					return;
				
				//**de-increment
				lFillLoc--;     //de-increment counter
				pxIdx--;        //de-increment pixel index
				//**exit loop if we're at edge of bitmap or color area
				if (lFillLoc < 0 || !CheckPixel(pixels, pxIdx, testColor))
					break;
			}
			lFillLoc++;
			
			//***Find Right Edge of Color Area
			int rFillLoc = x; //the location to check/fill on the left
			pxIdx = (width * y) + x;
			while (true)
			{
				//**fill with the color
				if(!AddPixel(pixels ,pxIdx, (short)rFillLoc,(short)y, writeColor, points))
					return;
				
				//**increment
				rFillLoc++;     //increment counter
				pxIdx++;        //increment pixel index
				//**exit loop if we're at edge of bitmap or color area
				if (rFillLoc >= width || !CheckPixel(pixels, pxIdx, testColor))
					break;
			}
			rFillLoc--;
			
			//add range to queue
			FloodFillRange r = new FloodFillRange(lFillLoc, rFillLoc, y);
			ranges.Enqueue(r);
		}


		private static bool CheckPixel(T[] pixels, int index, T testColor) {
			return pixels[index].Equals(testColor);
		}

		private static bool AddPixel(T[] pixels, int index, short x, short y, T writeColor, ICollection<Point> points) {
			pixels[index] = writeColor;
			points.Add(new Point(x,y));
			return true;
		}
	}
}

