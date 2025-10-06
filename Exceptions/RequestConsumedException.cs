using System;
using Maynard.Minq.Models;
using Maynard.Minq.Queries;

namespace Maynard.Minq.Exceptions;

public class RequestConsumedException<T>(RequestChain<T> chain) : Exception("The RequestChain was previously consumed by another action.  This is not allowed to prevent accidental DB spam.")
    where T : MinqDocument
{
    public string RenderedFilter { get; set; } = chain.RenderedFilter;
}