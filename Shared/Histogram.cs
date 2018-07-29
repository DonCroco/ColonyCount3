using System;
using System.Drawing;

namespace CC.Core
{
	public class Histogram
	{
		public static void FillGaps(float[] histogram) {

			float lastCount = 0;
			int lastCountIndex = 0;
			for (int i = 0; i < 256; i++)
			{
				int count = (int)histogram[i];
				if(count > 0)
				{
					// Pad with interpolate values so there are no "holes" in the histogram
					int col0 = lastCountIndex;
					int col1 = i;
					
					int deltaCol = col1 - col0;
					if(deltaCol > 1)
					{
						float val0 = lastCount;
						float val1 = count;
						
						int iCol = col0 + 1;
						float fDeltaVal =  (float)(val1 - val0)/(float)deltaCol;
						float fVal = (float)val0 + fDeltaVal;
						while(iCol < col1)
						{
							histogram[iCol] = fVal;
							fVal += fDeltaVal;
							iCol ++;
						}
					}
					
					// update last
					lastCount = count;
					lastCountIndex = i;
				}
			}
		}

		public static void loadHistogram(byte[] imageMap, int length, int[] histogram)
		{
			for (int i = 0; i < length; i++) 
			{
				int iVal = imageMap[i] & 0xff;
				histogram[iVal] ++;
			}
		}
	}
}

