using MongoDB.Driver;

namespace Maynard.Minq.Minq.Extensions;

public static class FindFluentExtension
{
    public static IFindFluent<T, T> ApplySortDefinition<T>(this IFindFluent<T, T> finder, SortDefinition<T> sort)
    {
        return sort == null
            ? finder
            : finder.Sort(sort);
    }
}