namespace YuGiOhFight.Client.Presentation.Extensions
{
    public static class TransformExtensions
    {
        public static void DestroyAllChildren(this UnityEngine.Transform transform)
        {
            for (int i = 0; i < transform.childCount; i++)
                UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
        }

        public static UnityEngine.Transform GetChildByName(this UnityEngine.Transform parent, string childName)
        {
            foreach (var t in parent.GetComponentsInChildren<UnityEngine.Transform>(true))
                if (t.name == childName)
                    return t;
            return null;
        }

        public static System.Collections.Generic.List<UnityEngine.Transform> GetChildrenByName(this UnityEngine.Transform parent, string childrenName)
        {
            var value = new System.Collections.Generic.List<UnityEngine.Transform>();
            foreach (var t in parent.GetComponentsInChildren<UnityEngine.Transform>(true))
                if (t.name == childrenName)
                    value.Add(t);
            return value;
        }

        public static void DestroyChildByName(this UnityEngine.Transform parent, string childName)
        {
            UnityEngine.Object.Destroy(parent.GetChildByName(childName).gameObject);
        }

        public static void DestroyChildrenByName(this UnityEngine.Transform parent, string childrenName)
        {
            var cs = parent.GetChildrenByName(childrenName);
            foreach (var c in cs)
                UnityEngine.Object.Destroy(c.gameObject);
        }
    }
}
