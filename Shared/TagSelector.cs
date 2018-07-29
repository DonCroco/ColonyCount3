using System;
using System.Collections.Generic;
using OpenTK;

namespace CC.Core
{
	public class TagSelector : PrimSelector
	{
		private Tag tag;
		private float dist;

		public static TagSelector Select(Tag tag, Vector2 pos, float maxDist)
		{
			Vector2 delta = tag.Pos - pos;
			float dist = delta.Length;
			if(dist < maxDist)
			{
				TagSelector selector = new TagSelector(tag, dist);
				return selector;
			}
			return null;
		}

		public static TagSelector Select (ICollection<Tag> tags, Vector2 pos, float maxDist)
		{
			TagSelector selector = null;
			foreach (Tag tag in tags) {
				TagSelector newSelector = Select (tag, pos, maxDist);
				if(newSelector != null) {
					selector = newSelector;
					maxDist = selector.GetSelectDistance();
				}
			}
			return selector;
		}


		private TagSelector (Tag tag, float dist)
		{
			this.tag = tag;
			this.dist = dist;
		}

		public float GetSelectDistance ()
		{
			return dist;
		}

		public void DragTo(Vector2 pos, Vector2 delta)
		{
			tag.Pos = pos;
			tag.calculateThreshold = true;
		}

		public object GetPrimitive()
		{
			return tag;
		}

		public Vector2 GetCenter() {
			return tag.Pos;
		}

	}
}

