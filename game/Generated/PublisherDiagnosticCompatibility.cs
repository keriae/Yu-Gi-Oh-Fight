using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.EventSystems;
using UnityEngine;
using YgomGame.Card;
using YgomGame.Duel;
using YgomSystem.ElementSystem;
using YgomSystem.UI;

// Preserve the publisher's Unity type, field and callback bindings without
// retaining its diagnostic logging or animator-testing implementation.

[RequireComponent(typeof(Animator))]
public class AnimatorTriggerTest : MonoBehaviour
{
    public string trigger;
    public bool test;
    private Animator animator;

    private void Start() { }
    private void Update() { }
}

public class EventSystemTest : MonoBehaviour, IPointerClickHandler, ISelectHandler
{
    public void OnPointerClick(PointerEventData eventData) { }
    public void OnSelect(BaseEventData eventData) { }
}

namespace YgomGame.Test
{
	public class CardMoveTest : ViewController
	{
		private enum PlayStatus
		{
			Idle = 0,
			Load = 1,
			SummonEffect = 2,
			MonsterCutin = 3,
			CardMove = 4
		}

		[SerializeField]
		private GameObject prefabUI;

		[SerializeField]
		private CardMoveMotionSetting motionSetting;

		[SerializeField]
		private GameObject prefabCard;

		[SerializeField]
		private GameObject prefabFieldMatNear;

		[SerializeField]
		private GameObject prefabFieldMatFar;

		private ElementObjectManager ui;

		private SharedDefinition.Location fromPlayer;

		private int fromPos;

		private bool fromIsAttack;

		private SharedDefinition.Location toPlayer;

		private int toPos;

		private bool toIsAttack;

		private EffectTaskCardMove.LandingType landingType;

		private EffectTaskCardMove.SummonEffectType summonEffectType;

		private bool isStrong;

		private bool isSummonEffect;

		private int cardID;

		private int matCardID;

		private int matCardNum;

		private int tunerLevel;

		private int materialLevel;

		private int tunerNum;

		private DuelEffectPool effectPool;

		private ElementObjectManager matNear;

		private ElementObjectManager matFar;

		private GameObject root;

		private GameObject card;

		private ChainedBezierMotion motion;

		private Vector3 fromPosition;

		private Vector3 toPosition;

		private Quaternion fromRotation;

		private Quaternion toRotation;

		private Vector3 fromScale;

		private Vector3 toScale;

		private SummonEffectBase summonEffect;

		private PlayStatus playStatus;

		private float time;

		public override void NotificationStackEntry()
		{
		}

		private void SetFromPlayer(int v)
		{
		}

		private void SetToPlayer(int v)
		{
		}

		private void SetFromPosition(int v)
		{
		}

		private void SetToPosition(int v)
		{
		}

		private void SetLandingType(int v)
		{
		}

		private void SetSummonEffectType(int v)
		{
		}

		private void SetFromIsAttack(int v)
		{
		}

		private void SetToIsAttack(int v)
		{
		}

		private void SetIsStrongSummon(int v)
		{
		}

		private void SetIsSummonEffect(int v)
		{
		}

		private void SetDestCardID(string v)
		{
		}

		private void SetMatCardID(string v)
		{
		}

		private void SetMatCardNum(string v)
		{
		}

		private void SetTunerLevel(string v)
		{
		}

		private void SetMaterialLevel(string v)
		{
		}

		private void SetTunerNum(string v)
		{
		}

		private void SetupMenu()
		{
		}

		private void Play()
		{
		}

		private (Vector3, Quaternion, Vector3) GetTrans(SharedDefinition.Location location, int position)
		{
			return default((Vector3, Quaternion, Vector3));
		}

		private void Update()
		{
		}

		private void SetCardTransform(float time)
		{
		}
	}
}

namespace YgomGame.Test
{
	public class DuelBattleTest : ViewController
	{
		private enum ZoneType
		{
			Main = 0,
			Ex = 1
		}

		[SerializeField]
		private CameraViewSetting cameraViewSetting;

		[SerializeField]
		private AttackMotionSetting battleRunMotionSetting;

		[SerializeField]
		private Transform objectsParent;

		[SerializeField]
		private Transform duelObjectsParent;

		[SerializeField]
		private GameObject cardPrefab;

		[SerializeField]
		private GameObject nearFieldPrefab;

		[SerializeField]
		private GameObject farFieldPrefab;

		[SerializeField]
		private float matOffset;

		[SerializeField]
		private ElementObjectManager ui;

		private GameObject nearMat;

		private GameObject farMat;

		private DuelEffectPool effectPool;

		private SharedDefinition.Location fromLocation;

		private ZoneType fromZone;

		private int fromPosition;

		private SharedDefinition.Location toLocation;

		private ZoneType toZone;

		private int toPosition;

		private GameObject motionCard;

		private GameObject targetCard;

		private EffectTaskBattleRun.AttackType attackType;

		private Content.Attribute attribute;

		private Util.AttackLevel attackLevel;

		private int motionIndex;

		private bool guard;

		private bool directAttack;

		private bool isLethal;

		private LethalEffect.EffectType effectType;

		private bool reqStart;

		private AttackMotionSetting.MotionInfo motionInfo;

		private bool isPlaying;

		private float currentTime;

		private int playMotionIndex;

		private Vector3 originPosition;

		private Vector3 targetPosition;

		private Quaternion originRotation;

		private Quaternion targetRotation;

		private SimpleEffect moveTrail;

		private bool isStart;

		private bool isTrail;

		private bool isAttack;

		private bool isHit;

		private bool isShoot;

		private ChainedBezierMotion chaindBezierMotion;

		private ChainedBezierMotion shootMotion;

		private SimpleEffect shootEffect;

		private Vector3 shootOriginPosition;

		private Vector3 shootTargetPosition;

		private Quaternion shootOriginRotation;

		private Quaternion shootTargetRotation;

		private EffectTaskBattleRun.AttackEffectInfo attackEffectInfo;

		public List<GameObject> cards
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public void Start()
		{
		}

		private void Update()
		{
		}

		private void SetupObjects()
		{
		}

		private GameObject CreateCard(Transform parent)
		{
			return null;
		}

		private void ClearObjects()
		{
		}

		private void StartMotionTest()
		{
		}

		private Transform GetZone(SharedDefinition.Location location, ZoneType zone_type, int position)
		{
			return null;
		}

		private Transform GetHand(SharedDefinition.Location location)
		{
			return null;
		}

		private void UpdateMotionTest()
		{
		}

		private void PlayTrailEffect()
		{
		}

		private void PlayAttackEffect()
		{
		}

		private void PlayHitEffect()
		{
		}

		private void PlayShootEffect()
		{
		}

		private void SetCardLayer(int layer)
		{
		}
	}
}

namespace YgomGame.Test
{
	public class MonsterCutinTest : ViewController
	{
		[SerializeField]
		private GameObject prefabUI;

		[SerializeField]
		private CardIndividualSetting setting;

		private int cardID;

		private bool useCardParameter;

		private bool isMyself;

		private Content.Attribute attribute;

		private string cardName;

		private MonsterCutinEffect.LevelType levelType;

		private int level;

		private int attack;

		private int defense;

		private bool isOcg;

		private MonsterCutinEffect monsterCutinEffect;

		private bool isPlaying;

		private ElementObjectManager ui;

		public override void NotificationStackEntry()
		{
		}

		private void PlayCutin()
		{
		}
	}
}
