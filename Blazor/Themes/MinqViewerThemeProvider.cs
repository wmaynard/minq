using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Maynard.Minq.Blazor.Themes;

public abstract partial class MinqViewerThemeProvider
{
    public abstract string Name { get; }
    
    public abstract int HeaderText { get; }
    
    // 1. Primary / Brand (Blues)
    public abstract int Primary { get; }
    public abstract int PrimaryHover { get; }
    public abstract int PrimaryLight { get; }

    // 2. Accents / Info (Light Blues)
    public abstract int AccentBg { get; }
    public abstract int AccentBorder { get; }
    public abstract int AccentText { get; }

    // 3. Danger / Alerts (Reds)
    public abstract int Danger { get; }
    public abstract int DangerHover { get; }
    public abstract int DangerLight { get; }
    public abstract int DangerDisabled { get; }

    // 4. Status Badges
    public abstract int BadgeTrue { get; }
    public abstract int BadgeFalse { get; }
    public abstract int BadgeFuture { get; }
    public abstract int BadgePast { get; }
    public abstract int BadgePastText { get; }

    // 5. Table Headers & Timers
    public abstract int HeaderBg { get; }
    public abstract int HeaderBorder { get; }
    public abstract int TimerText { get; }

    // 6. Backgrounds / Surfaces
    public abstract int BgSurface { get; }
    public abstract int BgAlt { get; }
    public abstract int BgEmpty { get; }
    public abstract int BgDisabled { get; }
    public abstract int BgHover { get; }
    public abstract string Overlay { get; } // String to support rgba()

    // 7. Borders / Lines
    public abstract int BorderLight { get; }
    public abstract int Border { get; }
    public abstract int BorderDark { get; }

    // 8. Text / Typography
    public abstract int TextDark { get; }
    public abstract int TextMain { get; }
    public abstract int TextMuted { get; }
    public abstract int TextDisabled { get; }
    public abstract int TextLight { get; }

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