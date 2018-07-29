using System;
using OpenTK;


namespace CC.Core
{
	public interface PrimSelector
	{
		float GetSelectDistance();
		void DragTo(Vector2 pos, Vector2 delta);
		object GetPrimitive();
		Vector2 GetCenter();
	}
}

