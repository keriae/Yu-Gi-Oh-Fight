using System.Collections.Generic;
using UnityEngine;

namespace YgomSystem.UI
{
	public class TweenLauncher : MonoBehaviour
	{
		public List<TweenInfo> infoList;

		[SerializeField]
		private bool generateTweensOnAwake;

		private void Awake()
		{
            if (generateTweensOnAwake)
                GenerateTweens();
        }

		public void GenerateTweens()
		{
            if (infoList == null)
                return;

            for (int i = 0; i < infoList.Count; i++)
            {
                var info = infoList[i];
                if (info == null)
                    continue;

                if (info is TweenGroupInfo group)
                {
                    if (group.infoList == null)
                        continue;
                    for (int j = 0; j < group.infoList.Count; j++)
                    {
                        var sub = group.infoList[j];
                        if (sub == null)
                            continue;
                        if (sub is TweenGroupInfo)
                        {
                            Debug.LogWarning($"[TweenLauncher] Nested TweenGroupInfo not supported: {sub.name}");
                            continue;
                        }
                        GenerateTween(sub, group.prefix, group.suffix);
                    }
                }
                else
                {
                    GenerateTween(info, "", "");
                }
            }
        }

		private void GenerateTween(TweenInfo baseInfo, string prefix, string suffix)
		{
            if (!(baseInfo is TweenGenerateInfo gen))
                return;

            Tween tween = CreateTweenComponent(gen);
            if (tween == null)
            {
                Debug.LogWarning($"[TweenLauncher] Unsupported TweenInfo type: {baseInfo.GetType().Name}");
                return;
            }

            tween.label = prefix + gen.label + suffix;
            tween.easing = gen.easing;
            tween.style = gen.style;
            tween.duration = gen.duration;
            tween.startDelay = gen.startDelay;
            tween.ignoreTimeScale = gen.ignoreTimeScale;
            tween.curve = null;

            ApplyInfoFields(tween, baseInfo);
        }

        private Tween CreateTweenComponent(TweenGenerateInfo gen)
        {
            switch (gen)
            {
                case TweenAlphaInfo _: return gameObject.AddComponent<TweenAlpha>();
                case TweenAlphaToInfo _: return gameObject.AddComponent<TweenAlphaTo>();
                case TweenColorInfo _: return gameObject.AddComponent<TweenColor>();
                case TweenColorToInfo _: return gameObject.AddComponent<TweenColorTo>();
                case TweenPositionInfo _: return gameObject.AddComponent<TweenPosition>();
                case TweenPositionToInfo _: return gameObject.AddComponent<TweenPositionTo>();
                case TweenRotationInfo _: return gameObject.AddComponent<TweenRotation>();
                case TweenRotationToInfo _: return gameObject.AddComponent<TweenRotationTo>();
                case TweenScaleInfo _: return gameObject.AddComponent<TweenScale>();
                case TweenScaleToInfo _: return gameObject.AddComponent<TweenScaleTo>();
                default: return null;
            }
        }

        private void ApplyInfoFields(Tween tween, TweenInfo info)
        {
            switch (info)
            {
                case TweenAlphaInfo alpha:
                    var tAlpha = (TweenAlpha)tween;
                    tAlpha.from = alpha.from;
                    tAlpha.to = alpha.to;
                    tAlpha.isRecusive = alpha.isRecusive;
                    break;
                case TweenAlphaToInfo alphaTo:
                    var tAlphaTo = (TweenAlphaTo)tween;
                    tAlphaTo.to = alphaTo.to;
                    tAlphaTo.isRecusive = alphaTo.isRecusive;
                    break;
                case TweenColorInfo color:
                    var tColor = (TweenColor)tween;
                    tColor.fromLabel = color.fromLabel;
                    tColor.from = color.from;
                    tColor.toLabel = color.toLabel;
                    tColor.to = color.to;
                    tColor.isOverride = color.isOverride;
                    tColor.isRecusive = color.isRecusive;
                    break;
                case TweenColorToInfo colorTo:
                    var tColorTo = (TweenColorTo)tween;
                    if (tColorTo.toLabel == null)
                        tColorTo.toLabel = new ColorLabelProperty();
                    tColorTo.toLabel.label = colorTo.toLabel;
                    tColorTo.toLabel.rgba = colorTo.to;
                    tColorTo.to = colorTo.to;
                    tColorTo.isRecusive = colorTo.isRecusive;
                    break;
                case TweenPositionInfo position:
                    var tPosition = (TweenPosition)tween;
                    tPosition.from = position.from;
                    tPosition.to = position.to;
                    break;
                case TweenPositionToInfo positionTo:
                    var tPositionTo = (TweenPositionTo)tween;
                    tPositionTo.to = positionTo.to;
                    break;
                case TweenRotationInfo rotation:
                    var tRotation = (TweenRotation)tween;
                    tRotation.from = rotation.from;
                    tRotation.to = rotation.to;
                    tRotation.quaternionLerp = rotation.quaternionLerp;
                    break;
                case TweenRotationToInfo rotationTo:
                    var tRotationTo = (TweenRotationTo)tween;
                    tRotationTo.to = rotationTo.to;
                    tRotationTo.quaternionLerp = rotationTo.quaternionLerp;
                    break;
                case TweenScaleInfo scale:
                    var tScale = (TweenScale)tween;
                    tScale.from = scale.from;
                    tScale.to = scale.to;
                    break;
                case TweenScaleToInfo scaleTo:
                    var tScaleTo = (TweenScaleTo)tween;
                    tScaleTo.to = scaleTo.to;
                    break;
            }
        }
    }
}
