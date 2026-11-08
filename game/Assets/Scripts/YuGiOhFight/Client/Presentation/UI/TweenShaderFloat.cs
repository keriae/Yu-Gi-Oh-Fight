using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class TweenShaderFloat : Tween
	{
		[SerializeField]
		public float from;

		[SerializeField]
		public float to;

		[SerializeField]
		public string paramName;

		[SerializeField]
		public Material[] materials;

		private System.Collections.Generic.Dictionary<int, Material> materialList;

		public bool isRecusive;

		public bool isShared;

		public void InitializeMaterials(System.Collections.Generic.Dictionary<int, Material> initMaterialList = null)
		{
            materialList = new System.Collections.Generic.Dictionary<int, Material>();

            if (initMaterialList != null)
            {
                foreach (System.Collections.Generic.KeyValuePair<int, Material> kv in initMaterialList)
                    materialList[kv.Key] = kv.Value;
                return;
            }

            if (materials != null)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    Material m = materials[i];
                    if (m != null)
                        materialList[m.GetInstanceID()] = m;
                }
            }

            if (isRecusive)
            {
                var renderers = GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var mats = isShared ? renderers[i].sharedMaterials : renderers[i].materials;
                    for (int j = 0; j < mats.Length; j++)
                    {
                        Material m = mats[j];
                        if (m != null && !materialList.ContainsKey(m.GetInstanceID()))
                            materialList[m.GetInstanceID()] = m;
                    }
                }
            }
            else
            {
                Renderer r = GetComponent<Renderer>();
                if (r != null)
                {
                    var mats = isShared ? r.sharedMaterials : r.materials;
                    for (int j = 0; j < mats.Length; j++)
                    {
                        Material m = mats[j];
                        if (m != null && !materialList.ContainsKey(m.GetInstanceID()))
                            materialList[m.GetInstanceID()] = m;
                    }
                }
            }
        }

		public System.Collections.Generic.Dictionary<int, Material> GetMaterials()
		{
            if (materialList == null)
                InitializeMaterials();
            return materialList;
        }

		protected override void CaptureFrom()
		{
            InitializeMaterials();
        }

		protected override void OnSetValue(float par)
		{
            if (materialList == null)
                InitializeMaterials();
            if (materialList.Count == 0 || string.IsNullOrEmpty(paramName))
                return;

            float v = Mathf.Lerp(from, to, par);
            foreach (System.Collections.Generic.KeyValuePair<int, Material> kv in materialList)
            {
                Material m = kv.Value;
                if (m != null)
                    m.SetFloat(paramName, v);
            }
        }
	}
}
