namespace Maynard.Minq.Blazor.Models;

public class MinqRadioOption<T>
{
    public T Value { get; set; }
    public string Label { get; set; }
    public bool IsDisabled { get; set; }
}