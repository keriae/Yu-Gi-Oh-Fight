namespace YuGiOhFight.Client.Presentation.Timeline
{
	public class SoundPlayableBehaviour : UnityEngine.Playables.PlayableBehaviour
	{
		public SoundPlayableAsset playableAsset;

		public string startLabel;

		public bool skipDuplicate;

		public override void OnGraphStart(UnityEngine.Playables.Playable playable)
		{

        }

        public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info)
		{
            Runtime.AudioManager.PlaySE(startLabel);
        }
    }
}
