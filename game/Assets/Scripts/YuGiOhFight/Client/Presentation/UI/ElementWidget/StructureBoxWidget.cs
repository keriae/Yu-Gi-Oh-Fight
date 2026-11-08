using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.UI.ElementWidget
{
	public class StructureBoxWidget : ElementWidgetBase
	{
		public readonly UnityEngine.UI.Image deckImage;

		public readonly UnityEngine.UI.Image deckOpenedImage;

		public readonly UnityEngine.UI.RawImage[] cardImages;

		public readonly TMPro.TextMeshProUGUI nameText;

		public readonly SelectionButton button;

		private UnityEngine.UI.Image[] _monsterImages;

		public System.Action<StructureBoxWidget> onClickEvent;

		private bool _useNewDeckImage;

		private bool _useNewOpenedDeckImage;

		public int structureId
		{
			[CompilerGenerated]
			get
			{
				return 0;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public StructureBoxWidget(Elements.ElementObjectManager eom, UnityEngine.Sprite deckSprite, UnityEngine.Sprite openedDeckSprite, UnityEngine.Sprite[] monsterSprites)
			: base(null)
		{
		}

		public StructureBoxWidget Binding(int structureId)
		{
			return null;
		}
	}
}
