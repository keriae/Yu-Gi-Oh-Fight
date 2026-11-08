using UnityEngine;

namespace YuGiOhFight.Client.Duel
{
    // Keep controller and hit-target identity while replacing their presentation.
    internal sealed class DuelSceneBindings
    {
        internal readonly Transform[] MateAnchors;
        internal readonly GameObject[] CosmeticRoots;

        internal DuelSceneBindings(Transform nearMate, Transform farMate, GameObject nearStand, GameObject farStand,
            Presentation.Fields.BgEffectManager nearGrave, Presentation.Fields.BgEffectManager farGrave)
        {
            MateAnchors = new[] { nearMate, farMate };
            CosmeticRoots = new[] { nearStand, farStand, nearGrave.gameObject, farGrave.gameObject };
        }

        internal bool ContainsMate(Transform transform)
        {
            foreach (var anchor in MateAnchors)
                if (anchor != null && (transform == anchor || transform.IsChildOf(anchor))) return true;
            return false;
        }
    }
}
