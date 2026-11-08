using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Willow.UI
{
	public class TextMeshProEx : TMPro.TextMeshProUGUI, ITextAccessor
	{
		public enum TextMeshProLang
		{
			JP_ = 0,
			US_ = 1,
			Max = 2
		}

		public enum TextMeshProSDF
		{
			Main_SDF = 0,
			Title_SDF = 1,
			未使用 = 2,
			Number_SDF = 3,
			Chat_SDF = 4,
			Summon_SDF = 5
		}

		public static string kNoMaterial;

		public static string kDefaultMaterial;

		public static string kNoTextColorSettings;

		public static string kNoReflectTextColorSettings;

		public static string kDefaultTextColorSettings;

		private float m_defaultLine;

		[SerializeField]
		private string m_labelName;

		[SerializeField]
		private TextMeshProSDF m_SDFName;

		[SerializeField]
		private string m_materialName;

		[SerializeField]
		private string m_textColorSettings;

		[SerializeField]
		private int m_textColorIndex;

		[SerializeField]
		private bool m_isNoDefaultLine;

		[SerializeField]
		private bool m_isNoIStyle;

		public System.Action<TextMeshProEx> onStartCallback;

		private string m_setSpriteText;

		private bool m_isLoadSDF;

		private float m_textWidth;

		private static SystemLanguage s_language;

		private static System.Collections.Generic.Dictionary<string, TMPro.TMP_FontAsset> s_dicAsset;

		private const string kTextPath = "Font/";

		public System.DateTime dateTime
		{
			get
			{
				return default(System.DateTime);
			}
			set
			{
			}
		}

		public string labelName
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public TextMeshProSDF SDFName
		{
			get
			{
				return default(TextMeshProSDF);
			}
			set
			{
			}
		}

		public string materialName
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public string textColorSettings
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public int textColorIndex
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public bool isNoDefaultLine
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool isNoIStyle
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public string setString
		{
			set
			{
			}
		}

		protected override void Start()
		{
		}

		public void SetLabel()
		{
		}

		public float GetPreferredWidthEx()
		{
			return 0f;
		}

		public void SetButtonText()
		{
		}

		public void SetLabel(string tagName, bool isAutoSet = false)
		{
		}

		public void SetString(string value, bool shouldInOutGameTagFilter = false)
		{
		}

		public void SetTextMaterial(string matName, bool isRefresh = false)
		{
		}

		public static System.Collections.Generic.List<string> GetMatList(string sdfName)
		{
			return null;
		}

		public static System.Collections.Generic.List<string> GetMatList(TMPro.TMP_FontAsset fontAsset)
		{
			return null;
		}

		protected override void LoadFontAsset()
		{
		}

		private void SetSDFColor()
		{
		}

		public void SetSDF(SystemLanguage lang, bool cache = true)
		{
		}

		private string FilterInOutGameTag(string str)
		{
			return null;
		}

		private string RemoveOnlyTag(string str, string tagName)
		{
			return null;
		}

		private string RemoveTag(string str, string tagName)
		{
			return null;
		}

		protected override Material[] GetSharedMaterials()
		{
			return null;
		}

		private static void InitLang(SystemLanguage lang)
		{
		}

		private static string GetFontAssetPath(TextMeshProSDF sdfName)
		{
			return null;
		}

		private static string GetFontMatPath(TextMeshProSDF sdfName, string matName)
		{
			return null;
		}

		public static void LoadFontAsset(Object owner, SystemLanguage lang, TextMeshProSDF sdfName, System.Action<TMPro.TMP_FontAsset> callback, bool cache = true)
		{
		}

		public static void LoadFontMaterial(Object owner, SystemLanguage lang, TextMeshProSDF sdfName, string matName, System.Action<Material> callback, bool cache = true)
		{
		}

		public static void ClearFontAsset()
		{
		}

		private static void LoadFontAssetFallback(Object owner, SystemLanguage lang, TextMeshProSDF sdfName, TMPro.TMP_FontAsset baseAsset, System.Action<TMPro.TMP_FontAsset> callback)
		{
		}

		private static void LoadFontAssetFallback(Object owner, SystemLanguage lang, TextMeshProSDF sdfName, string sdfNameBase, TMPro.TMP_FontAsset baseAsset, System.Action<TMPro.TMP_FontAsset> callback, bool isFontAdd = false, bool isFontCurrency = false)
		{
		}

		private static void LoadAsync<T>(string path, Object owner, System.Action<T> callback = null, bool cache = true) where T : Object
		{
		}

		private static Object[] LoadAllMaterial(string path)
		{
			return null;
		}
	}
}
