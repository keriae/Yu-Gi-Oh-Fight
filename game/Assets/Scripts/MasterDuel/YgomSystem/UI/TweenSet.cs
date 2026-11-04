using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YgomSystem.UI
{
	public class TweenSet : Tween
	{
		[Flags]
		public enum Target
		{
			position = 1,
			scale = 2,
			rotation = 4,
			color = 8,
			gameObject = 0x10
		}

		[Serializable]
		public class Param
		{
			public Vector3 position;

			public Vector3 scale;

			public Quaternion rotation;

			public Color color;

			public GameObject gameObject;
		}

		[SerializeField]
		public Param from;

		[SerializeField]
		public Param to;

		[SerializeField]
		[EnumFlags]
		public Target target;

		private Color fromColor;

		private RectTransform rtrans;

		private Graphic graphic;

		private int crntParam;

		private List<KeyValuePair<Graphic, Color>> childGraps;

		private void ScanChildGraphic(GameObject obj)
		{
            if (obj == null)
                return;
            childGraps = new List<KeyValuePair<Graphic, Color>>();
            var graphics = obj.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                var g = graphics[i];
                if (g != null)
                    childGraps.Add(new KeyValuePair<Graphic, Color>(g, g.color));
            }
        }

		protected override void CaptureFrom()
		{
            rtrans = GetComponent<RectTransform>();
            graphic = GetComponent<Graphic>();
            if (graphic != null)
                fromColor = graphic.color;

            if ((target & Target.gameObject) != 0 && to != null && to.gameObject != null)
                ScanChildGraphic(to.gameObject);
        }

		protected override void OnSetValue(float par)
		{
            if (from == null || to == null)
                return;

            if ((target & Target.position) != 0)
            {
                if (rtrans == null)
                    rtrans = GetComponent<RectTransform>();
                if (rtrans != null)
                    rtrans.anchoredPosition3D = Vector3.Lerp(from.position, to.position, par);
            }

            if ((target & Target.scale) != 0)
                transform.localScale = Vector3.Lerp(from.scale, to.scale, par);

            if ((target & Target.rotation) != 0)
                transform.localRotation = Quaternion.Slerp(from.rotation, to.rotation, par);

            if ((target & Target.color) != 0)
            {
                Color color = Color.Lerp(from.color, to.color, par);
                if (childGraps != null && childGraps.Count > 0)
                {
                    for (int i = 0; i < childGraps.Count; i++)
                    {
                        var pair = childGraps[i];
                        if (pair.Key == null)
                            continue;
                        pair.Key.color = color;
                    }
                }
                else if (graphic != null)
                {
                    graphic.color = color;
                }
            }

            if ((target & Target.gameObject) != 0 && to != null && to.gameObject != null)
            {
                bool active = par >= 1f;
                if (crntParam != (active ? 1 : 0))
                {
                    crntParam = active ? 1 : 0;
                    to.gameObject.SetActive(active);
                }
            }
        }
	}
}
