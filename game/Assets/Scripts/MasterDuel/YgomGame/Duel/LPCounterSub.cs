using System;
using UnityEngine;

namespace YgomGame.Duel
{
	public class LPCounterSub : MonoBehaviour
	{
		protected const string LABEL_TW_EFFECTRECOVER = "RecoverEffect";

		protected const string LABEL_TW_EFFECTDAMAGE = "DamageEffect";

		protected const string LABEL_TW_EFFECTCOST = "CostEffect";

		protected Action<Color, int> onFinished;

		protected int m_TargetLP;

		protected TMPro.TextMeshProUGUI m_SubText_Origin;

		protected TMPro.TextMeshProUGUI m_SubText => null;

		public bool active => false;

		public void ApplyEffect(int value, int targetlp, Vector2 startpos, Engine.DamageType type, Action<Color, int> onFinished)
		{
		}

		public void OnEffectEnd()
		{
		}

		public void Reset()
		{
		}
	}
}
