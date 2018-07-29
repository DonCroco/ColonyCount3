using System;
using OpenTK;

namespace CC.Core
{
	public class SphereSelector : PrimSelector
	{
		Sphere sphere;
		float dist;
		Vector2 selectPoint;
		bool edge;

		public SphereSelector (Sphere sphere, float dist, bool edge, Vector2 selectPoint)
		{
			this.sphere = sphere;
			this.dist = dist;
			this.selectPoint = selectPoint;
			this.edge = edge;
		}

		public static SphereSelector Select(Sphere sphere, Vector2 selectPos, float maxDist, bool testCenter, bool testEdge)
		{
			bool edge = false;
			Sphere selectedSphere = null;
			Vector2 center = sphere.center;
			float radius = sphere.radius;
			
			// test center selection
			float centerDist = (selectPos - center).Length;
			if(testCenter)
			{
				if(centerDist <= maxDist) {
					maxDist = centerDist;
					selectedSphere = sphere;
				}
			}
			
			if(testEdge && selectedSphere == null) {
				float dist = Math.Abs(centerDist - radius);
				if(dist < maxDist) {
					maxDist = dist;
					selectedSphere = sphere;
					edge = true;
					
					// snap edgepos to radius
					Vector2 vSelectOffset = selectPos - center;
					vSelectOffset = Vector2.Normalize(vSelectOffset);
					vSelectOffset = vSelectOffset*radius;
					selectPos = center + vSelectOffset;
				}
			}
			
			if(selectedSphere != null)
			{
				SphereSelector selector =  new SphereSelector(selectedSphere, maxDist, edge, selectPos);
				return selector;
			}
			
			return null;
		}

		public float GetSelectDistance()
		{
			return dist;
		}
		
		public void DragTo(Vector2 newPos, Vector2 delta)
		{
			Vector2 center = sphere.center;
			
			if(edge) {
				
				// move select point
				selectPoint = selectPoint + delta;
				
				// move center half distance
				Vector2 deltaCenter = delta*0.5f;
				center = center +  deltaCenter;
				
				
				// Calc new radius from new center to edge pos
				Vector2 v = center - selectPoint;
				float radius = v.Length;
				sphere.radius = radius;
			} else {
				center = center + delta;
			}
			sphere.center = center;
		}

		public object GetPrimitive()
		{
			return sphere;
		}

		public Vector2 GetCenter() {
			return selectPoint;
		}


	}
}

