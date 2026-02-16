# The Great Blazor Tokenizer Mystery: Architecting a C#-Native Theming Engine in .NET 9.0

**Authors:** Will & Gemini  
**Target Framework:** .NET Core 9.0  
**Context:** Developing `MinqViewer`, a zero-configuration, drop-in Blazor component for MongoDB data visualization.

---

## Act I: The Push for Polish and Scoped CSS

As `MinqViewer` grew in complexity—managing pagination, data formatting, modals, and timers—the single `MinqViewer.razor` file swelled to over 1,300 lines of code. A significant portion of this was a massive, inline `<style>` block. 

To improve maintainability, isolate our styles, and polish the product, we decided to break the component down and implement a "Branding Bible." The goal was to extract hardcoded hex values into standard CSS custom properties (variables) so we could easily theme the UI.

We initially reached for **Blazor Scoped CSS** (`.razor.css` files). Scoped CSS in Blazor is fantastic for component libraries: the compiler automatically generates unique identifiers (like `b-1a2b3c4d`) and appends them to your HTML elements and CSS selectors. 

```css
/* MinqViewer.razor.css */
.minq-container {
    --minq-primary: #3f51b5;
    --minq-danger: #f44336;
    --minq-bg-surface: #ffffff;
    /* ... */
}

.minq-button-primary {
    background-color: var(--minq-primary);
    border: 1px solid var(--minq-primary);
}
```

This ensures that our `.minq-button` class will never accidentally overwrite a `.minq-button` class in the parent application consuming our library.

Everything looked beautiful. Until it didn't.

![Working CSS](/Blazor/Themes/working_css.jpeg)

---

## Act II: The Anomaly

Shortly after establishing our scoped CSS and variables, the layout completely collapsed. The browser rendered raw, unstyled HTML. 

![Broken CSS](/Blazor/Themes/broken_css.jpeg)

At first glance, it looked like a classic caching issue or a missing stylesheet reference. 

**Gemini's Initial (and Incorrect) Hypothesis:** I immediately suspected Visual Studio's Hot Reload or a missing `<link>` tag. When Blazor compiles scoped CSS, it bundles it into a file like `YourLibrary.bundle.scp.css`. I assumed the host application was missing this reference, causing the CSS to 404, while Hot Reload was occasionally "ghost-patching" the CSS directly into the browser's memory, creating the illusion of intermittent success.

**Will's Reality Check:** Will quickly corrected this:
1. He was using Rider on a Mac, not Visual Studio.
2. Hot Reload was explicitly disabled.
3. **Crucial Constraint:** `MinqViewer` is designed to be a true, single-file, zero-configuration component. Forcing developers to add a `<link>` tag to their host application's `index.html` was a non-starter. 

This constraint meant we had to abandon `.razor.css` files and move everything back into a master `<style>` block inside the `.razor` file itself. 

But even after moving the variables back into the inline `<style>` block, the CSS continued to randomly break.

---

## Act III: Scientific Debugging

To isolate the issue, Will ran a series of atomic tests. He left the structural CSS in the `<style>` tag (which rendered fine) and isolated the CSS variables, changing one value at a time, followed by a Clean, Rebuild, and Run:

1. Moving `.minq-container` CSS to the scoped file: **Broke.**
2. Changing `--minq-primary` to `#770000`: **Worked.** The site looked normal, and the color reflected.
3. Changing `--minq-primary` to `#00FF00`: **Broke.**
4. Changing `--minq-primary` to `#777777`: **Worked.**
5. Changing `--minq-primary` to `#777778`: **Broke.**

Values like `#777778` are perfectly valid hex colors. There was no logical reason for the browser to reject it, especially when the problem was so intermittent, which meant the browser wasn't the problem—the compiler was.  Gemini continued to insist that the problem was with the host application's `<link>` tag or that hot reload was somehow "ghost patching" the CSS.

**The Houdini Workaround Attempt** Will theorized a bug in how Blazor processes CSS variables and suggested using the CSS Houdini `@property` at-rule to get around a misbehaving parser:

```css
@property --minq-primary {
    syntax: "<color>";
    inherits: true;
    initial-value: #3f51b5;
}
```
The theory was that the `@property` directive might force the parser down a different evaluation path. While clever, further testing revealed that even this succumbed to the same intermittent corruption when specific hex values (like `#770000`) were introduced.

---

## Act IV: The Root Cause Analysis

The culprit was the **Razor Tokenizer/Compiler in .NET 9.0**.

When building a `.razor` file, the Razor engine has to parse three different languages simultaneously: HTML, C#, and CSS. It uses a complex state machine to figure out where one language ends and another begins. 

The tokenizer is notoriously fragile when it encounters large `<style>` blocks heavily populated with:
* Dashes (`--`)
* Curly braces (`{ }`)
* And specifically, **Hashes (`#`)**

In C#, the `#` symbol denotes a preprocessor directive (e.g., `#region`, `#if`, `#pragma`). When the Razor parser scans a `<style>` block and sees something like `#777778`, its regex evaluator likely misinterprets the sequence, panics, and corrupts its internal file buffer. 

Instead of throwing a clear compile-time error, the Razor engine silently gives up on the CSS block, resulting in the style block being dropped or malformed in the final render. Depending on the exact alphanumeric sequence following the `#` (which is why `#770000` behaved differently than `#777778` with a commented value above it), the tokenizer's state machine reacts differently.

At least, this was the working theory that Gemini had in mind.  Ultimately, with a working solution in place, our investigation came to a close-or at least a hiatus.

---

## Act V: The Ultimate Workaround (A C#-Native Theme Engine)

Since we couldn't trust the Razor HTML/CSS tokenizer to safely compile our CSS variables, we decided to blindfold it entirely. 

If we define our CSS variables as a standard C# string and bind them to the `style` attribute of our root `div`, the Razor parser completely ignores the CSS syntax. To the IDE, it's just a C# string. To the browser, it renders as perfectly valid, cascading inline styles.

We built a strongly-typed, extensible theming engine in C#:

```csharp
// ThemeProvider.cs
namespace Maynard.Minq.Blazor.Themes;

public abstract class ThemeProvider
{
    internal abstract int Primary { get; }
    internal abstract int Danger { get; }
    internal abstract int BgSurface { get; }
    // ... other properties

    public override string ToString()
    {
        PropertyInfo[] properties = GetType().GetProperties(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        IEnumerable<string> cssVariables = properties.Select(prop =>
        {
            var value = prop.GetValue(this);
            string formattedValue = value switch
            {
                int hex => $"#{hex:x6}", // Format ints to hex safely in C#
                _ => $"{value}"
            };

            // Convert PascalCase to kebab-case
            string kebabName = Regex.Replace(prop.Name, "([a-z])([A-Z])", "$1-$2").ToLower();
            return $"--minq-{kebabName}: {formattedValue};";
        });

        return string.Join(" ", cssVariables);
    }
}
```

We then implemented a default theme using safe, integer-based hex representations:

```csharp
// DefaultThemeProvider.cs
internal class DefaultThemeProvider : ThemeProvider
{
    internal override int Primary => 0x3F51B5;
    internal override int Danger => 0xF44336;
    internal override int BgSurface => 0xFFFFFF;
    // ...
}
```

Finally, in `MinqViewer.razor`, we evaluated the theme once and applied it:

```razor
@code {
    // Evaluated once per lifecycle
    private string ThemeVariables { get; } = new DefaultThemeProvider().ToString();
}

<CascadingValue Value="this" IsFixed="true">
    <div class="minq-container" style="@ThemeVariables">
        </div>
</CascadingValue>
```

### Conclusion

What started as an infuriating encounter with a "profoundly dumb bug" in the .NET 9.0 Razor tokenizer ultimately forced us into a superior architecture - even if it means it's harder to wrap our heads around.  That's why we documented this headache here.

Not only did we successfully preserve the zero-configuration, single-file drop-in requirement for `MinqViewer`, but we also accidentally built a fully dynamic theming engine. Because the variables are generated via C# reflection, we can easily expose `ThemeProvider` as a `[Parameter]` in the future, allowing end-users to pass in custom Dark Modes or corporate brand themes with zero additional CSS required.