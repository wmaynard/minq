namespace Maynard.Minq.Blazor.Themes;

public class DarkThemeProvider : ThemeProvider
{
    public override string Name => "Dark Mode";
    
    public override int HeaderText => 0xE0E0E0; // Light gray text for the dark gray headers

    // 1. Primary / Brand (Blues - Lightened for dark bg contrast)
    public override int Primary => 0x7986CB; 
    public override int PrimaryHover => 0x9FA8DA; 
    public override int PrimaryLight => 0x283593; // Used for subtle active backgrounds

    // 2. Accents / Info 
    public override int AccentBg => 0x1A237E;
    public override int AccentBorder => 0x283593;
    public override int AccentText => 0x8C9EFF;

    // 3. Danger / Alerts (Reds)
    public override int Danger => 0xEF5350;
    public override int DangerHover => 0xE53935;
    public override int DangerLight => 0x4A0000; // Deep red for hover states
    public override int DangerDisabled => 0x3E2723;

    // 4. Status Badges
    public override int BadgeTrue => 0x4CAF50;
    public override int BadgeFalse => 0xEF5350;
    public override int BadgeFuture => 0x42A5F5;
    public override int BadgePast => 0xFFD54F;
    public override int BadgePastText => 0x212121;

    // 5. Table Headers & Timers
    public override int HeaderBg => 0x252526;
    public override int HeaderBorder => 0x111111;
    public override int TimerText => 0xB0BEC5;

    // 6. Backgrounds / Surfaces
    public override int BgSurface => 0x1E1E1E;
    public override int BgAlt => 0x2D2D30;
    public override int BgEmpty => 0x252526;
    public override int BgDisabled => 0x333333;
    public override int BgHover => 0x3E3E42;
    public override string Overlay => "rgba(0, 0, 0, 0.7)"; // Slightly darker overlay

    // 7. Borders / Lines
    public override int BorderLight => 0x444444;
    public override int Border => 0x333333;
    public override int BorderDark => 0x555555;

    // 8. Text / Typography (Inverted)
    public override int TextDark => 0xE0E0E0; // 'Dark' is now the lightest main text
    public override int TextMain => 0xCCCCCC;
    public override int TextMuted => 0x999999;
    public override int TextDisabled => 0x666666;
    public override int TextLight => 0x444444;
}