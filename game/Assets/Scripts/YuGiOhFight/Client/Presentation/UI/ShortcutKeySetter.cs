using System;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class ShortcutKeySetter : MonoBehaviour
	{
		[Serializable]
		public class Setting
		{
			[SerializeField]
			private string m_ButtonLabel;

			[SerializeField]
			private SelectorManager.KeyType m_KeyType;

			[SerializeField]
			private SelectorManager.KeyType m_KeyTypeSub;

			[SerializeField]
			private string m_IconLabel;

			[SerializeField]
			private YgomSystem.Utility.GamePadIconUtil.Variation m_IconVariation;

			public string buttonLabel
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			public SelectorManager.KeyType keyType
			{
				get
				{
					return default(SelectorManager.KeyType);
				}
				set
				{
				}
			}

			public SelectorManager.KeyType keyTypeSub
			{
				get
				{
					return default(SelectorManager.KeyType);
				}
				set
				{
				}
			}

			public string iconLabel
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			public YgomSystem.Utility.GamePadIconUtil.Variation iconVariation
			{
				get
				{
					return default(YgomSystem.Utility.GamePadIconUtil.Variation);
				}
				set
				{
				}
			}
		}

		[Serializable]
		public class MouseSetting
		{
			[SerializeField]
			private string m_ButtonLabel;

			[SerializeField]
			private SelectorManager.MouseType m_MouseType;

			public string buttonLabel
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			public SelectorManager.MouseType mouseType
			{
				get
				{
					return default(SelectorManager.MouseType);
				}
				set
				{
				}
			}
		}

		[SerializeField]
		private string m_MouseCancelLabel;

		[SerializeField]
		private string[] m_MouseCancelAdditionalLabels;

		[SerializeField]
		private Setting[] m_Settings;

		private int m_LoadingCount;

		public bool isLoading => false;

		public void Initialize(Action onComplete = null)
		{
		}

		public void Initialize(Elements.ElementObjectManager eom, Action onComplete = null)
		{
		}

		public static bool SetShortcut(Elements.ElementObjectManager eom, Setting setting, Action onComplete = null)
		{
			return false;
		}

		public static bool SetShortcut(Elements.ElementObjectManager eom, string buttonLabel, string iconLabel, SelectorManager.KeyType keyType, SelectorManager.KeyType keyTypeSub = SelectorManager.KeyType.None, YgomSystem.Utility.GamePadIconUtil.Variation iconVariation = YgomSystem.Utility.GamePadIconUtil.Variation.Var00, Action onComplete = null)
		{
			return false;
		}

		public static bool SetShortcutKey(Elements.ElementObjectManager eom, string buttonLabel, SelectorManager.KeyType keyType, SelectorManager.KeyType keyTypeSub = SelectorManager.KeyType.None)
		{
			return false;
		}

		public static bool SetShortcutIcon(Elements.ElementObjectManager eom, string iconLabel, SelectorManager.KeyType keyType, YgomSystem.Utility.GamePadIconUtil.Variation variation = YgomSystem.Utility.GamePadIconUtil.Variation.Var00, Action onComplete = null)
		{
			return false;
		}

		public static bool SetShortcutIcon(Elements.ElementObjectManager eom, string iconLabelMain, string iconLabelSub, string iconLabelPlus, SelectorManager.KeyType keyTypeMain, SelectorManager.KeyType keyTypeSub, YgomSystem.Utility.GamePadIconUtil.Variation variationMain = YgomSystem.Utility.GamePadIconUtil.Variation.Var00, YgomSystem.Utility.GamePadIconUtil.Variation variationSub = YgomSystem.Utility.GamePadIconUtil.Variation.Var00, Action onComplete = null)
		{
			return false;
		}

		public static bool SetMouseCancelShortcutKey(Elements.ElementObjectManager eom, string buttonLabel)
		{
			return false;
		}
	}
}
