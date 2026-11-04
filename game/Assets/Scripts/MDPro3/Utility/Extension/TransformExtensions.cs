using System.Collections.Generic;
using UnityEngine;

namespace MDPro3
{
    public static class TransformExtensions
    {
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = 0; i < transform.childCount; i++)
                Object.Destroy(transform.GetChild(i).gameObject);
        }

        public static Transform GetChildByName(this Transform parent, string childName)
        {
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == childName)
                    return t;
            return null;
        }

        public static List<Transform> GetChildrenByName(this Transform parent, string childrenName)
        {
            var value = new List<Transform>();
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == childrenName)
                    value.Add(t);
            return value;
        }

        public static void DestroyChildByName(this Transform parent, string childName)
        {
            Object.Destroy(parent.GetChildByName(childName).gameObject);
        }

        public static void DestroyChildrenByName(this Transform parent, string childrenName)
        {
            var cs = parent.GetChildrenByName(childrenName);
            foreach (var c in cs)
                Object.Destroy(c.gameObject);
        }
    }
}
