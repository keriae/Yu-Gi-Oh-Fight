namespace YuGiOhFight.Client.Presentation.UI
{
	public class RectSelectionItem : SelectionItem
	{
		private UnityEngine.Vector3[] rectWorldCorners;

		private UnityEngine.Vector3[] boxWorldCorners;

		private UnityEngine.Vector2[] viewPoints;

		public bool useWorldRectSetting;

		public UnityEngine.Vector2 worldRectHalfSize;

		public UnityEngine.Vector3 worldRectCenter;

		public UnityEngine.Vector3 worldRectAngle;

		public override UnityEngine.Vector2 viewCenterPosition => default(UnityEngine.Vector2);

		public override UnityEngine.Vector2 GetClosestPoint(UnityEngine.Vector2 base_position, UnityEngine.Vector2 direction, bool contains_check = true)
		{
			return default(UnityEngine.Vector2);
		}

		protected virtual void SetupViewPoints()
		{
		}

		private UnityEngine.Vector3 ElementScale(UnityEngine.Vector3 a, UnityEngine.Vector3 b)
		{
			return default(UnityEngine.Vector3);
		}

		public override bool IsContainsPoint(UnityEngine.Vector2 view_position)
		{
			return false;
		}

		public override bool IsRectContains(UnityEngine.Vector2 rect_point0, UnityEngine.Vector2 rect_point1, UnityEngine.Vector2 rect_point2, UnityEngine.Vector2 rect_point3, bool containedComplete)
		{
			return false;
		}

		private void LateUpdate()
		{
		}

		public void SetupWorldRect(bool use, UnityEngine.Vector2 half_size, UnityEngine.Vector3 center, UnityEngine.Vector3 angle)
		{
		}

		public UnityEngine.Vector2[] CloneViewPoints()
		{
			return null;
		}
	}
}
