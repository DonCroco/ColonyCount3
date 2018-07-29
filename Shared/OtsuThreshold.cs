using System;

namespace CC.Core
{
	public class OtsuThreshold
	{

		// function is used to compute the q values in the equation
		static float Px(int init, int end, float[] hist)
		{
			float sum = 0;
			int i;
			for (i = init; i <= end; i++)
				sum += hist[i];
			
			return sum;
		}
		
		// function is used to compute the mean values in the equation (mu)
		static float Mx(int init, int end, float[] hist)
		{
			float sum = 0;
			int i;
			for (i = init; i <= end; i++)
				sum += i * hist[i];
			
			return sum;
		}
		
		// finds the maximum element in a vector
		static int findMax(float[] vec, int n)
		{
			float maxVec = 0;
			int idx=0;
			int i;
			
			for (i = 1; i < n - 1; i++)
			{
				if (vec[i] > maxVec)
				{
					maxVec = vec[i];
					idx = i;
				}
			}
			return idx;
		}
		
		// find otsu threshold
		public static int getThreshold(float[] hist) {
			float[] vet = new float[256];
			
			float p1,p2,p12, m1, m2;
			int k;
			
			
			// loop through all possible t values and maximize between class variance
			for (k = 1; k != 255; k++)
			{
				p1 = Px(0, k, hist);
				p2 = Px(k + 1, 255, hist);
				m1 = Mx(0, k, hist);
				m2 = Mx(k + 1, 255, hist);
				float diff=(m1 * p2) - (m2 * p1);
				
				p12 = p1 * p2;
				if (p12 == 0)
					p12 = 1;
				vet[k] = (float)diff * diff / p12;
			}
			
			int t = 0;
			t = findMax(vet, 256);
			
			return t;
		}
	}
}

