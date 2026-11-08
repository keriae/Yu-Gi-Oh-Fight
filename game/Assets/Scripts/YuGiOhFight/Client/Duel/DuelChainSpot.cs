namespace YuGiOhFight.Client.Duel
{
	public class DuelChainSpot : UnityEngine.MonoBehaviour
	{
		private enum Step
		{
			WaitChainResolveBegin = 0,
			WaitChainResolveEnd = 1,
			WaitTimelineEnd = 2,
			Idle = 3
		}

		private const string LABEL_ICON_CHAINWRAP = "ChainWrapSet";

		private const string LABEL_ICON_NUM0 = "DummyNum01";

		private const string LABEL_ICON_NUM1 = "DummyNum02_01";

		private const string LABEL_ICON_NUM2 = "DummyNum02_02";

		private Presentation.Elements.ElementObjectManager m_EOManager_Origin;

		private UnityEngine.Vector3 m_OriginPos;

		private Step m_Step;

		private bool m_OnChainResolveBeginFlag;

		private bool m_OnChainResolveEndFlag;

		private YgomGame.Duel.DuelIconSprites m_DuelIconSprites => null;

		private Presentation.Elements.ElementObjectManager m_EOManager => GetComponent<Presentation.Elements.ElementObjectManager>();

		private UnityEngine.Playables.PlayableDirector m_PlayableDirector => GetComponent<UnityEngine.Playables.PlayableDirector>();

		private Presentation.Timeline.LabeledPlayableController m_LPController;

        private void Awake()
        {
            m_LPController = Presentation.Timeline.LabeledPlayableController.Create(GetComponent<UnityEngine.Playables.PlayableDirector>());
			foreach (UnityEngine.SpriteRenderer sr in transform.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true))
				sr.sortingLayerName = "ChainSpot";
        }

		public void Play(int chainNum, GPS p, bool modelExist)
		{
            if (chainNum < 10)
            {
                m_EOManager.GetElement("DummyNum02_01").SetActive(false);
                m_EOManager.GetElement("DummyNum02_02").SetActive(false);
                m_EOManager.GetElement<UnityEngine.SpriteRenderer>("DummyNum01").sprite = GetNumSprite(chainNum);
            }
            else
            {
                m_EOManager.GetElement("DummyNum01").SetActive(false);
                int tensDigit = (chainNum / 10) % 10;
                int onesDigit = chainNum % 10;
                m_EOManager.GetElement<UnityEngine.SpriteRenderer>("DummyNum02_01").sprite = GetNumSprite(tensDigit);
                m_EOManager.GetElement<UnityEngine.SpriteRenderer>("DummyNum02_02").sprite = GetNumSprite(onesDigit);
            }

			UnityEngine.Vector3 position = GameCard.GetCardPosition(p);
			float offsetY = 0f;
			if(p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Hand))
				offsetY = 3f;
			else if(p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.Deck, MDPro3.Duel.YGOSharp.CardLocation.Extra))
                offsetY = 0.11f * Runtime.Program.instance.ocgcore.GetLocationCardCount((MDPro3.Duel.YGOSharp.CardLocation)p.location, p.controller);
			position.y += offsetY;
			transform.position = position;
			transform.localScale = GameCard.GetCardScale(p);

            if (!modelExist)
                Destroy(m_EOManager.GetElement(LABEL_ICON_CHAINWRAP));
			if(p.InLocation(MDPro3.Duel.YGOSharp.CardLocation.MonsterZone) && p.InPosition(MDPro3.Duel.YGOSharp.CardPosition.Defence))
                m_EOManager.GetElement<UnityEngine.Transform>(LABEL_ICON_CHAINWRAP).localEulerAngles = new UnityEngine.Vector3(0, 90, 0);
            if (chainNum == 1)
                Runtime.AudioManager.nextMuteSE = "SE_DUELCHAIN_01";
        }

        UnityEngine.Sprite GetNumSprite(int num)
		{
			switch(num)
			{
				case 0:
					return Runtime.TextureManager.container.chainCircleNum0;
                case 1:
                    return Runtime.TextureManager.container.chainCircleNum1;
                case 2:
                    return Runtime.TextureManager.container.chainCircleNum2;
                case 3:
                    return Runtime.TextureManager.container.chainCircleNum3;
                case 4:
                    return Runtime.TextureManager.container.chainCircleNum4;
                case 5:
                    return Runtime.TextureManager.container.chainCircleNum5;
                case 6:
                    return Runtime.TextureManager.container.chainCircleNum6;
                case 7:
                    return Runtime.TextureManager.container.chainCircleNum7;
                case 8:
                    return Runtime.TextureManager.container.chainCircleNum8;
                case 9:
                    return Runtime.TextureManager.container.chainCircleNum9;
				default:
					return Runtime.TextureManager.container.typeNone;
            }
        }

		System.Collections.IEnumerator PlayLabel(int type)
		{
			while (m_LPController.loopMixerBehaviour == null)
				yield return null;
			if(type == 0)
                m_LPController.PlayLabel("ChainResolveBegin", m_LPController.loopMixerBehaviour.loopClips[1]);
			else if(type == 1)
                m_LPController.PlayLabel("ChainResolveEnd", (UnityEngine.Timeline.TimelineClip)null);
        }

        public void OnChainResolveBegin()
		{
			StartCoroutine(PlayLabel(0));
		}

		public void OnChainResolveEnd()
		{
            StartCoroutine(PlayLabel(1));
        }

		public void OnChainSetMore()
		{
		}

		private void Update()
		{
		}

		private void WaitChainResolveBegin()
		{
		}

		private void WaitChainResolveEnd()
		{
		}

		private void WaitTimelineEnd()
		{
		}
	}
}
