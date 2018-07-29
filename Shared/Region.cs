using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace CC.Core
{
	public class Region
	{
		List<Point> points; 
		Rectangle bounds;

		public List<Point> Points {
			get { return points; }
		}

		public Rectangle Bounds {
			get { return bounds; }
		}
	

		public static void Transform (List<Point> Points, Point offset, out Rectangle bounds)
		{
			int minX = int.MaxValue;
			int minY = int.MaxValue;
			int maxX = 0;
			int maxY = 0;

			for (int i= 0;i<Points.Count;i++) {
				Point point = Points[i];
				int x = point.X + offset.X;
				int y = point.Y + offset.Y;

				Points[i] = new Point(x,y);

				minX = Math.Min(x, minX);
				maxX = Math.Max(x, maxX);
				minY = Math.Min(y, minY);
				maxY = Math.Max(y, maxY);
			}
			
			bounds = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
		}

		public static void loadBinaryMap(List<Point> points, Point offset, ushort[] binaryMap, int mapWidth)
		{
			foreach(Point point in points)
			{
				int x = point.X + offset.X;
				int y = point.Y + offset.Y;
				int index = y*mapWidth + x;
				binaryMap[index] = ushort.MaxValue;
			}
		}

		public static bool Contains (List<Point> points, Point p)
		{
			foreach(Point point in points)
			{
				if(point.Equals(p))
					return true;
			}
			return false;
		}


		public Region (ICollection<Point> incPoints, Rectangle bounds)
		{
			// TODO: perhaps points should be local space?
			this.points = new List<Point>(incPoints);
			this.bounds = bounds;
		}
	}
}

