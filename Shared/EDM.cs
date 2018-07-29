using System;

namespace CC.Core
{
	public class EDM
	{
		public const int ONE = 41;	// One unit distance
		public const int SQRT2 = 58; // ~ 41 * sqrt(2)
		public const int SQRT5 = 92; // ~ 41 * sqrt(5)	

		/**
	 * Convert a binary map into Euclidean Distance Map (EDM).  
	 *
	 * @param  edm binary map (initial values 0 or Short.MAX_VALUE) that is converted to EDM
	 * @param  width width of map
	 * @param  height height of map
	 */
		static public void createEDM(ushort[] edm, int width, int height) 
		{
			int rowsize = width;
			int xmax    = width - 2;
			int ymax    = height - 2;
			int offset;
			for (int y=0; y<height; y++) {
				for (int x=0; x<width; x++) {
					offset = x + y * rowsize;
					if (edm[offset] > 0) 
					{
						if ((x<=1) || (x>=xmax) || (y<=1) || (y>=ymax))
							setEdgeValue(offset, rowsize, edm, x, y, xmax, ymax);
						else
							setValue(offset, rowsize, edm);
					}
				} // for x
			} // for y
			
			for (int y=height-1; y>=0; y--) {
				for (int x=width-1; x>=0; x--) {
					offset = x + y * rowsize;
					if (edm[offset] > 0) {
						if ((x<=1) || (x>=xmax) || (y<=1) || (y>=ymax))
							setEdgeValue(offset, rowsize, edm, x, y, xmax, ymax);
						else
							setValue(offset, rowsize, edm);
					}
				} // for x
			} // for y
		}	 
		
		static void setValue(int offset, int rowsize, ushort[] image16) {
			int  v;
			int r1  = offset - rowsize - rowsize - 2;
			int r2  = r1 + rowsize;
			int r3  = r2 + rowsize;
			int r4  = r3 + rowsize;
			int r5  = r4 + rowsize;
			int min = 32767;
			
			v = image16[r2 + 2] + ONE;
			if (v < min)
				min = v;
			v = image16[r3 + 1] + ONE;
			if (v < min)
				min = v;
			v = image16[r3 + 3] + ONE;
			if (v < min)
				min = v;
			v = image16[r4 + 2] + ONE;
			if (v < min)
				min = v;
			
			v = image16[r2 + 1] + SQRT2;
			if (v < min)
				min = v;
			v = image16[r2 + 3] + SQRT2;
			if (v < min)
				min = v;
			v = image16[r4 + 1] + SQRT2;
			if (v < min)
				min = v;
			v = image16[r4 + 3] + SQRT2;
			if (v < min)
				min = v;
			
			v = image16[r1 + 1] + SQRT5;
			if (v < min)
				min = v;
			v = image16[r1 + 3] + SQRT5;
			if (v < min)
				min = v;
			v = image16[r2 + 4] + SQRT5;
			if (v < min)
				min = v;
			v = image16[r4 + 4] + SQRT5;
			if (v < min)
				min = v;
			v = image16[r5 + 3] + SQRT5;
			if (v < min)
				min = v;
			v = image16[r5 + 1] + SQRT5;
			if (v < min)
				min = v;
			v = image16[r4] + SQRT5;
			if (v < min)
				min = v;
			v = image16[r2] + SQRT5;
			if (v < min)
				min = v;
			
			image16[offset] = (ushort)min;
			
		} // setValue()
		
		static void setEdgeValue(int offset, int rowsize, ushort[] image16, int x, int y, int xmax, int ymax) {
			int  v;
			int r1 = offset - rowsize - rowsize - 2;
			int r2 = r1 + rowsize;
			int r3 = r2 + rowsize;
			int r4 = r3 + rowsize;
			int r5 = r4 + rowsize;
			int min = 32767;
			int offimage = image16[r3 + 2];
			
			if (y<1)
				v = offimage + ONE;
			else
				v = image16[r2 + 2] + ONE;
			if (v < min)
				min = v;
			
			if (x<1)
				v = offimage + ONE;
			else
				v = image16[r3 + 1] + ONE;
			if (v < min)
				min = v;
			
			if (x>xmax)
				v = offimage + ONE;
			else
				v = image16[r3 + 3] + ONE;
			if (v < min)
				min = v;
			
			if (y>ymax)
				v = offimage + ONE;
			else
				v = image16[r4 + 2] + ONE;
			if (v < min)
				min = v;
			
			if ((x<1) || (y<1))
				v = offimage + SQRT2;
			else
				v = image16[r2 + 1] + SQRT2;
			if (v < min)
				min = v;
			
			if ((x>xmax) || (y<1))
				v = offimage + SQRT2;
			else
				v = image16[r2 + 3] + SQRT2;
			if (v < min)
				min = v;
			
			if ((x<1) || (y>ymax))
				v = offimage + SQRT2;
			else
				v = image16[r4 + 1] + SQRT2;
			if (v < min)
				min = v;
			
			if ((x>xmax) || (y>ymax))
				v = offimage + SQRT2;
			else
				v = image16[r4 + 3] + SQRT2;
			if (v < min)
				min = v;
			
			if ((x<1) || (y<=1))
				v = offimage + SQRT5;
			else
				v = image16[r1 + 1] + SQRT5;
			if (v < min)
				min = v;
			
			if ((x>xmax) || (y<=1))
				v = offimage + SQRT5;
			else
				v = image16[r1 + 3] + SQRT5;
			if (v < min)
				min = v;
			
			if ((x>=xmax) || (y<1))
				v = offimage + SQRT5;
			else
				v = image16[r2 + 4] + SQRT5;
			if (v < min)
				min = v;
			
			if ((x>=xmax) || (y>ymax))
				v = offimage + SQRT5;
			else
				v = image16[r4 + 4] + SQRT5;
			if (v < min)
				min = v;
			
			if ((x>xmax) || (y>=ymax))
				v = offimage + SQRT5;
			else
				v = image16[r5 + 3] + SQRT5;
			if (v < min)
				min = v;
			
			if ((x<1) || (y>=ymax))
				v = offimage + SQRT5;
			else
				v = image16[r5 + 1] + SQRT5;
			if (v < min)
				min = v;
			
			if ((x<=1) || (y>ymax))
				v = offimage + SQRT5;
			else
				v = image16[r4] + SQRT5;
			if (v < min)
				min = v;
			
			if ((x<=1) || (y<1))
				v = offimage + SQRT5;
			else
				v = image16[r2] + SQRT5;
			if (v < min)
				min = v;
			
			image16[offset] = (ushort)min;
			
		} // setEdgeValue()
	}
}

