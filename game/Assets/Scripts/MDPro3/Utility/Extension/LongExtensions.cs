using MDPro3.Duel.YGOSharp;

namespace MDPro3
{
    public static class LongExtensions
    {
        public static bool HasFlag(this long filter, long type)
        {
            return (filter & type) > 0;
        }

        public static bool HasType(this long filter, CardType type)
        {
            return HasFlag(filter, (long)type);
        }

        public static bool HasAllTypes(this long filter, CardType type1, CardType type2)
        {
            return filter.HasType(type1) && filter.HasType(type2);
        }

        public static bool HasType(this long filter, CardPro3Type type)
        {
            return HasFlag(filter, (long)type);
        }

        public static bool InPool(this long filter, CardPool pool)
        {
            return HasFlag(filter, (long)pool);
        }
    }
}
