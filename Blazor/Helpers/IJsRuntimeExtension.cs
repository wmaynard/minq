using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Maynard.Minq.Blazor.Helpers;

internal static class IJsRuntimeExtension
{
    private const string CLIPBOARD = "navigator.clipboard.writeText";
    public static ValueTask CopyToClipboard(this IJSRuntime js, string text) => js.InvokeVoidAsync(CLIPBOARD, text);
}