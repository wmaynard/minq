# MINQ Viewer / Dashboard for Blazor

MinqViewer is a zero-configuration, drop-in Blazor component library designed for seamless MongoDB data visualization, management, and administration. 

Built with developers in mind, MinqViewer uses reflection to automatically generate data grids, modals, and forms directly from your existing C# models and service contracts. Simply drop the component into your page, point it at a service, and you instantly get a fully featured, themeable dashboard with pagination, auto-refreshing timers, and dynamic settings.

![Minq Dashboard](/Documentation/Images/minq_dashboard.png)
![Minq Viewer Settings](/Documentation/Images/minq_viewer_settings.png)

---

## Quick Start

The library exposes two public components: `MinqDashboard` (a tabbed container) and `MinqViewer` (the data grid). 

To create a tabbed administration panel, wrap your viewers in a dashboard and provide the `Contract` (your data service type) for each viewer:

```html
@using Maynard.Minq.Blazor

<MinqDashboard Title="System Administration">
    <MinqViewer Contract="@typeof(AccountsMinq)" TabTitle="Users" />
    <MinqViewer Contract="@typeof(OrdersMinq)" TabTitle="Order History" />
</MinqDashboard>
```

That's it. No CSS imports or JavaScript configuration required.

#### Note: At this time, the components have only been tested with InteractiveServer enabled.  We plan on adding support for static pages in the future.

## Available Settings

MinqViewer places heavy emphasis on user preference. Every viewer includes a highly configurable settings panel that persists locally in the user's browser.

| Setting                       | Description                                                                                                                   |
|:------------------------------|:------------------------------------------------------------------------------------------------------------------------------|
| **Page Size**                 | The number of records to fetch and display per page.                                                                          |
| **Refresh Rate**              | The auto-refresh interval (in seconds) for fetching new data. If set to 0, the timer is disabled.                             |
| **Theme**                     | Toggles the UI between Light Mode, Dark Mode, or any custom themes discovered in the assembly.                                |
| **Font Size**                 | Adjusts the text size within the data grid.                                                                                   |
| **Pinned Column Width**       | Sets the exact pixel width for any columns marked as "Sticky".                                                                |
| **Unpinned Column Max Width** | Sets the maximum pixel width for standard columns before truncating text with an ellipsis.                                    |
| **Choose Columns**            | Opens a picker to selectively hide or show specific columns in the grid.                                                      |
| **Flatten Nested Objects**    | When enabled, parses nested JSON/BSON sub-documents and pulls their properties up as top-level table columns.                 |
| **Hide Default Values**       | Hides properties that contain default types (e.g., `0`, `null`, `""`, empty arrays/objects) to reduce visual clutter.         |
| **Timestamp Format**          | Converts numeric timestamps into Local Time, UTC Time, raw Unix, or human-readable Elapsed Time (e.g., "in 11d09h", "2d14h"). |
| **On Click**                  | Determines the action taken when a row is clicked: Select Text, Edit Record, or Delete Record (if permissions allow).         |

> **Pro Tip:** If the **Refresh Rate** is set to a value greater than `0`, you can click the visual countdown timer in the top right corner of the viewer to quickly pause and resume the auto-refresh cycle!

### Feature Customization

While MinqViewer generates columns automatically using reflection, you can fine-tune how specific properties are displayed and interacted with using the `[MinqView]` attribute.

By applying this attribute to properties on your C# data models, you can easily control the following behavior:

* **Make columns sticky**: Pins the column to the left side of the table so it remains visible while scrolling horizontally. *(Warning: Be careful with this feature! Pinning too many columns can easily eat up the entire screen real estate on smaller displays).*
* **Change the display order**: Adjusts the left-to-right display sequence. Columns with lower numbers appear first (the default is `int.MaxValue`).
* **Make a field ReadOnly**: Prevents the specific field from being modified when a user opens the Edit Record modal.

**Example:**

```csharp
using Maynard.Minq.Attributes;

public class UserAccount : MinqDocument
{
    // Pins the ID to the left, ensures it's the very first column, and prevents editing
    [MinqView(sticky: true, order: 0, readOnly: true)]
    public string Id { get; set; }

    // Ensures the email appears immediately after the ID
    [MinqView(order: 1)]
    public string EmailAddress { get; set; }

    // Renders normally 
    public string DisplayName { get; set; }
}
```

#### Note: Nested objects follow an order hierarchy.  If you flatten objects for viewing in the table, their respective columns will replace the parent object's original place in the display order.  If a Parent has Order 10 and a Child has Order 1, it's similar to treating it as 10.1.  It won't "jump" out of line.

---

## Component Reference

### `<MinqViewer>` Parameters

The viewer is highly configurable via parameters to ensure data safety and transparency:

| Parameter                | Type     | Description                                                                                                                                                                     |
|:-------------------------|:---------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Contract`               | `Type`   | **(Required)** The MINQ service the viewer will use to fetch data.                                                                                                              |
| `Title`                  | `string` | The text displayed on the tab when nested inside a `<MinqDashboard>`. Defaults to the Contract name.                                                                            |
| `IsReadOnly`             | `bool`   | Completely locks down the viewer. Disables edit/delete UI and prevents executing mutating methods via reflection. Perfect for customer transparency portals.                    |
| `MinqViewerDeletionMode` | `Enum`   | Controls deletion permissions (`None`, `SingleRecord`, `Collection`, `SingleRecordAdminOnly`, `CollectionAdminOnly`).                                                           |
| `IsAdmin`                | `bool`   | Informs the viewer if the current user holds administrative privileges (evaluates against `AdminOnly` deletion modes).                                                          |
| `CustomQuery`            | `string` | The exact string name of a custom method on your contract to use for data fetching instead of the default, which pages through all records.  Use `nameof()` for best practices. |

---

## Advanced Usage

### Custom Queries

By default, MinqViewer looks for a method named `PageAllRecords` on your provided `Contract`. However, you can easily instruct the viewer to use a specific, filtered query by passing the method name to the `CustomQuery` parameter.

**1. Define your method in your MINQ service:**
*(Note: The method signature must accept `int pageSize`, `int pageNumber`, and `out long remaining` to support the viewer's pagination engine).*

```csharp
// Example: Limiting a viewer to only show the last month's worth of new accounts
public Account[] ViewLastMonthsSignups(int pageSize, int pageNumber, out long remaining) 
{
    return mongo
        .Where(query => query.GreaterThanOrEqualTo(db => db.CreatedOn, Timestamp.OneMonthAgo))
        .Sort(sort => sort.OrderByDescending(model => model.CreatedOn))
        .Page(size: pageSize, number: pageNumber, out remaining);
}
```

This query limits the results to only those accounts created in the last month; a common use case for customer service.

**2. Point the viewer to your method:**
```html
<MinqViewer 
    Contract="@typeof(IAccountService)" 
    TabTitle="Recent Signups" 
    CustomQuery="ViewLastMonthsSignups" 
    IsReadOnly="true" />
```

---

## Custom Theming

MinqViewer utilizes a powerful, C#-native theming engine that automatically cascades CSS variables down the component tree, completely bypassing traditional stylesheet limitations.

The library automatically scans your application via reflection for any classes inheriting from `ThemeProvider`. If it finds them, they are instantly added to the user's settings dropdown!

To create a custom theme, simply inherit from `LightThemeProvider` or `DarkThemeProvider` and override the colors you want to change.

### Example: The "Thyme" Theme

Here is a custom theme inspired by the herb Thyme, utilizing earthy greens. The inline comments indicate the primary elements each variable controls:

```csharp
using Maynard.Minq.Blazor.Themes;

public class ThymeThemeProvider : MinqViewerThemeProvider
{
    public override string Name => "Thyme";

    // Brand Colors
    public override int Primary => 0x2E7D32;       // Controls primary buttons, active tabs, toggle switches
    public override int PrimaryHover => 0x1B5E20;  // Darker green for primary button hover states
    public override int PrimaryLight => 0xE8F5E9;  // Very pale green for active button backgrounds
    
    // Accents & Info
    public override int AccentBg => 0xF1F8E9;      // Soft mossy background for table row hover states
    public override int AccentBorder => 0xC5E1A5;  // Light green borders for shared alerts
    public override int AccentText => 0x33691E;    // Dark earthy text for shared alerts
    
    // Danger / Alerts
    public override int Danger => 0xF44336;        // Standard danger red
    public override int DangerHover => 0xD32F2F;   // Darker red for hover states
    public override int DangerLight => 0xFFEBEE;   // Pale red for danger button backgrounds
    public override int DangerDisabled => 0xFFCDD2;// Muted red for disabled destructive actions

    // Table Headers
    public override int HeaderBg => 0x33691E;      // Solid earthy green for the sticky table headers & slider bubbles
    public override int HeaderText => 0xFFFFFF;    // Crisp white text to ensure contrast against the dark headers
    public override int HeaderBorder => 0x1B5E20;  // Darker green bottom border for table header depth
    public override int TimerText => 0x78909C;     // Muted slate gray for the countdown timer text
    
    // Backgrounds & Surfaces
    public override int BgSurface => 0xFFFFFF;     // Pure white for main application surface
    public override int BgAlt => 0xFAFAFA;         // Slight off-white for striped table rows and tab tracks
    public override int BgEmpty => 0xF9F9F9;       // Off-white for empty state boxes
    public override int BgDisabled => 0xF5F5F5;    // Light gray for disabled inputs
    public override int BgHover => 0xF0F0F0;       // Light gray for standard button hover states
    public override string Overlay => "rgba(0, 0, 0, 0.5)"; // Semi-transparent black for modal backdrops
    
    // Borders
    public override int BorderLight => 0xEEEEEE;   // Very faint borders for subtle dividers
    public override int Border => 0xE0E0E0;        // Standard structural borders
    public override int BorderDark => 0xCCCCCC;    // Slightly darker borders for inputs and buttons
    
    // Typography
    public override int TextDark => 0x333333;      // Near-black for headings and primary text
    public override int TextMain => 0x555555;      // Standard dark gray for body text
    public override int TextMuted => 0x666666;     // Medium gray for secondary info
    public override int TextDisabled => 0x888888;  // Lighter gray for disabled text
    public override int TextLight => 0xAAAAAA;     // Very faint text for placeholder hints

    // Badges
    public override int BadgeTrue => 0x4CAF50;     // Standard success green for 'True' badges
    public override int BadgeFalse => 0xD32F2F;    // Standard danger red for 'False' badges
    public override int BadgeFuture => 0x1976D2;   // Standard blue for future timestamps
    public override int BadgePast => 0xFBC02D;     // Standard amber/yellow for past timestamps
    public override int BadgePastText => 0x212121; // Dark text to contrast the yellow 'past' badge
}
```

It might seem like a lot of values to override, but the goal is to provide granular control.

Simply placing this class anywhere in your executing assembly will make "Thyme (Earthy Green)" instantly available in the MinqViewer settings panel!  Ignore your IDE if it warns you it's unused - it's just loaded at runtime via reflection.

---

## End-User Settings & Sharing

MinqViewer hands control over to the user. Every viewer includes a settings panel allowing users to:
* Adjust **Page Size** and **Refresh Intervals**.
* Toggle **UI Themes** and adjust **Table Font Size**.
* Set custom column widths for both Pinned and Unpinned columns.
* Hide or show specific columns using the **Column Picker**.
* Flatten nested JSON objects or hide default/null values.
* Change Timestamp formats (Local, UTC, Unix, or Elapsed).

**Share Links:** Users can click "Copy Share Link" to generate a URL containing a Base64 encoded payload of their exact current layout and settings. When a colleague opens the link, the viewer temporarily pauses their local settings and renders the shared view perfectly!

## Roadmap

* Support FilterChains for filtering results more directly rather than requiring a custom query.  Ideally this can be built into a UI for filters to be built, run, and saved at runtime.
* Activity logging to record all modification or destructive operations performed by users.
* Chart generation and other telemetry analysis dashboards.
* Tie the internal Log class into an internal MINQ to provide an easy capture for all logging events, configurable through startup.
* More granular access control via attribute decorations for MINQ models.