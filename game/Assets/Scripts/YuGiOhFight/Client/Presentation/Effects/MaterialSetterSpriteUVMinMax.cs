using UnityEngine;


namespace YuGiOhFight.Client.Presentation.Effects
{
	public class MaterialSetterSpriteUVMinMax : MonoBehaviour
	{
		[SerializeField]
		private string m_MinMaxLabel;

		[SerializeField]
		private string m_SetTexLabel;

		[SerializeField]
		private Sprite m_SourceSpriteDefault;

		[SerializeField]
		private Sprite m_SourceSpriteMobile;

		private UnityEngine.UI.Image m_TargetImage;

		private MaterialSetterGraphWriter m_TargetWriter;

		private Sprite m_TargetSprite;

		private void OnEnable()
		{
		}

		private void Update()
		{
		}

		private bool TrySetUvMinMax()
		{
			return false;
		}
	}
}
