using MongoDB.Driver;

namespace Rumble.Platform.Common.MinqOld;

public static class IFindFluentExtension
{
    public static IFindFluent<T, T> ApplySortDefinition<T>(this IFindFluent<T, T> finder, SortDefinition<T> sort)
    {
        return sort == null
            ? finder
            : finder.Sort(sort);
    }
}