using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Maynard.Minq.Blazor.Themes;

public abstract partial class ThemeProvider
{
    // 1. Primary / Brand (Blues)
    internal abstract int Primary { get; }
    internal abstract int PrimaryHover { get; }
    internal abstract int PrimaryLight { get; }

    // 2. Accents / Info (Light Blues)
    internal abstract int AccentBg { get; }
    internal abstract int AccentBorder { get; }
    internal abstract int AccentText { get; }

    // 3. Danger / Alerts (Reds)
    internal abstract int Danger { get; }
    internal abstract int DangerHover { get; }
    internal abstract int DangerLight { get; }
    internal abstract int DangerDisabled { get; }

    // 4. Status Badges
    internal abstract int BadgeTrue { get; }
    internal abstract int BadgeFalse { get; }
    internal abstract int BadgeFuture { get; }
    internal abstract int BadgePast { get; }
    internal abstract int BadgePastText { get; }

    // 5. Table Headers & Timers
    internal abstract int HeaderBg { get; }
    internal abstract int HeaderBorder { get; }
    internal abstract int TimerText { get; }

    // 6. Backgrounds / Surfaces
    internal abstract int BgSurface { get; }
    internal abstract int BgAlt { get; }
    internal abstract int BgEmpty { get; }
    internal abstract int BgDisabled { get; }
    internal abstract int BgHover { get; }
    internal abstract string Overlay { get; } // String to support rgba()

    // 7. Borders / Lines
    internal abstract int BorderLight { get; }
    internal abstract int Border { get; }
    internal abstract int BorderDark { get; }

    // 8. Text / Typography
    internal abstract int TextDark { get; }
    internal abstract int TextMain { get; }
    internal abstract int TextMuted { get; }
    internal abstract int TextDisabled { get; }
    internal abstract int TextLight { get; }

    public override string ToString()
    {
        // Grab all instance properties (Public and NonPublic/Internal)
        PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        IEnumerable<string> cssVariables = properties.Select(prop =>
        {
            object value = prop.GetValue(this);
            
            // Format ints as exactly 6-character lowercase hex strings.
            // Strings (like rgba values) pass through unchanged.
            string formattedValue = value switch
            {
                int hex => $"#{hex:x6}",
                _ => $"{value}"
            };

            // Convert PascalCase (PrimaryHover) to kebab-case (primary-hover)
            string kebabName = KebabRegex().Replace(prop.Name, "$1-$2").ToLower();

            // Using pure string interpolation
            return $"--minq-{kebabName}: {formattedValue};";
        });

        // Join them all into a single inline string
        return string.Join(" ", cssVariables);
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex KebabRegex();
}