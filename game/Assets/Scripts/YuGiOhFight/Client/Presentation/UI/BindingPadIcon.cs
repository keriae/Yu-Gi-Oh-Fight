using System.Runtime.CompilerServices;
using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class BindingPadIcon : MonoBehaviour, YgomGame.Menu.Common.IAsyncProgressContent, ILoadingIconHandler
	{
		[SerializeField]
		private ShortcutIcon.Mode m_Mode;

		[SerializeField]
		private SelectorManager.KeyType m_KeyType;

		[SerializeField]
		private SelectorManager.AnalogType m_AnalogType;

		[SerializeField]
		private ShortcutIcon.AnalogDirection m_AnalogDirection;

		[SerializeField]
		private SelectorManager.MouseType m_MouseType;

		[SerializeField]
		private YgomSystem.Utility.GamePadIconUtil.Variation m_IconVariation;

		private System.Collections.IEnumerator m_SequenceRoutine;

		public ShortcutIcon.Mode mode
		{
			get
			{
				return default(ShortcutIcon.Mode);
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

		public SelectorManager.AnalogType analogType
		{
			get
			{
				return default(SelectorManager.AnalogType);
			}
			set
			{
			}
		}

		public ShortcutIcon.AnalogDirection analogDirection
		{
			get
			{
				return default(ShortcutIcon.AnalogDirection);
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

		public bool visible => false;

		public event System.Action onReloadEvent
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public bool IsDone()
		{
			return false;
		}

		private void Update()
		{
		}

		public void ProgressUpdate()
		{
		}

		private System.Collections.IEnumerator ySequence()
		{
			return null;
		}

		private void SourceChange()
		{
		}

		private void OnDestroy()
		{
		}
	}
}
