using System;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.Common.MinqOld;

public class RequestConsumedException<T> : Exception where T : PlatformCollectionDocument
{
    public string RenderedFilter { get; set; }
    
    public RequestConsumedException(RequestChain<T> chain) : base("The RequestChain was previously consumed by another action.  This is not allowed to prevent accidental DB spam.")
    {
        RenderedFilter = chain.RenderedFilter;
    }
}