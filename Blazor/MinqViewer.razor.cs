using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using Maynard.Logging;
using Maynard.Minq.Attributes;
using Maynard.Minq.Blazor.Enums;
using Maynard.Minq.Blazor.Helpers;
using Maynard.Minq.Blazor.Models;
using Maynard.Minq.Blazor.Themes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;


namespace Maynard.Minq.Blazor;

/// <summary>
/// Main viewer component for Minq data visualization.  Be very aware of what data you are exposing to users!  This component
/// is primarily aimed at developers and administrators who have elevated privileges to view and manage data.  To lock this behind
/// IAM or any other form of access control, be sure to pass in a boolean value to IsAdmin.  At this time, that is as granular
/// as the control gets for viewing data out of the box.  If you need something more specific, you can leverage Blazor with @if
/// and the CustomQuery feature to tailor some data, but we're aware this is just a workaround.
/// </summary>
public partial class MinqViewer
{
    #region Parameters
    [CascadingParameter]
    public HttpContext HttpContext { get; set; }
    
    /// <summary>
    /// The type of MINQ to use for the viewer.  Use the typeof() operator on your MINQ class to generate a table from it.
    /// </summary>
    [Parameter]
    public Type Contract { get; set; }

    /// <summary>
    /// A custom paging function to change the default behavior of the viewer.  The parameter signature must match the stock
    /// paging method PageAllRecords(int pageSize, int pageNumber, out long remaining), where pageSize is zero-indexed.
    /// </summary>
    [Parameter]
    public string CustomQuery { get; set; }

    /// <summary>
    /// Allows deletion of individual records or of the entire collection.  We strongly recommend that you disable this
    /// in production environments as it is a destructive operation that can't easily be undone, and will be accessible by
    /// anyone who can view this tool.  This enum supports flags.
    /// </summary>
    [Parameter]
    public MinqDeletion DeletionMode { get; set; } = MinqDeletion.None;

    /// <summary>
    /// Determines whether or not a user has administrator permissions.  This flag is useless if <see cref="DeletionMode"/>
    /// does not have any Admin flags.
    /// </summary>
    [Parameter]
    public bool IsAdmin { get; set; } = false;
    #endregion Parameters
    
    
    
    
    
    
    // Expose the dynamically discovered themes to the UI
    public static readonly IReadOnlyList<ThemeProvider> AvailableThemes = ThemeManager.GetAvailableThemes();
    
    public string SelectedThemeName { get; set; } = "Dark Mode";

    // Dynamically render the CSS variables based on the selected theme
    // For an explanation of why this was needed, see the markdown file in Themes.
    private string ThemeVariables => AvailableThemes
        .FirstOrDefault(t => t.Name == SelectedThemeName)?.ToString() 
        ?? new LightThemeProvider().ToString();

    public event Action OnSecondTicked;

    public MinqTimer RefreshTimerComponent { get; set; }
    
    public PeriodicTimer ElapsedTimer { get; set; }
    public CancellationTokenSource ElapsedCts { get; set; } = new CancellationTokenSource();

    public bool IsViewingSharedState { get; set; } = false;

    // Default Settings
    public int PageSize { get; set; } = 25;
    public int RefreshInterval { get; set; } = 30;
    public int TableFontSize { get; set; } = 14;
    public int PinnedColumnWidth { get; set; } = 200;
    public int MaxColumnWidth { get; set; } = 400;
    
    public TimestampFormatOption TimestampFormat { get; set; } = TimestampFormatOption.Local;
    public bool FlattenJsonProperties { get; set; } = false;
    public bool HideDefaultValues { get; set; } = false;
    public RowClickBehaviorOption RowClickBehavior { get; set; } = RowClickBehaviorOption.SelectText;
    
    public int PageNumber { get; set; } = 0;
    public long TotalRecords { get; set; } = 0;
    public int TotalPages { get; set; } = 0;
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; } = false;
    public string ErrorMessage { get; set; } = string.Empty;

    public bool IsSettingsOpen { get; set; } = false;
    public bool IsColumnPickerOpen { get; set; } = false;
    public bool IsEditorOpen { get; set; } = false;
    public bool IsDeleteModalOpen { get; set; } = false;
    public bool IsDeleteAllModalOpen { get; set; } = false;
    public object SelectedRecord { get; set; } = null;

    public bool CanDeleteSingle => DeletionMode.HasFlag(MinqDeletion.SingleRecord) || (IsAdmin && DeletionMode.HasFlag(MinqDeletion.SingleRecordAdminOnly));
    public bool CanDeleteCollection => DeletionMode.HasFlag(MinqDeletion.Collection) || (IsAdmin && DeletionMode.HasFlag(MinqDeletion.CollectionAdminOnly));

    private Array LastRecords { get; set; } = Array.Empty<object>();

    public List<string> Columns { get; set; } = [];
    public HashSet<string> HiddenColumns { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public List<string> VisibleColumns => Columns.Where(c => !HiddenColumns.Contains(c)).ToList();
    
    public List<string> PickerColumns
    {
        get
        {
            HashSet<string> allCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (MinqColumnDefinition def in ColumnDefinitions.Values.Where(d => !d.IsIgnored))
                allCols.Add(def.Name);
            
            foreach (string col in Columns)
                allCols.Add(col);

            return allCols.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
    
    public List<Dictionary<string, string>> Rows { get; set; } = [];
    public Dictionary<string, MinqColumnDefinition> ColumnDefinitions { get; set; } = new Dictionary<string, MinqColumnDefinition>(StringComparer.OrdinalIgnoreCase);

    // Serialization DTOs


    #region Settings Callbacks    
    // Force the UI to re-render the style block immediately when the dropdown changes
    internal async Task OnThemeChangedAsync()
    {
        await SavePreferencesAsync();
        StateHasChanged(); 
    }
    internal async Task SavePreferencesAsync()
    {
        if (IsViewingSharedState) return;

        try 
        {
            GlobalSettingsPayload g = new() 
            {
                PageSize = PageSize,
                RefreshInterval = RefreshInterval,
                TableFontSize = TableFontSize,
                TimestampFormat = TimestampFormat,
                FlattenJsonProperties = FlattenJsonProperties,
                HideDefaultValues = HideDefaultValues,
                ThemeName = SelectedThemeName
            };

            LocalSettingsPayload l = new() 
            {
                PinnedColumnWidth = PinnedColumnWidth,
                MaxColumnWidth = MaxColumnWidth,
                RowClickBehavior = RowClickBehavior,
                HiddenColumns = HiddenColumns.ToList()
            };

            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "MinqViewer_Global", g.ToJson());
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"MinqViewer_Local_{Contract.Name}", l.ToJson());
        } 
        catch { }
    }
    
    internal async Task SaveSharedAsDefault()
    {
        IsViewingSharedState = false;
        await SavePreferencesAsync();
        NavManager.NavigateTo(NavManager.GetUriWithQueryParameter("view", (string)null), replace: true);
        Log.Info("Shared view saved as your personal default.");
    }
    
    #endregion Settings Callbacks



    protected override void OnInitialized()
    {
        if (HttpContext != null)
            Log.Warn("MinqViewer is executing in a server request context (Static SSR or Prerendering). If it remains unresponsive after loading, verify the parent page has an interactive render mode applied.");
            
        ElapsedTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = RunElapsedTimerAsync();
        
        try
        {
            Uri uri = new(NavManager.Uri);
            if (uri.Query.Contains("view="))
            {
                string base64 = uri.Query.Split("view=")[1].Split('&')[0];
                base64 = Uri.UnescapeDataString(base64);
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                SharedViewPayload payload = JsonSerializer.Deserialize<SharedViewPayload>(json);

                if (payload != null)
                {
                    ApplySettings(payload.Global, payload.Local);
                    IsViewingSharedState = true;
                }
            }
        }
        catch { } 

        LoadData();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || IsViewingSharedState)
            return;
        try 
        {
            FlexJson globalJson = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "MinqViewer_Global");
            FlexJson localJson = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"MinqViewer_Local_{Contract.Name}");
            
            GlobalSettingsPayload gSet = globalJson?.ToModel<GlobalSettingsPayload>();
            LocalSettingsPayload lSet = localJson?.ToModel<LocalSettingsPayload>();
            
            if (gSet != null || lSet != null) 
            {
                ApplySettings(gSet, lSet);
                StateHasChanged();
                LoadData(); 
            }
        } 
        catch { }
    }

    private void ApplySettings(GlobalSettingsPayload g, LocalSettingsPayload l)
    {
        if (g != null) 
        {
            PageSize = g.PageSize;
            RefreshInterval = g.RefreshInterval;
            TableFontSize = g.TableFontSize;
            TimestampFormat = g.TimestampFormat;
            FlattenJsonProperties = g.FlattenJsonProperties;
            HideDefaultValues = g.HideDefaultValues;
            
            if (!string.IsNullOrWhiteSpace(g.ThemeName))
                SelectedThemeName = g.ThemeName;
        }

        if (l != null) 
        {
            PinnedColumnWidth = l.PinnedColumnWidth;
            MaxColumnWidth = l.MaxColumnWidth;
            RowClickBehavior = l.RowClickBehavior;
            HiddenColumns = new HashSet<string>(l.HiddenColumns ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        }

        if (RowClickBehavior == RowClickBehaviorOption.DeleteRecord && !CanDeleteSingle) 
        {
            Log.Warn("User settings requested DeleteRecord behavior, but user lacks permission. Reverting to EditRecord.");
            RowClickBehavior = RowClickBehaviorOption.EditRecord;
        }

        if (RefreshTimerComponent != null)
            RefreshTimerComponent.Interval = RefreshInterval;
    }
    
    private async Task CopyShareLink()
    {
        try 
        {
            SharedViewPayload payload = new() 
            {
                Global = new GlobalSettingsPayload 
                {
                    PageSize = PageSize, 
                    RefreshInterval = RefreshInterval, 
                    TableFontSize = TableFontSize,
                    TimestampFormat = TimestampFormat, 
                    FlattenJsonProperties = FlattenJsonProperties, 
                    HideDefaultValues = HideDefaultValues, 
                    ThemeName = SelectedThemeName
                },
                Local = new LocalSettingsPayload 
                {
                    PinnedColumnWidth = PinnedColumnWidth,
                    MaxColumnWidth = MaxColumnWidth,
                    RowClickBehavior = RowClickBehavior, 
                    HiddenColumns = HiddenColumns.ToList()
                }
            };

            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload.ToJson()));
            string url = NavManager.GetUriWithQueryParameter("view", base64);
            await JSRuntime.CopyToClipboard(url);
            
            Log.Info("Share link copied to clipboard!");
        } 
        catch (Exception ex) 
        {
            Log.Error($"Failed to copy share link: {ex.Message}");
        }
    }





    private async Task RunElapsedTimerAsync()
    {
        try
        {
            while (await ElapsedTimer.WaitForNextTickAsync(ElapsedCts.Token))
                if (TimestampFormat == TimestampFormatOption.Elapsed)
                    OnSecondTicked?.Invoke();
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        ElapsedCts.Cancel();
        ElapsedCts.Dispose();
        ElapsedTimer?.Dispose();
    }

    private void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;
    
    internal void OpenColumnPicker() => IsColumnPickerOpen = true;
    private void CloseColumnPicker() => IsColumnPickerOpen = false;

    private void ApplyColumns(HashSet<string> newHiddenColumns)
    {
        HiddenColumns = newHiddenColumns;
        IsColumnPickerOpen = false;
        _ = SavePreferencesAsync();
    }

    public void HandleRowClick(int rowIndex)
    {
        if (RowClickBehavior == RowClickBehaviorOption.SelectText) 
            return;
            
        SelectedRecord = LastRecords.GetValue(rowIndex);
        
        if (RowClickBehavior == RowClickBehaviorOption.EditRecord)
            IsEditorOpen = true;
        else if (RowClickBehavior == RowClickBehaviorOption.DeleteRecord && CanDeleteSingle)
            IsDeleteModalOpen = true;
    }

    public void OpenDeleteAllModal() => IsDeleteAllModalOpen = true;

    private void CloseModals()
    {
        IsEditorOpen = false;
        IsDeleteModalOpen = false;
        IsDeleteAllModalOpen = false;
        SelectedRecord = null;
    }

    private void SaveRecord(object updatedModel)
    {
        try
        {
            object service = ServiceProvider.GetRequiredService(Contract);
            MethodInfo updateMethod = Contract.GetMethod("Update");
            
            if (updateMethod != null)
            {
                updateMethod.Invoke(service, new[] { updatedModel });
                Log.Info($"Successfully updated record via {Contract.Name}.Update()");
            }
            else
            {
                Log.Error($"Update method not found on contract {Contract.Name}.");
            }

            CloseModals();
            LoadData(); 
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to update record.", ex);
        }
    }

    private void DeleteRecord(object modelToDelete)
    {
        try
        {
            object service = ServiceProvider.GetRequiredService(Contract);
            MethodInfo deleteMethod = Contract.GetMethod("Delete");
            
            if (deleteMethod != null)
            {
                deleteMethod.Invoke(service, new[] { modelToDelete });
                Log.Info($"Successfully deleted record via {Contract.Name}.Delete()");
            }
            else
            {
                Log.Error($"Delete method not found on contract {Contract.Name}.");
            }

            CloseModals();
            LoadData(); 
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete record.", ex);
        }
    }

    private void DeleteAllRecords()
    {
        try
        {
            object service = ServiceProvider.GetRequiredService(Contract);
            MethodInfo deleteAllMethod = Contract.GetMethod("DeleteAll"); 
            
            if (deleteAllMethod != null)
            {
                deleteAllMethod.Invoke(service, null);
                Log.Info($"Successfully deleted all records via {Contract.Name}.DeleteAll()");
            }
            else
            {
                Log.Error($"DeleteAll method not found on contract {Contract.Name}.");
            }

            CloseModals();
            PageNumber = 0;
            LoadData(); 
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete all records: {ex.Message}");
        }
    }

    public void OnPageSizeChanged()
    {
        PageNumber = 0;
        _ = SavePreferencesAsync();
        LoadData();
    }
    
    public void OnRefreshIntervalChanged()
    {
        if (RefreshTimerComponent != null)
            RefreshTimerComponent.Interval = RefreshInterval;
        _ = SavePreferencesAsync();
    }

    public void OnViewDataChanged()
    {
        if (LastRecords.Length > 0)
            ParseData(LastRecords);
        _ = SavePreferencesAsync();
    }

    private void PreviousPage()
    {
        if (PageNumber <= 0)
            return;
        PageNumber--;
        LoadData();
    }

    private void NextPage()
    {
        if (PageNumber + 1 >= TotalPages)
            return;
        PageNumber++;
        LoadData();
    }

    private string GenerateSignatureString(MethodInfo method)
    {
        string returnType = method.ReturnType.GetFriendlyName();
        ParameterInfo[] parameters = method.GetParameters();
        List<string> paramStrings = [];

        foreach (ParameterInfo p in parameters)
        {
            string modifier = p.IsOut ? "out " : (p.ParameterType.IsByRef ? "ref " : "");
            string typeName = p.ParameterType.GetFriendlyName();
            paramStrings.Add($"{modifier}{typeName} {p.Name}");
        }

        return $"{returnType} {method.Name}({string.Join(", ", paramStrings)})";
    }

    public void LoadData()
    {
        IsLoading = true;
        HasError = false;
        
        RefreshTimerComponent?.ResetTimer();

        try
        {
            object service = ServiceProvider.GetRequiredService(Contract);
            MethodInfo defaultMethod = Contract.GetMethod("PageAllRecords");

            if (defaultMethod == null)
            {
                HasError = true;
                ErrorMessage = $"Method {"PageAllRecords"} not found on the provided contract.";
                IsLoading = false;
                return;
            }

            MethodInfo targetMethod = defaultMethod;

            if (!string.IsNullOrWhiteSpace(CustomQuery))
            {
                MethodInfo customMethod = Contract.GetMethod(CustomQuery);
                
                if (customMethod == null)
                    Log.Error($"Custom query '{CustomQuery}' not found on contract '{Contract.Name}'. Falling back to PageAllRecords.");
                else if (!defaultMethod.SignatureMatches(customMethod))
                {
                    string expectedSignature = GenerateSignatureString(defaultMethod);
                    string actualSignature = GenerateSignatureString(customMethod);
                    Log.Error($"Custom Query does not match the required parameter signature.  Falling back to {Contract.Name}.{defaultMethod.Name}().", data: new
                    {
                        ExpectedSignature = expectedSignature,
                        ActualSignature = actualSignature,
                        Help = "The method chosen must support paging.  If you do not want paging, simply use Limit() and Sort() to tailor your data, but the method parameter signature must match."
                    });
                }
                else
                    targetMethod = customMethod;
            }

            if (PageSize <= 0)
            {
                Log.Warn($"{nameof(PageSize)} was 0 or less.  This should never happen; a bug is present.  Defaulting to 1.", new
                {
                    Help = "This is likely the result of bad JSON deserialization.  Try deleting local storage and refreshing the page."
                });
                PageSize = 1;
            }
            
            object[] parameters = [PageSize, PageNumber, 0L];
            object result = targetMethod.Invoke(service, parameters);

            if (result is Array records)
            {
                LastRecords = records;
                long remaining = (long)parameters[2];
                TotalRecords = (PageNumber * PageSize) + records.Length + remaining;
                TotalPages = TotalRecords == 0 ? 1 : (int)Math.Ceiling((double)TotalRecords / PageSize);

                ParseData(records);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        
        IsLoading = false;
    }

    private void BuildColumnDefinitions(Type type, string prefix, List<int> currentOrderPath, HashSet<Type> visitedTypes, PropertyInfo[] currentPropertyPath)
    {
        if (!visitedTypes.Add(type))
            return;

        PropertyInfo[] properties = type.GetProperties();

        foreach (PropertyInfo prop in properties)
        {
            PropertyInfo declaredProp = prop;
            if (prop.DeclaringType != null)
                declaredProp = prop.DeclaringType.GetProperty(prop.Name) ?? prop;

            FlexKeys flexAttr = declaredProp.GetCustomAttribute<FlexKeys>();
            FlexIgnore flexIgnoreAttr = declaredProp.GetCustomAttribute<FlexIgnore>();
            MinqViewAttribute viewAttr = declaredProp.GetCustomAttribute<MinqViewAttribute>();

            string jsonKey = prop.Name;
            
            if (flexAttr != null && !string.IsNullOrWhiteSpace(flexAttr.Json))
                jsonKey = flexAttr.Json;
            else if (prop.Name == "Id")
                jsonKey = "_id"; 

            string fullPath = string.IsNullOrEmpty(prefix) ? jsonKey : $"{prefix}.{jsonKey}";

            bool isIgnored = false;
            bool isBsonIgnored = false;
            bool isJsonIgnored = false;

            if (flexAttr != null)
            {
                if (flexAttr.Ignore.HasFlag(Ignore.InBson) || flexAttr.Ignore == Ignore.Always) isBsonIgnored = true;
                if (flexAttr.Ignore.HasFlag(Ignore.InJson) || flexAttr.Ignore == Ignore.Always) isJsonIgnored = true;
            }
            if (flexIgnoreAttr != null)
            {
                if (flexIgnoreAttr.Ignore.HasFlag(Ignore.InBson) || flexIgnoreAttr.Ignore == Ignore.Always) isBsonIgnored = true;
                if (flexIgnoreAttr.Ignore.HasFlag(Ignore.InJson) || flexIgnoreAttr.Ignore == Ignore.Always) isJsonIgnored = true;
            }

            isIgnored = isBsonIgnored; 

            bool isSticky = viewAttr != null && viewAttr.Sticky;
            bool isReadOnly = viewAttr != null && viewAttr.ReadOnly;
            int order = viewAttr != null ? viewAttr.Order : int.MaxValue;
            bool isTimestamp = prop.PropertyType == typeof(long) || prop.PropertyType == typeof(long?);
            bool isBool = prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?);

            List<int> orderPath = [..currentOrderPath, order];
            PropertyInfo[] newPath = [..currentPropertyPath, declaredProp];

            MinqColumnDefinition definition = new()
            {
                Name = fullPath,
                PropertyName = prop.Name,
                BsonName = flexAttr?.Bson,
                JsonName = flexAttr?.Json,
                PropertyPath = newPath,
                PropertyType = prop.PropertyType,
                IsIgnored = isIgnored,
                IsJsonIgnored = isJsonIgnored,
                IsBsonIgnored = isBsonIgnored,
                IsSticky = isSticky,
                ReadOnly = isReadOnly,
                OrderPath = orderPath,
                IsTimestamp = isTimestamp,
                IsBool = isBool,
                IsNested = !string.IsNullOrEmpty(prefix),
                IsComplex = prop.PropertyType.IsComplex()
            };

            ColumnDefinitions[fullPath] = definition;

            if (prop.Name == "Id" && (flexAttr == null || string.IsNullOrWhiteSpace(flexAttr.Json)))
            {
                string idPath1 = string.IsNullOrEmpty(prefix) ? "_id" : $"{prefix}._id";
                string idPath2 = string.IsNullOrEmpty(prefix) ? "id" : $"{prefix}.id";
                string idPath3 = string.IsNullOrEmpty(prefix) ? "Id" : $"{prefix}.Id";
                ColumnDefinitions[idPath1] = definition;
                ColumnDefinitions[idPath2] = definition;
                ColumnDefinitions[idPath3] = definition;
            }

            if (definition.IsComplex && !isIgnored)
                BuildColumnDefinitions(prop.PropertyType, fullPath, orderPath, new HashSet<Type>(visitedTypes), newPath);
        }
    }

    private void ParseData(Array records)
    {
        Columns.Clear();
        Rows.Clear();

        if (records.Length == 0)
            return;

        Type modelType = records.GetType().GetElementType() ?? records.GetValue(0).GetType();
        ColumnDefinitions.Clear();
        BuildColumnDefinitions(modelType, string.Empty, [], [], []);

        foreach (MinqColumnDefinition def in ColumnDefinitions.Values.Where(d => !d.IsIgnored))
            if (FlattenJsonProperties)
            {
                if (!def.IsComplex && !Columns.Contains(def.Name))
                    Columns.Add(def.Name);
            }
            else if (!def.IsNested && !Columns.Contains(def.Name))
                Columns.Add(def.Name);

        foreach (object record in records)
        {
            MethodInfo toJsonMethod = record.GetType().GetMethod(nameof(FlexModel.ToJson));

            if (toJsonMethod == null)
            {
                Log.Error($"Could not find method {nameof(FlexModel.ToJson)} on record.  Is it not a {nameof(FlexModel)}?");
                return;
            }
        
            string json = (string)toJsonMethod.Invoke(record, null) ?? "{}";
            JsonDocument document = JsonDocument.Parse(json);
            Dictionary<string, string> rowData = [];

            if (FlattenJsonProperties)
                FlattenJsonElement(document.RootElement, string.Empty, rowData);
            else
                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    string propName = property.Name;
                    if (ColumnDefinitions.TryGetValue(propName, out MinqColumnDefinition def))
                    {
                        if (def.IsIgnored) continue;
                        propName = def.Name; 
                    }

                    if (!Columns.Contains(propName))
                        Columns.Add(propName);
                    else if (HideDefaultValues && property.Value.IsDefault())
                        continue;

                    rowData[propName] = property.Value.ToString();
                }

            Rows.Add(rowData);
        }

        
        Columns.Sort(new ColumnComparer(ColumnDefinitions));
    }

    private void FlattenJsonElement(JsonElement element, string prefix, Dictionary<string, string> rowData)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string rawPropName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
            string propName = rawPropName;
            
            if (ColumnDefinitions.TryGetValue(rawPropName, out MinqColumnDefinition def))
            {
                if (def.IsIgnored) 
                    continue;
                propName = def.Name;
            }

            if (HideDefaultValues && property.Value.IsDefault())
                continue;

            if (property.Value.ValueKind == JsonValueKind.Object)
                FlattenJsonElement(property.Value, rawPropName, rowData);
            else
            {
                if (!Columns.Contains(propName))
                    Columns.Add(propName);

                rowData[propName] = property.Value.ToString();
            }
        }
    }
}