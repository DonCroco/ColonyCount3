using System;
using System.Drawing;
using OpenTK;

namespace CC.Core
{
	public class Tag
	{
		private Vector2 pos;
		public Vector2 Pos {
			get { return pos; }
			set { 
				pos = value;
			}
		}

		public bool calculateThreshold = true;
		public int[] Threshold = new int[3];
		public int[] Color = new int[3];
		
		public void GetNormalizedMaxMin (ref float[] min, ref float[] max)
		{
			float f = 1.0f/255.0f;
			for (int i=0; i<3; i++) {
				if(Threshold[i] > Color[i]) {
					min[i] = 0; //1.0f*f;
					max[i] = Threshold[i]*f;
				} else {
					min[i] = Threshold[i]*f;
					max[i] = 1.0f; //255.0f*f;
				}
			}
		}

		public Rectangle ThresholdSampleRect
		{
			get {
				int halfWidth = 50;
				Rectangle rect = new Rectangle((int)pos.X - halfWidth, (int)pos.Y - halfWidth, halfWidth*2, halfWidth*2);
				return rect;
			}
		}

		public Tag (Vector2 pos)
		{
			this.pos = pos;
		}
	}
}

