using System;
using System.Drawing;
using OpenTK;

namespace CC.Core
{
	public class ColorHelper
	{
		public static Color ToolbarEdgeColor { get { return Color.Black;  } }
		public static Color ToolbarBgColor { get { return Color.Black;  } }
		public static Color ROIEdge { get { return Color.Black; } }
		public static Color ROIEdgeActive { get { return Color.FromArgb(0xFF,0x00, 0xFF, 0x00); } }
		public static Color ROIEdgeSelected { get { return Color.Yellow; } }
		public static Color ROIBackground { get { return Color.FromArgb(150,200,200,255); } }
		public static Color ROIBackgroundActive { get { return ROIBackground; } }

		public static Vector4 TagColor { get { return ColorToVec(Color.FromArgb(0xFF,0x00, 0x99, 0x00)); } }
		public static Vector4 TagColorActive { get { return ColorToVec(Color.FromArgb(0xFF,0x00, 0xFF, 0x00)); } }
		public static Vector4 TagColorSelected { get { return ColorToVec(Color.Yellow); } }
		public static Vector4 Region { get { return TagColorActive; } }


		public static Vector4 ColorToVec(Color color) {
			return new Vector4((float)color.R/255f, (float)color.G/255f, (float)color.B/255f, (float)color.A/255f);
		}
	}
}

