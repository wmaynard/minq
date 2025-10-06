using MongoDB.Driver;

namespace Maynard.Minq.Extensions;

public static class FindFluentExtension
{
    public static IFindFluent<T, T> ApplySortDefinition<T>(this IFindFluent<T, T> finder, SortDefinition<T> sort) => sort == null
        ? finder
        : finder.Sort(sort);
}