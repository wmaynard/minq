namespace Maynard.Minq.Blazor.Themes;

internal class LightThemeProvider : ThemeProvider
{
    public override string Name => "Light Mode";
    
    public override int HeaderText => 0xFFFFFF; // White text for the dark blue headers
    // 1. Primary / Brand (Blues)
    public override int Primary => 0x3F51B5;
    public override int PrimaryHover => 0x303F9F;
    public override int PrimaryLight => 0xE8EAF6;

    // 2. Accents / Info (Light Blues)
    public override int AccentBg => 0xE3F2FD;
    public override int AccentBorder => 0xBBDEFB;
    public override int AccentText => 0x1565C0;

    // 3. Danger / Alerts (Reds)
    public override int Danger => 0xF44336;
    public override int DangerHover => 0xD32F2F;
    public override int DangerLight => 0xFFEBEE;
    public override int DangerDisabled => 0xFFCDD2;

    // 4. Status Badges
    public override int BadgeTrue => 0x388E3C;
    public override int BadgeFalse => 0xD32F2F;
    public override int BadgeFuture => 0x1976D2;
    public override int BadgePast => 0xFBC02D;
    public override int BadgePastText => 0x212121;

    // 5. Table Headers & Timers
    public override int HeaderBg => 0x37474F;
    public override int HeaderBorder => 0x263238;
    public override int TimerText => 0x78909C;

    // 6. Backgrounds / Surfaces
    public override int BgSurface => 0xFFFFFF;
    public override int BgAlt => 0xF0F2F5;
    public override int BgEmpty => 0xF9F9F9;
    public override int BgDisabled => 0xF5F5F5;
    public override int BgHover => 0xF0F0F0;
    public override string Overlay => "rgba(0, 0, 0, 0.5)";

    // 7. Borders / Lines
    public override int BorderLight => 0xEEEEEE;
    public override int Border => 0xE0E0E0;
    public override int BorderDark => 0xCCCCCC;

    // 8. Text / Typography
    public override int TextDark => 0x333333;
    public override int TextMain => 0x555555;
    public override int TextMuted => 0x666666;
    public override int TextDisabled => 0x888888;
    public override int TextLight => 0xAAAAAA;
}