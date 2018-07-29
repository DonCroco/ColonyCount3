using System;
using System.Collections.Generic;

namespace CC.Core
{
	public class Segmentation
	{
		/** the following constants are used to set bits corresponding to pixel types */
		public const byte MAXIMUM = (byte)1;			// marks local maxima (irrespective of noise tolerance)
		public const byte LISTED = (byte)2;             // marks points currently in the list
		public const byte PROCESSED = (byte)4;          // marks points processed previously
		public const byte MAX_AREA = (byte)8;           // marks areas near a maximum, within the tolerance
		public const byte EQUAL = (byte)16;             // marks contigous maximum points of equal level
		public const byte MAX_POINT = (byte)32;         // marks a single point standing for a maximum
		public const byte ELIMINATED = (byte)64;        // marks maxima that have been eliminated before watershed
		
		/** what type of output to create was chosen in the dialog (see constants below)*/
		
		public const int SINGLE_POINTS=0;		/** Output type single points */
		public const int IN_TOLERANCE=1;			/** Output type all points around the maximum within the tolerance */
		public const int SEGMENTED=2;			/** Output type watershed-segmented image */
		public const int POINT_SELECTION=3;		/** Do not create image, only mark points */
		public const int COUNT=4;				/** Do not create an image, just count maxima and add count to Results table*/
		public static int outputType = SEGMENTED;

		const int IS_LINE=1;                     // a point on a line (as a return type of isLineOrDot)
		const int IS_DOT=2;                      // an isolated point (as a return type of isLineOrDot)	
		
		static int[] dirXoffset = {    0,      1,     		1,     	1,        0,     -1,      -1,    -1    };
		static int[] dirYoffset = {   -1,     	-1,     	0,     	1,        1,      1,       0,    -1,   };

		public class MaxPoint : IComparable {
			public float value;
			public short x;
			public short y;
			
			/** a constructor filling in the data */
			public MaxPoint(short x, short y, float value) 
			{
				this.x = x;
				this.y = y;
				this.value = value;
			}
			
			/** a comparator required for sorting (interface Comparable) */
			public int CompareTo(Object o) 
			{
				//return Float.compare(value,((MaxPoint)o).value); //not possible since this requires Java 1.4
				float diff = value-((MaxPoint)o).value;
				if (diff > 0f) return 1;
				else if (diff == 0f) return 0;
				else return -1;
			}
		} 
		
		public static string GetMaxMapString (int val)
		{
			string s = "";
			s = (val & MAXIMUM) > 0 ? s + "MAXIMUM," : s;
			s = (val & LISTED) > 0 ? s + "LISTED," : s;
			s = (val & PROCESSED) > 0 ? s + "PROCESSED," : s;
			s = (val & MAX_AREA) > 0 ? s + "MAX_AREA," : s;
			s = (val & EQUAL) > 0 ? s + "EQUAL," : s;
			s = (val & MAX_POINT) > 0 ? s + "MAX_POINT," : s;
			s = (val & ELIMINATED) > 0 ? s + "ELIMINATED," : s;
			return s;
		}

		private static int[] CreateDirOffset (int width)
		{
			int[] dirOffset = { -width, 	-width+1, 	+1, 	+width+1, +width, +width-1,   -1, -width-1 };
			return dirOffset;
		}

		
		//		public MaxPoint[] getMaxPoints()
//		{
//			return maxPoints;
//		}
		
		/** returns whether the neighbor in a given direction is within the image
     * NOTE: it is assumed that the pixel x,y itself is within the image!
     * Uses class variables width, height: dimensions of the image
     * @param x         x-coordinate of the pixel that has a neighbor in the given direction
     * @param y         y-coordinate of the pixel that has a neighbor in the given direction
     * @param direction the direction from the pixel towards the neighbor (see makeDirectionOffsets)
     * @return          true if the neighbor is within the image (provided that x, y is within)
     */
		static bool isWithin(int width, int height, int x, int y, int direction) 
		{
			int xmax = width - 1;
			int ymax = height -1;
			switch(direction) {
			case 0:
				return (y>0);
			case 1:
				return (x<xmax && y>0);
			case 2:
				return (x<xmax);
			case 3:
				return (x<xmax && y<ymax);
			case 4:
				return (y<ymax);
			case 5:
				return (x>0 && y<ymax);
			case 6:
				return (x>0);
			case 7:
				return (x>0 && y>0);
			}
			return false;   
		} // isWithin	


		static bool isOnEdge (int width, int height, int x, int y)
		{
			int xmax = width-1;
			int ymax = height-1;
			return x==0 || y==0 || x==xmax || y==ymax;
		}

//		static int trueEdmHeight2(ushort[] inputMap, int width, int x, int y, int[] dirOffset ) {
//			int index = y*width + x;
//			int v = inputMap[index];
//			if (v==0) 
//				return v;                               //don't recalculate for edge pixels or background
//
//			int one = EDM.ONE;
//			int sqrt2 = EDM.SQRT2;
//			//            int trueH = v+sqrt2/2;                  //true height can never by higher than this
//			int trueH = v+(sqrt2 >> 1);                  //true height can never by higher than this
//			bool ridgeOrMax = false;
//			for (int d=0; d<4; d++) {               //for all directions halfway around:
//				int d2 = (d+4)%8;                   //get the opposite direction and neighbors
//				int v1 = inputMap[index + dirOffset[d]];
//				int v2 = inputMap[index + dirOffset[d2]];
//				int h;
//
//				if (v>=v1 && v>=v2) {
//					ridgeOrMax = true;
//					//                    h = (v1 + v2)/2;
//					h = (v1 + v2) >> 1;
//				} else {
//					//                    h = Math.min(v1, v2);
//					h = v1 < v2 ? v1 : v2;
//				}
//				h += (d%2==0) ? one : sqrt2;        //in diagonal directions, distance is sqrt2
//				if (trueH > h) trueH = h;
//			}
//			if (!ridgeOrMax) trueH = v;
//			return trueH;
//		}

		static int trueEdmHeight(ushort[] inputMap, int width, int x, int y, int[] buffer ) {
			int index = y*width + x;
			int v = inputMap[index];
			if (v==0) 
				return v;                               //don't recalculate for edge pixels or background

			getNeighbourPixels(inputMap, width, index, buffer); 

			int one = EDM.ONE;
			int sqrt2 = EDM.SQRT2;
			//            int trueH = v+sqrt2/2;                  //true height can never by higher than this
			int trueH = v+(sqrt2 >> 1);                  //true height can never by higher than this
			bool ridgeOrMax = false;
			for (int d=0; d<4; d++) {               //for all directions halfway around:

				int v1 = buffer[d];
				int v2 = buffer[d+4];

				int h;
				
				if (v>=v1 && v>=v2) {
					ridgeOrMax = true;
					//                    h = (v1 + v2)/2;
					h = (v1 + v2) >> 1;
				} else {
					//                    h = Math.min(v1, v2);
					h = v1 < v2 ? v1 : v2;
				}
				h += (d%2==0) ? one : sqrt2;        //in diagonal directions, distance is sqrt2
				if (trueH > h) trueH = h;
			}
			if (!ridgeOrMax) trueH = v;
			return trueH;
		}

		static void getNeighbourPixels(ushort[] inputMap, int width, int index, int[] buffer) {
			buffer[7] = inputMap[index-width-1];
			buffer[0] = inputMap[index-width];	
			buffer[1] = inputMap[index-width+1];
			buffer[6] = inputMap[index-1];
			buffer[2] = inputMap[index+1];
			buffer[5] = inputMap[index+width-1];
			buffer[4] = inputMap[index+width];
			buffer[3] = inputMap[index+width+1];
		}

		static MaxPoint[] getSortedMaxPoints(ushort[] inputMap, int width, int height, byte[] maxMap, float globalMin, 
		                                     double threshold) 
		{
			int nMax = 0;  //counts local maxima
			int[] edmBuffer = new int[8];
//			List<MaxPoint> maxPoints = new List<MaxPoint>(128);

			int[] dirOffset = CreateDirOffset(width);
			for (int y=0, i=0; y<height; y++) 
			{        
				for (int x=0; x<width; x++, i++) 
				{      
					// For optimization we dont test outer 2 pixels (then we dont have to make outside checks in inner loops)
					if (x<=1 || x>=width-2 || y<=1 || y>=height-2) 
						continue;

					int index = y*width + x;
					float v = inputMap[index];
					if (v==globalMin) 
						continue;
					if (v<threshold) 
						continue;

					float vTrue = trueEdmHeight(inputMap, width, x, y, edmBuffer );  // for 16-bit EDMs, use interpolated ridge height
					bool isMax = true;
					/* check wheter we have a local maximum.
                  Note: For a 16-bit EDM, we need all maxima: those of the EDM-corrected values
                  (needed by findMaxima) and those of the raw values (needed by cleanupMaxima) */

					for (int d=0; d<8; d++) 
					{                         // compare with the 8 neighbor pixels
						float vNeighbor = inputMap[index + dirOffset[d]];
						if (vNeighbor > v) {
							float vNeighborTrue = trueEdmHeight(inputMap, width, x+dirXoffset[d], y+dirYoffset[d], edmBuffer);
							if(vNeighborTrue > vTrue) {
								isMax = false;
								break;
							}
						}
					}
					if (isMax) {
						nMax++;
						maxMap[i] = MAXIMUM;
//						maxPoints.Add(new MaxPoint((short)x, (short)y, vTrue));
					}
				} // for x
			} // for y
			

			List<MaxPoint> maxPoints = new List<MaxPoint>(nMax);
			for (int y=0, i=0; y < height; y++)           //enter all maxima into an array
				for (int x=0; x < width; x++, i++)
				if (maxMap[i]==MAXIMUM) {
					maxPoints.Add(new MaxPoint((short)x, (short)y, trueEdmHeight(inputMap, width, x, y, edmBuffer)));
				}
			maxPoints.Sort();
			return maxPoints.ToArray();
		} 
		
		/** Check all maxima in list maxPoints, mark type of the points in typeP
	    * @param ip             the image to be analyzed
	    * @param typeP          8-bit image, here the point types are marked by type: MAX_POINT, etc.
	    * @param maxPoints      input: a list of all local maxima, sorted by height
	    * @param isEDM          whether ip is a 16-bit Euclidian distance map
	    * @param globalMin      minimum pixel value in ip
	    * @param tolerance      minimum pixel value difference for two separate maxima
	    */   
		static void analyzeAndMarkMaxima(ushort[] inputMap, int width, int height, byte[] maxMap, MaxPoint[] maxPoints, 
		                          bool isEDM, float globalMin, double tolerance) 
		{
//			long time = System.currentTimeMillis();
			int[] dirOffset = CreateDirOffset(width);
			byte[] types = maxMap;
			int[] edmBuffer = new int[8];
			int nMax = maxPoints.Length;
			short[] xList = new short[width*height];    //here we enter points starting from a maximum
			short[] yList = new short[width*height];
			//        Vector xyVector = null;
			//        Roi roi = null;

			for (int iMax=nMax-1; iMax>=0; iMax--) 
			{   
				// TODO: thread interrupt test
//				//process all maxima now, starting from the highest
//				if (iMax%100 == 0 && Thread.currentThread().isInterrupted()) 
//					return;

				float v = maxPoints[iMax].value;
				if (v==globalMin) 
					break;
				int offset = maxPoints[iMax].x + width*maxPoints[iMax].y;
				if ((types[offset]&PROCESSED)!=0)       //this maximum has been reached from another one, skip it
					continue;
				xList[0] = maxPoints[iMax].x;           //we create a list of connected points and start the list
				yList[0] = maxPoints[iMax].y;           //  at the current maximum
				types[offset] |= (EQUAL|LISTED);        //mark first point as equal height (to itself) and listed
				int listLen = 1;                        //number of elements in the list
				int listI = 0;                          //index of current element in the list
				bool maxPossible = true;             //it may be a true maxmum
				double xEqual = xList[0];               //for creating a single point: determine average over the
				double yEqual = yList[0];               //  coordinates of contiguous equal-height points
				int nEqual = 1;                         //counts xEqual/yEqual points that we use for averaging
				do {
					offset = xList[listI] + width*yList[listI];
					for (int d=0; d<8; d++) {           //analyze all neighbors (in 8 directions) at the same level
						int offset2 = offset+dirOffset[d];
						if (isWithin(width, height, xList[listI], yList[listI], d) && (types[offset2]&LISTED)==0) {
							if ((types[offset2]&PROCESSED)!=0) {
								maxPossible = false;    //we have reached a point processed previously, thus it is no maximum now
								//if(xList[0]>510&&xList[0]<77)IJ.write("stop at processed neighbor from x,y="+xList[listI]+","+yList[listI]+", dir="+d);
								break;
							}
							int x2 = xList[listI]+dirXoffset[d];
							int y2 = yList[listI]+dirYoffset[d];
							float v2 = ImageHelper.getPixel(inputMap, width, x2, y2);
							if (isEDM && (v2 <=v-(float)tolerance)) {
								if(!isOnEdge(width,height, x2,y2)) {
									v2 = trueEdmHeight(inputMap, width, x2, y2, edmBuffer); //correcting for EDM peaks may move the point up
								}
							}
							if (v2 > v) {
								maxPossible = false;    //we have reached a higher point, thus it is no maximum
								//if(xList[0]>510&&xList[0]<77)IJ.write("stop at higher neighbor from x,y="+xList[listI]+","+yList[listI]+", dir,value,value2,v2-v="+d+","+v+","+v2+","+(v2-v));
								break;
							} else if (v2 >= v-(float)tolerance) {
								xList[listLen] = (short)(x2);
								yList[listLen] = (short)(y2);
								listLen++;              //we have found a new point within the tolerance
								types[offset2] |= LISTED;
								if (x2==0 || x2==width-1 || y2==0 || y2==height-1) {
									maxPossible = false;
									break;          //we have an edge maximum;
								}
								if (v2==v) {            //prepare finding center of equal points (in case single point needed)
									types[offset2] |= EQUAL;
									xEqual += x2;
									yEqual += y2;
									nEqual ++;
								}
							}
						} // if isWithin & not LISTED
					} // for directions d
					listI++;
				} while (listI < listLen);
				byte resetMask = (byte)~(maxPossible?(byte)LISTED:(byte)(LISTED|EQUAL));
				xEqual /= nEqual;
				yEqual /= nEqual;
				double minDist2 = 1e20;
				int nearestI = 0;
				for (listI=0; listI<listLen; listI++) {
					offset = xList[listI] + width*yList[listI];
					types[offset] &= resetMask;     //reset attributes no longer needed
					types[offset] |= PROCESSED;     //mark as processed
					if (maxPossible) {
						types[offset] |= MAX_AREA;
						if ((types[offset]&EQUAL)!=0) {
							double dist2 = (xEqual-xList[listI])*(xEqual-xList[listI]) + (yEqual-yList[listI])*(yEqual-yList[listI]);
							if (dist2 < minDist2) {
								minDist2 = dist2;   //this could be the best "single maximum" point
								nearestI = listI;
							}
						}
					}
				} // for listI
				if (maxPossible) 
				{
					types[xList[nearestI] + width*yList[nearestI]] |= MAX_POINT;
					
					//                if (displayOrCount && !(this.excludeOnEdges && isEdgeMaximum)) 
					//                {
					//                    if (xyVector==null) {
					//                        xyVector = new Vector();
					//                        roi = imp.getRoi();
					//                    }
					//                    short mpx = xList[nearestI];
					//                    short mpy = yList[nearestI];
					//                    if (roi==null || roi.contains(mpx, mpy))
					//                        xyVector.addElement(new MaxPoint(mpx, mpy, 0f));
					//                }
				}
			} 
			
			//        if (displayOrCount && xyVector!=null) {
			//            int npoints = xyVector.size();
			//            if (outputType == POINT_SELECTION) {
			//                int[] xpoints = new int[npoints];
			//                int[] ypoints = new int[npoints];
			//                MaxPoint mp;
			//                for (int i=0; i<npoints; i++) {
			//                    mp = (MaxPoint)xyVector.elementAt(i);
			//                    xpoints[i] = mp.x;
			//                    ypoints[i] = mp.y;
			//                }
			//                imp.setRoi(new PointRoi(xpoints, ypoints, npoints));
			//            }
			//            if (outputType==COUNT) {
			//                ResultsTable rt = ResultsTable.getResultsTable();
			//                rt.incrementCounter();
			//                rt.setValue("Count", rt.getCounter()-1, npoints);
			//                rt.show("Results");
			//            }
			//        }
			
			//        LogHelper.v("", "Segmentation.analyzeAndMarkMaxima " + (System.currentTimeMillis() - time)+ "ms");
		} //void analyzeAndMarkMaxima	 
		

		
		/** eliminate unmarked maxima for use by watershed. Starting from each previous maximum,
	     * explore the surrounding down to successively lower levels until a marked maximum is
	     * touched (or the plateau of a previously eliminated maximum leads to a marked maximum).
	     * Then set all the points above this value to this value
	     * @param outIp     the image containing the pixel values
	     * @param typeP     the types of the pixels are marked here
	     * @param maxPoints array containing the coordinates of all maxima that might be relevant
	     */    
		static void cleanupMaxima(byte[] outIp, int width, int height, byte[] typeP, MaxPoint[] maxPoints) {
			byte[] pixels = outIp;
			byte[] types = typeP;
			int nMax = maxPoints.Length;
			short[] xList = new short[width*height];
			short[] yList = new short[width*height];
			int[] dirOffset = CreateDirOffset(width);
			for (int iMax = nMax-1; iMax>=0; iMax--) {
				int offset = maxPoints[iMax].x + width*maxPoints[iMax].y;
				//if (maxPoints[iMax].x==122) IJ.write("max#"+iMax+" at x,y="+maxPoints[iMax].x+","+maxPoints[iMax].y+": type="+types[offset]);
				if ((types[offset]&(MAX_AREA|ELIMINATED))!=0) continue;
				int level = pixels[offset]&255;
				int loLevel = level+1;
				xList[0] = maxPoints[iMax].x;           //we start the list at the current maximum
				yList[0] = maxPoints[iMax].y;
				//if (xList[0]==122) IJ.write("max#"+iMax+" at x,y="+xList[0]+","+yList[0]+"; level="+level);
				types[offset] |= LISTED;                //mark first point as listed
				int listLen = 1;                        //number of elements in the list
				int lastLen = 1;
				int listI = 0;                          //index of current element in the list
				bool saddleFound = false;
				while (!saddleFound && loLevel >0) {
					loLevel--;
					lastLen = listLen;                  //remember end of list for previous level
					listI = 0;                          //in each level, start analyzing the neighbors of all pixels
					do {                                //for all pixels listed so far
						offset = xList[listI] + width*yList[listI];
						for (int d=0; d<8; d++) {       //analyze all neighbors (in 8 directions) at the same level
							int offset2 = offset+dirOffset[d];
							if (isWithin(width, height, xList[listI], yList[listI], d) && (types[offset2]&LISTED)==0) {
								if ((types[offset2]&MAX_AREA)!=0 || (((types[offset2]&ELIMINATED)!=0) && (pixels[offset2]&255)>=loLevel)) {
									saddleFound = true; //we have reached a point touching a "true" maximum...
									//if (xList[0]==122) IJ.write("saddle found at level="+loLevel+"; x,y="+xList[listI]+","+yList[listI]+", dir="+d);
									break;              //...or a level not lower, but touching a "true" maximum
								} else if ((pixels[offset2]&255)>=loLevel && (types[offset2]&ELIMINATED)==0) {
									xList[listLen] = (short)(xList[listI]+dirXoffset[d]);
									yList[listLen] = (short)(yList[listI]+dirYoffset[d]);
									listLen++;          //we have found a new point to be processed
									types[offset2] |= LISTED;
								}
							} // if isWithin & not LISTED
						} // for directions d
						if (saddleFound) break;         //no reason to search any further
						listI++;
					} while (listI < listLen);
				} // while !levelFound && loLevel>=0
				for (listI=0; listI<listLen; listI++)   //reset attribute since we may come to this place again
					types[xList[listI] + width*yList[listI]] &= (byte)(~LISTED & 0xFF);
				for (listI=0; listI<lastLen; listI++) { //for all points higher than the level of the saddle point
					offset = xList[listI] + width*yList[listI];
					pixels[offset] = (byte)loLevel;     //set pixel value to the level of the saddle point
					types[offset] |= ELIMINATED;        //mark as processed: there can't be a local maximum in this area
				}
			} // for all maxima iMax
		} // void cleanupMaxima	 

		/** Creates the lookup table used by the watershed function for dilating the particles.
	     * The algorithm allows dilation in both straight and diagonal directions.
	     * There is an entry in the table for each possible 3x3 neighborhood:
	     *          x-1          x          x+1
	     *  y-1    128            1          2
	     *  y       64     pxl_unset_yet     4
	     *  y+1     32           16          8
	     * (to find throws entry, sum up the numbers of the neighboring pixels set; e.g.
	     * entry 6=2+4 if only the pixels (x,y-1) and (x+1, y-1) are set.
	     * A pixel is added on the 1st pass if bit 0 (2^0 = 1) is set,
	     * on the 2nd pass if bit 1 (2^1 = 2) is set, etc.
	     * pass gives the direction of rotation, with 0 = to top left (x--,y--), 1 to top,
	     * and clockwise up to 7 = to the left (x--).
	     * E.g. 4 = add on 3rd pass, 3 = add on either 1st or 2nd pass.
	     */
		private static int[] makeFateTable() {
			int[] table = new int[256];
			bool[] isSet = new bool[8];
			for (int item=0; item<256; item++) {        //dissect into pixels
				for (int i=0, mask=1; i<8; i++) {
					isSet[i] = (item&mask)==mask;
					mask*=2;
				}
				for (int i=0, mask=1; i<8; i++) {       //we dilate in the direction opposite to the direction of the existing neighbors
					if (isSet[(i+4)%8]) table[item] |= mask;
					mask*=2;
				}
				for (int i=0; i<8; i+=2)                //if side pixels are set, for counting transitions it is as good as if the adjacent edges were also set
				if (isSet[i]) {
					isSet[(i+1)%8] = true;
					isSet[(i+7)%8] = true;
				}
				int transitions=0;
				for (int i=0; i<8; i++) {
					if (isSet[i] != isSet[(i+1)%8])
						transitions++;
				}
				if (transitions>=4) {                   //if neighbors contain more than one region, dilation ito this pixel is forbidden
					table[item] = 0;
				} else {
				}
			}
			return table;
		} // int[] makeFateTable	 
		
		/** dilate the UEP on one level by one pixel in the direction specified by step, i.e., set pixels to 255
	     * @param level the level of the EDM that should be processed (all other pixels are untouched)
	     * @param pass gives direction of dilation, see makeFateTable
	     * @param ip1 the EDM with the segmeted blobs successively getting set to 255
	     * @param ip2 a processor used as scratch storage, must hold the same data as ip1 upon entry.
	     *            This method ensures that ip2 equals ip1 upon return
	     * @param table the fateTable
	     * class variables used:
	     *  xCoordinate[], yCoordinate[]    list of pixel coordinates sorted by level (in sequence of y, x within each level)
	     *  levelStart[]                    offsets of the level in xCoordinate[], yCoordinate[]
	     * @return                          true if pixels have been changed
	     */
		private static bool processLevel (int level, int pass, byte[] ip1, int width, int height, byte[] ip2, int[] table,
		                             int[] histogram, int[] levelStart, short[] xCoordinate, short[] yCoordinate)
		{
			int rowSize = width;
			int xmax = rowSize - 1;
			int ymax = height - 1;
			byte[] pixels1 = ip1;
			byte[] pixels2 = ip2;
			bool changed = false;
			for (int i=0; i<histogram[level]; i++) {
				int coordOffset = levelStart [level] + i;
				int x = xCoordinate [coordOffset];
				int y = yCoordinate [coordOffset];
				int offset = x + y * rowSize;
				int index;
				if ((pixels2 [offset] & 255) != 255) {
					index = 0;
					if (y > 0 && (pixels2 [offset - rowSize] & 255) == 255)
						index ^= 1;
					if (x < xmax && y > 0 && (pixels2 [offset - rowSize + 1] & 255) == 255)
						index ^= 2;
					if (x < xmax && (pixels2 [offset + 1] & 255) == 255)
						index ^= 4;
					if (x < xmax && y < ymax && (pixels2 [offset + rowSize + 1] & 255) == 255)
						index ^= 8;
					if (y < ymax && (pixels2 [offset + rowSize] & 255) == 255)
						index ^= 16;
					if (x > 0 && y < ymax && (pixels2 [offset + rowSize - 1] & 255) == 255)
						index ^= 32;
					if (x > 0 && (pixels2 [offset - 1] & 255) == 255)
						index ^= 64;
					if (x > 0 && y > 0 && (pixels2 [offset - rowSize - 1] & 255) == 255)
						index ^= 128;
					switch (pass) {
					case 0:
						if ((table [index] & 1) == 1) {
							pixels1 [offset] = (byte)255;
							changed = true;
						}
						break;
					case 1:
						if ((table [index] & 2) == 2) {
							pixels1 [offset] = (byte)255;
							changed = true;
						}
						break;
					case 2:
						if ((table [index] & 4) == 4) {
							pixels1 [offset] = (byte)255;
							changed = true;
						}
						break;
					case 3:
						if ((table [index] & 8) == 8) {
							pixels1 [offset] = (byte)255;
							changed = true;
						}
						break;
					case 4:
						if ((table [index] & 16) == 16) {
							pixels1 [offset] = (byte)255;
							changed = true;
						}
						break;
					case 5:
						if ((table [index] & 32) == 32) {
							pixels1 [offset] = (byte)255;
							changed = true;
						}
						break;
					case 6:
						if ((table [index] & 64) == 64) {
							pixels1 [offset] = (byte)255;
							changed = true;
						}
						break;
					case 7:
						if ((table [index] & 128) == 128) {
							pixels1 [offset] = (byte)255;
							changed = true;
						}
						break;
					} // switch
				} // if .. !=255
			} // for pixel i
			//IJ.write("level="+level+", pass="+pass+", changed="+changed);
			if (changed) {
				//make sure that ip2 is updated for the next time
				//System.arraycopy(pixels1, 0, pixels2, 0, rowSize*height);
				Array.Copy(pixels1, pixels2,rowSize*height);
			}
			return changed;
		} //processLevel	 
		

		/** Do watershed segmentation on a byte image, with the start points (maxima)
	     * set to 255 and the background set to 0. The image should not have any local maxima
	     * other than the marked ones. Local minima will lead to artifacts that can be removed
	     * later. On output, all particles will be set to 255, segmentation lines remain at their
	     * old value.
	     * @param ip  The byteProcessor containing the image, with size given by the class variables width and height
	     * @return    false if canceled by the user (note: can be cancelled only if called by "run" with a known ImagePlus)
	     */    
		private static bool watershedSegment(byte[] ip, int w, int h) {
			
			byte[] pixels = ip;
			// create arrays with the coordinates of all points between value 1 and 254
			//This method, suggested by Stein Roervik (stein_at_kjemi-dot-unit-dot-no),
			//greatly speeds up the watershed segmentation routine.
			// FIXME P3: use already generated histogram ?
			
//			Histogram.loadHistogram(ip, w, h, histogram);
//			int[] histogramValus = histogram.values;
			int[] histogramValus = new int[256];
			Histogram.loadHistogram(ip, w*h, histogramValus);

			int arraySize = w*h - histogramValus[0] - histogramValus[255];
			short[] xCoordinate = new short[arraySize];
			short[] yCoordinate = new short[arraySize];
			int highestValue = 0;
			int offset = 0;
			int[] levelStart = new int[256];
			for (int v=1; v<255; v++) {
				levelStart[v] = offset;
				offset += histogramValus[v];
				if (histogramValus[v] > 0) highestValue = v;
			}
			int[] levelOffset = new int[highestValue + 1];
			for (int y=0, i=0; y<h; y++) {
				for (int x=0; x<w; x++, i++) {
					int v = pixels[i]&255;
					if (v>0 && v<255) {
						offset = levelStart[v] + levelOffset[v];
						xCoordinate[offset] = (short) x;
						yCoordinate[offset] = (short) y;
						levelOffset[v] ++;
					}
				} //for x
			} //for y
			// now do the segmentation, starting at the highest level and working down.
			// At each level, dilate the particle, constrained to pixels whose values are
			// at that level and also constrained to prevent features from merging.
			int[] table = makeFateTable();


			byte[] ip2 = (byte[])ip.Clone();
			for (int level=highestValue; level>=1; level--) {
				int idle = 1;
				while(true) {                   // break the loop at any point after 8 idle processLevel calls
					if (processLevel(level, 7, ip, w, h, ip2, table, histogramValus, levelStart, xCoordinate, yCoordinate))
						idle = 0;
					if (idle++ >=8) break;
					if (processLevel(level, 3, ip, w, h, ip2, table, histogramValus, levelStart, xCoordinate, yCoordinate))
						idle = 0;
					if (idle++ >=8) break;
					if (processLevel(level, 1, ip, w, h, ip2, table, histogramValus, levelStart, xCoordinate, yCoordinate))
						idle = 0;
					if (idle++ >=8) break;
					if (processLevel(level, 5, ip, w, h, ip2, table, histogramValus, levelStart, xCoordinate, yCoordinate))
						idle = 0;
					if (idle++ >=8) break;
					//IJ.write("diagonal only; level="+level+"; countW="+countW);
					if (processLevel(level, 0, ip, w, h, ip2, table, histogramValus, levelStart, xCoordinate, yCoordinate))
						idle = 0;
					if (idle++ >=8) break;
					if (processLevel(level, 4, ip, w, h, ip2, table, histogramValus, levelStart, xCoordinate, yCoordinate))
						idle = 0;
					if (idle++ >=8) break;
					if (processLevel(level, 2, ip, w, h, ip2, table, histogramValus, levelStart, xCoordinate, yCoordinate))
						idle = 0;
					if (idle++ >=8) break;
					if (processLevel(level, 6, ip, w, h, ip2, table, histogramValus, levelStart, xCoordinate, yCoordinate))
						idle = 0;
					if (idle++ >=8) break;
					//IJ.write("All directions; level="+level+"; countW="+countW);
				}
			}
			return true;
		} // bool watershedSegment

		
		/** analyze the neighbors of a pixel (x, y) in a byte image; pixels <255 are
     * considered part of lines.
     * @param ip
     * @param x
     * @param y
     * @return  IS_LINE if pixel is part of a line, IS_DOT if a single dot
     */    
		static int isLineOrDot(byte[] ip, int width, int height, int x, int y, int[] dirOffset) {
			int result = 0;
			byte[] pixels = ip;
			int offset = x + y*width;
			int whiteNeighbors = 0;             //counts neighbors that are not part of a line
			int countTransitions = 0;
			bool pixelSet;
			bool prevPixelSet = true;
			bool firstPixelSet = true;       //initialize to make the compiler happy
			for (int d=0; d<8; d++) {           //walk around the point and note every no-line->line transition
				if (isWithin(width, height, x, y, d)) {
					pixelSet = (pixels[offset+dirOffset[d]]!=(byte)255);
					if (!pixelSet) whiteNeighbors++;
				} else {
					pixelSet = true;
				}
				if (pixelSet && !prevPixelSet)
					countTransitions ++;
				prevPixelSet = pixelSet;
				if (d==0)
					firstPixelSet = pixelSet;
			}
			if (firstPixelSet && !prevPixelSet)
				countTransitions ++;
			//if (x>=210&&x<=215 && y>=10 && y<=17)IJ.write("x,y="+x+","+y+": transitions="+countTransitions);
			if (countTransitions==1 && whiteNeighbors>=5)
				result = IS_LINE;
			else if (whiteNeighbors==8)
				result = IS_DOT;
			return result;
		} // int isLineOrDot	
		
		
		/** Delete single dots and lines ending somewhere within a segmented particle
     * Needed for post-processing watershed-segmented images that can have local minima
     * @param ip 8-bit image with background = 0, lines between 1 and 254 and segmented particles = 255
     */    
		static void cleanupExtraLines(byte[] ip, int width, int height) {
			int[] dirOffset = CreateDirOffset(width);
			byte[] pixels = ip;
			for (int y=0, i=0; y<height; y++) {
				for (int x=0; x<width; x++,i++) {
					int v = pixels[i]&255;
					if (v<255 && v>0) {
						int type = isLineOrDot(ip, width, height, x, y, dirOffset);
						if (type==IS_DOT) {
							pixels[i] = (byte)255;                  //delete the point;
						} else if (type==IS_LINE) {
							int xEnd = x;
							int yEnd = y;
							bool endFound = true;
							while (endFound) {
								pixels[xEnd + width*yEnd] = (byte)255;  //delete the point
								//if(movie.getSize()<100)movie.addSlice("part-cleaned", ip.duplicate());
								endFound = false;
								for (int d=0; d<8; d++) {               //analyze neighbors of the point
									if (isWithin(width, height, xEnd, yEnd, d)) {
										v = pixels[xEnd + width*yEnd + dirOffset[d]]&255;
										//if(x>210&&x<215&&y==13)IJ.write("x,y start="+x+","+y+": look at="+xEnd+","+yEnd+"+dir "+d+": v="+v);
										if (v<255 && v>0 && isLineOrDot(ip, width, height, xEnd+dirXoffset[d], yEnd+dirYoffset[d], dirOffset)==IS_LINE) {
											xEnd += dirXoffset[d];
											yEnd += dirYoffset[d];
											endFound = true;
											break;
										}
									}
								} // for directions d
							} // while (endFound)
						} // if IS_LINE
					} // if v<255 && v>0
				} // for x
			} // for y
		} // void cleanupExtraLines	
		
		/** after watershed, set all pixels in the background and segmentation lines to 0
     */
		private static void watershedPostProcess(byte[] ip, int width, int height) {
			//new ImagePlus("before postprocess",ip.duplicate()).show();
			byte[] pixels = ip;
			int size = width*height;
			for (int i=0; i<size; i++) {
				if ((pixels[i]&255)<255)
					pixels[i] = (byte)0;
			}
			//new ImagePlus("after postprocess",ip.duplicate()).show();
		}	
		
		/** Create an 8-bit image by scaling the pixel values of ip (background 0) and mark maximum areas as 255.
	    * For use as input for watershed segmentation
	    * @param ip         The original image that should be segmented
	    * @param typeP      Pixel types in ip
	    * @param isEDM      Whether ip is an Euclidian distance map
	    * @param globalMin  The minimum pixel value of ip
	    * @param globalMax  The maximum pixel value of ip
	    * @param threshold  Pixels of ip below this value (calibrated) are considered background. Ignored if ImageProcessor.NO_THRESHOLD
	    * @return           The 8-bit output image.
	    */   
		static void make8bit(ushort[] ip, int width, int height, byte[] typeP, bool isEDM, float globalMin, float globalMax, double threshold, byte[] outMap) {
			byte[] types = typeP;
			double minValue = threshold;
			double offset = minValue - (globalMax-minValue)*(1.0f/253.0f/2-1e-6); //everything above minValue should become >(byte)0
			double factor = 253.0f/(globalMax-minValue);
			
			if (isEDM && factor >1.0f/EDM.ONE) 
				factor = 1.0f/EDM.ONE;   // in EDM, no better resolution is needed
			
			byte[] pixels = outMap;  //convert possibly calibrated image to byte without damaging threshold (setMinAndMax would kill threshold)
			
			int val;
			long v;
			int count = width*height;
			{
				int i = 0;
				while(i < count)
				{
					val = ip[i];
					v = (long)Math.Round((val-offset)*factor);
					if (v<threshold)
					{
						//Set out-of-threshold to background (inactive)
						pixels[i] = (byte)0;
					}
					else if (v<=254)
					{
						
						pixels[i] = (byte)(v&255);
					}
					else 
					{
						//pixel value 255 is reserved for maxima
						pixels[i] = (byte) 254;
					}
					
					if((types[i]&MAX_AREA)!=0)
						pixels[i] = (byte)255;              //prepare watershed by setting "true" maxima+surroundings to 255

					i++;
				}
			}
		} // byteProcessor make8bit

		public static void FindGlobalMinMax(ushort[] edm, int width, int height, out int[] globalMinMax) {
			globalMinMax = new int[] { int.MaxValue, -int.MaxValue };
			// TODO: speed up - linear run through list
			for (int y=0; y<height; y++) {         //find local minimum/maximum now
				for (int x=0; x<width; x++) {      //ImageStatistics won't work if we have no ImagePlus
					int v = ImageHelper.getPixel(edm, width, x, y);
					if (globalMinMax[0]>v) globalMinMax[0] = v;
					if (globalMinMax[1]<v) globalMinMax[1] = v;
				}
			}
		}

		public static void findMaxima(ushort[] edm, int width, int height, double tolerance, double threshold, byte[] maxMap, 
		                              bool isEDM, int[] globalMinMax, out MaxPoint[] maxPoints)
		{

			maxPoints = getSortedMaxPoints(edm, width, height, maxMap, globalMinMax[0], threshold); 
			if(maxPoints.Length == 0) {
				return;
			}

			analyzeAndMarkMaxima(edm, width, height, maxMap, maxPoints, isEDM, globalMinMax[0], tolerance);
		}


		public static void performWatershed(ushort[] inputMap, int width, int height, double threshold, byte[] maxMap, 
		                                    bool isEDM, byte[] outMap, MaxPoint[] maxPoints, int[] globalMinMax)
		{
			//new ImagePlus("Pixel types",typeP.duplicate()).show();
			if (outputType==COUNT || outputType==POINT_SELECTION)
				return;
			
			
			if (outputType == SEGMENTED) 
			{                  
				//Segmentation required, convert to 8bit (also for 8-bit images, since the calibration may have a negative slope)
				make8bit(inputMap, width, height, maxMap, isEDM, globalMinMax[0], globalMinMax[1], threshold, outMap);

				//eliminate all the small maxima (i.e. those outside MAX_AREA)
				cleanupMaxima(outMap, width, height, maxMap, maxPoints);    
				
				//do watershed segmentation
				watershedSegment(outMap, width, height);              
				
				if (!isEDM) {
					cleanupExtraLines(outMap, width, height);       //eliminate lines due to local minima (none in EDM)
				}
				watershedPostProcess(outMap, width, height);                //levels to binary image
				
				// MH: to do	            
				//	            if (excludeOnEdges) 
				//	            	deleteEdgeParticles(outMap, typeMap);
				
			} 
			//	        else 
			//	        {                                        //outputType other than SEGMENTED
			//	            for (int i=0; i<width*height; i++)
			//	                types[i] = (byte)(((types[i]&outputTypeMasks[outputType])!=0)?255:0);
			//	            outIp = typeP;
			//	        }
			
			//	        byte[] outPixels = (byte[])outMap.getPixels();
			//	        //IJ.write("roi: "+roi.toString());
			//	        if (roi!=null) {
			//	            for (int y=0, i=0; y<outIp.getHeight(); y++) { //delete everything outside roi
			//	                for (int x=0; x<outIp.getWidth(); x++, i++) {
			//	                    if (x<roi.x || x>=roi.x+roi.getWidth() || y<roi.y || y>=roi.y+roi.getHeight()) outPixels[i] = (byte)0;
			//	                    else if (mask !=null && (mask[x-roi.x + roi.getWidth()*(y-roi.y)]==0)) outPixels[i] = (byte)0;
			//	                }
			//	            }
			//	        }
		} 
	}
}

