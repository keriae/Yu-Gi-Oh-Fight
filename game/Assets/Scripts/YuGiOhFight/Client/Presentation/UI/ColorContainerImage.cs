using UnityEngine;


namespace YuGiOhFight.Client.Presentation.UI
{
	public class ColorContainerImage : ColorContainerGraphic
	{
		[SerializeField]
		private Sprite _spriteUnselected;

		[SerializeField]
		private Sprite _spriteSelected;

		[SerializeField]
		private Sprite _spriteButtonDown;

		[SerializeField]
		private Sprite _spriteButtonEnter;

		[SerializeField]
		private Sprite _spriteInactive;

		private UnityEngine.UI.Image _targetImage;

		public Sprite spriteUnselected
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public Sprite spriteSelected
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public Sprite spriteButtonDown
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public Sprite spriteButtonEnter
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public Sprite spriteInactive
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public UnityEngine.UI.Image targetImage => null;

		public override void SetColor(SelectMode select_mode, StatusMode status_mode, bool is_active = true)
		{
		}
	}
}
