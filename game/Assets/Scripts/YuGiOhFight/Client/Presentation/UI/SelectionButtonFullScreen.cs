namespace YuGiOhFight.Client.Presentation.UI
{
	public class SelectionButtonFullScreen : SelectionButton
	{
		public override UnityEngine.Vector2 GetClosestPoint(UnityEngine.Vector2 base_position, UnityEngine.Vector2 direction, bool contains_check = true)
		{
			return default(UnityEngine.Vector2);
		}

		public override bool IsContainsPoint(UnityEngine.Vector2 view_position)
		{
			return false;
		}

		public override bool IsRectContains(UnityEngine.Vector2 rect_point0, UnityEngine.Vector2 rect_point1, UnityEngine.Vector2 rect_point2, UnityEngine.Vector2 rect_point3, bool containedComplete)
		{
			return false;
		}
	}
}
