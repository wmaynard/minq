using System;
using System.Linq.Expressions;
using Maynard.Minq.Models;

namespace Maynard.Minq.Extensions;

public static class MinqExpressionExtension
{
    public static string GetFieldName<T>(this Expression<Func<T, object>> field) where T : MinqDocument => MinqClient<T>.Render(field);
}