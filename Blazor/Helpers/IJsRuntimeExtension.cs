using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Maynard.Minq.Blazor.Helpers;

internal static class IJsRuntimeExtension
{
    private const string CLIPBOARD = "navigator.clipboard.writeText";
    private const string STORAGE_SET_ITEM = "localStorage.setItem";
    private const string STORAGE_GET_ITEM = "localStorage.getItem";
    public static ValueTask CopyToClipboard(this IJSRuntime js, string text) => js.InvokeVoidAsync(CLIPBOARD, text);
    public static ValueTask Store(this IJSRuntime js, string key, string value) => js.InvokeVoidAsync(STORAGE_SET_ITEM, key, value);
    public static ValueTask<string> Load(this IJSRuntime js, string key) => js.InvokeAsync<string>(STORAGE_GET_ITEM, key);

}