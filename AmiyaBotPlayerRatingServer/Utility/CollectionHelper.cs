namespace AmiyaBotPlayerRatingServer.Utility
{
    public static class CollectionHelper
    {
        public static T GetValueOrSetDefault<TK,T>(this Dictionary<TK,T> dict,TK key,T defaultValue) where TK : notnull
        {
            if(!dict.TryAdd(key, defaultValue))
            {
                return dict[key];
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
