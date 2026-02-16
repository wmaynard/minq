namespace Maynard.Minq.Blazor.Themes;

internal class DefaultThemeProvider : ThemeProvider
{
    // 1. Primary / Brand (Blues)
    internal override int Primary => 0x3F51B5;
    internal override int PrimaryHover => 0x303F9F;
    internal override int PrimaryLight => 0xE8EAF6;

    // 2. Accents / Info (Light Blues)
    internal override int AccentBg => 0xE3F2FD;
    internal override int AccentBorder => 0xBBDEFB;
    internal override int AccentText => 0x1565C0;

    // 3. Danger / Alerts (Reds)
    internal override int Danger => 0xF44336;
    internal override int DangerHover => 0xD32F2F;
    internal override int DangerLight => 0xFFEBEE;
    internal override int DangerDisabled => 0xFFCDD2;

    // 4. Status Badges
    internal override int BadgeTrue => 0x388E3C;
    internal override int BadgeFalse => 0xD32F2F;
    internal override int BadgeFuture => 0x1976D2;
    internal override int BadgePast => 0xFBC02D;
    internal override int BadgePastText => 0x212121;

    // 5. Table Headers & Timers
    internal override int HeaderBg => 0x37474F;
    internal override int HeaderBorder => 0x263238;
    internal override int TimerText => 0x78909C;

    // 6. Backgrounds / Surfaces
    internal override int BgSurface => 0xFFFFFF;
    internal override int BgAlt => 0xFAFAFA;
    internal override int BgEmpty => 0xF9F9F9;
    internal override int BgDisabled => 0xF5F5F5;
    internal override int BgHover => 0xF0F0F0;
    internal override string Overlay => "rgba(0, 0, 0, 0.5)";

    // 7. Borders / Lines
    internal override int BorderLight => 0xEEEEEE;
    internal override int Border => 0xE0E0E0;
    internal override int BorderDark => 0xCCCCCC;

    // 8. Text / Typography
    internal override int TextDark => 0x333333;
    internal override int TextMain => 0x555555;
    internal override int TextMuted => 0x666666;
    internal override int TextDisabled => 0x888888;
    internal override int TextLight => 0xAAAAAA;
}