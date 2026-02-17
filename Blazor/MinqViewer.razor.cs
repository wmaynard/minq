using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using Maynard.Logging;
using Maynard.Minq.Attributes;
using Maynard.Minq.Blazor.Components;
using Maynard.Minq.Blazor.Enums;
using Maynard.Minq.Blazor.Helpers;
using Maynard.Minq.Blazor.Models;
using Maynard.Minq.Blazor.Themes;
using Maynard.Minq.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Maynard.Minq.Blazor;

public partial class MinqViewer
{
    #region Parameters
    [CascadingParameter]
    public MinqDashboard TabGroup { get; set; }

    [Parameter]
    public string TabTitle { get; set; }

    // Helper to get a clean title if the user doesn't provide one
    public string DisplayTitle => !string.IsNullOrWhiteSpace(TabTitle) ? TabTitle : (Contract?.Name ?? "Viewer");
    
    [CascadingParameter]
    public HttpContext HttpContext { get; set; }
    
    [Parameter]
    public Type Contract { get; set; }

    [Parameter]
    public string CustomQuery { get; set; }

    [Parameter]
    public MinqViewerDeletionMode MinqViewerDeletionMode { get; set; } = MinqViewerDeletionMode.None;

    [Parameter]
    public bool IsAdmin { get; set; }
    [Parameter]
    public bool IsReadOnly { get; set; }
    #endregion Parameters
    
    public static readonly IReadOnlyList<ThemeProvider> AvailableThemes = ThemeManager.GetAvailableThemes();
    
    // The consolidated State object for the viewer settings
    public MinqViewerState State { get; set; } = new();

    private string ThemeVariables => AvailableThemes
        .FirstOrDefault(t => t.Name == State.SelectedThemeName)?.ToString() 
        ?? new LightThemeProvider().ToString();

    public event Action OnSecondTicked;

    public MinqInputRefreshTimer RefreshTimerComponent { get; set; }
    
    public PeriodicTimer ElapsedTimer { get; set; }
    public CancellationTokenSource ElapsedCts { get; set; } = new();

    public bool IsViewingSharedState { get; set; }

    public int PageNumber { get; set; }
    public long TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public bool IsSettingsOpen { get; set; }
    public bool IsColumnPickerOpen { get; set; }
    public bool IsEditorOpen { get; set; }
    public bool IsDeleteModalOpen { get; set; }
    public bool IsDeleteAllModalOpen { get; set; }
    public object SelectedRecord { get; set; }

    public bool CanDeleteSingle => !IsReadOnly && (MinqViewerDeletionMode.HasFlag(MinqViewerDeletionMode.SingleRecord) || (IsAdmin && MinqViewerDeletionMode.HasFlag(MinqViewerDeletionMode.SingleRecordAdminOnly)));
    public bool CanDeleteCollection => !IsReadOnly && (MinqViewerDeletionMode.HasFlag(MinqViewerDeletionMode.Collection) || (IsAdmin && MinqViewerDeletionMode.HasFlag(MinqViewerDeletionMode.CollectionAdminOnly)));

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

    #region Settings Callbacks    
    internal async Task OnThemeChangedAsync()
    {
        await SavePreferencesAsync();
        TabGroup?.NotifyThemeChanged(State.SelectedThemeName);
        StateHasChanged(); 
    }
    
    internal async Task SavePreferencesAsync()
    {
        if (IsViewingSharedState)
            return;

        try
        {
            GlobalSettingsPayload g = new()
            {
                PageSize = State.PageSize,
                RefreshInterval = State.RefreshInterval,
                TableFontSize = State.TableFontSize,
                MinqViewerTimestampFormat = State.MinqViewerTimestampFormat,
                FlattenJsonProperties = State.FlattenJsonProperties,
                HideDefaultValues = State.HideDefaultValues,
                ThemeName = State.SelectedThemeName
            };

            LocalSettingsPayload l = new()
            {
                PinnedColumnWidth = State.PinnedColumnWidth,
                MaxColumnWidth = State.MaxColumnWidth,
                RowClickBehavior = State.RowClickBehavior,
                HiddenColumns = HiddenColumns.ToList()
            };

            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "MinqViewer_Global", g.ToJson());
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"MinqViewer_Local_{Contract.Name}", l.ToJson());
        }
        catch (Exception ex)
        {
            Log.Error("Unable to save preferences.", ex);
        }
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
        TabGroup?.AddViewer(this);
        
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
                SharedViewPayload payload = FlexModel.FromJSON<SharedViewPayload>(json);

                if (payload != null)
                {
                    ApplySettings(payload.Global, payload.Local);
                    IsViewingSharedState = true;
                }
            }
        }
        catch { } 

        if (IsViewingSharedState)
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
                ApplySettings(gSet, lSet);
        } 
        catch { }
        finally
        {
            LoadData(); 
            StateHasChanged();
        }
    }

    private void ApplySettings(GlobalSettingsPayload global, LocalSettingsPayload local)
    {
        if (global != null) 
        {
            State.PageSize = global.PageSize;
            State.RefreshInterval = global.RefreshInterval;
            State.TableFontSize = global.TableFontSize;
            State.MinqViewerTimestampFormat = global.MinqViewerTimestampFormat;
            State.FlattenJsonProperties = global.FlattenJsonProperties;
            State.HideDefaultValues = global.HideDefaultValues;
            
            if (!string.IsNullOrWhiteSpace(global.ThemeName))
                State.SelectedThemeName = global.ThemeName;
        }

        if (local != null) 
        {
            State.PinnedColumnWidth = local.PinnedColumnWidth;
            State.MaxColumnWidth = local.MaxColumnWidth;
            State.RowClickBehavior = local.RowClickBehavior;
            HiddenColumns = new HashSet<string>(local.HiddenColumns ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        }

        if (State.RowClickBehavior == RowClickBehaviorOption.DeleteRecord && !CanDeleteSingle) 
        {
            Log.Warn("User settings requested DeleteRecord behavior, but user lacks permission. Reverting to EditRecord.");
            State.RowClickBehavior = RowClickBehaviorOption.EditRecord;
        }
        
        if (IsReadOnly && State.RowClickBehavior != RowClickBehaviorOption.SelectText)
        {
            Log.Warn("User settings requested click behavior other than SelectText, but viewer is ReadOnly. Reverting to SelectText.");
            State.RowClickBehavior = RowClickBehaviorOption.SelectText;
        }

        if (RefreshTimerComponent != null)
            RefreshTimerComponent.Interval = State.RefreshInterval;
    }
    
    private async Task CopyShareLink()
    {
        try 
        {
            SharedViewPayload payload = new() 
            {
                Global = new GlobalSettingsPayload 
                {
                    PageSize = State.PageSize, 
                    RefreshInterval = State.RefreshInterval, 
                    TableFontSize = State.TableFontSize,
                    MinqViewerTimestampFormat = State.MinqViewerTimestampFormat, 
                    FlattenJsonProperties = State.FlattenJsonProperties, 
                    HideDefaultValues = State.HideDefaultValues, 
                    ThemeName = State.SelectedThemeName
                },
                Local = new LocalSettingsPayload 
                {
                    PinnedColumnWidth = State.PinnedColumnWidth,
                    MaxColumnWidth = State.MaxColumnWidth,
                    RowClickBehavior = State.RowClickBehavior, 
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
                if (State.MinqViewerTimestampFormat == MinqViewerTimestampFormatOption.Elapsed)
                    OnSecondTicked?.Invoke();
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        ElapsedCts.Cancel();
        ElapsedCts.Dispose();
        ElapsedTimer?.Dispose();
        GC.SuppressFinalize(this);
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

    private void HandleRowClick(int rowIndex)
    {
        if (State.RowClickBehavior == RowClickBehaviorOption.SelectText || IsReadOnly) 
            return;
            
        SelectedRecord = LastRecords.GetValue(rowIndex);
        
        if (State.RowClickBehavior == RowClickBehaviorOption.EditRecord)
            IsEditorOpen = true;
        else if (State.RowClickBehavior == RowClickBehaviorOption.DeleteRecord && CanDeleteSingle)
            IsDeleteModalOpen = true;
    }

    internal void OpenDeleteAllModal() => IsDeleteAllModalOpen = true;

    private void CloseModals()
    {
        IsEditorOpen = false;
        IsDeleteModalOpen = false;
        IsDeleteAllModalOpen = false;
        SelectedRecord = null;
    }

    private void SaveRecord(object updatedModel)
    {
        if (IsReadOnly) 
            return;
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
                Log.Error($"Update method not found on contract {Contract.Name}.");

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
        if (IsReadOnly) 
            return;
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
                Log.Error($"Delete method not found on contract {Contract.Name}.");

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
        if (IsReadOnly) 
            return;
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
                Log.Error($"DeleteAll method not found on contract {Contract.Name}.");

            CloseModals();
            PageNumber = 0;
            LoadData(); 
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete all records: {ex.Message}");
        }
    }

    internal async Task OnPageSizeChanged()
    {
        PageNumber = 0;
        await SavePreferencesAsync();
        LoadData();
    }
    
    internal async Task OnRefreshIntervalChanged()
    {
        if (RefreshTimerComponent != null)
            RefreshTimerComponent.Interval = State.RefreshInterval;
        await SavePreferencesAsync();
    }

    internal async Task OnViewDataChanged()
    {
        if (LastRecords.Length > 0)
            ParseData(LastRecords);
        await SavePreferencesAsync();
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
                    string expectedSignature = defaultMethod.GenerateSignatureString();
                    string actualSignature = defaultMethod.GenerateSignatureString();
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

            if (State.PageSize <= 0)
            {
                Log.Warn($"{nameof(State.PageSize)} was 0 or less.  This should never happen; a bug is present.  Defaulting to 1.", new
                {
                    Help = "This is likely the result of bad JSON deserialization.  Try deleting local storage and refreshing the page."
                });
                State.PageSize = 1;
            }
            
            object[] parameters = [State.PageSize, PageNumber, 0L];
            object result = targetMethod.Invoke(service, parameters);

            if (result is Array records)
            {
                LastRecords = records;
                long remaining = (long)parameters[2];
                TotalRecords = (PageNumber * State.PageSize) + records.Length + remaining;
                TotalPages = TotalRecords == 0 ? 1 : (int)Math.Ceiling((double)TotalRecords / State.PageSize);

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
            PropertyInfo declaredProp = prop?.DeclaringType.GetProperty(prop.Name) ?? prop;
            FlexKeys flexKeys = declaredProp.GetCustomAttribute<FlexKeys>();
            MinqViewAttribute viewAttr = declaredProp.GetCustomAttribute<MinqViewAttribute>();

            string jsonKey = prop.Name switch
            {
                _ when !string.IsNullOrWhiteSpace(flexKeys?.Json) => flexKeys.Json,
                nameof(MinqDocument.Id) => "_id",
                _ => prop.Name
            };

            string fullPath = string.IsNullOrEmpty(prefix) ? jsonKey : $"{prefix}.{jsonKey}";

            Ignore policy = declaredProp.GetCustomAttribute<FlexIgnore>()?.Ignore ?? Ignore.Never
                | declaredProp.GetCustomAttribute<FlexKeys>()?.Ignore ?? Ignore.Never;

            List<int> orderPath = [..currentOrderPath, viewAttr?.Order ?? int.MaxValue];
            PropertyInfo[] newPath = [..currentPropertyPath, declaredProp];

            MinqColumnDefinition definition = new()
            {
                Name = fullPath,
                PropertyName = prop.Name,
                BsonName = flexKeys?.Bson,
                JsonName = flexKeys?.Json,
                PropertyPath = newPath,
                PropertyType = prop.PropertyType,
                IsIgnored = policy.HasFlag(Ignore.InBson),
                IsJsonIgnored = policy.HasFlag(Ignore.InJson),
                IsBsonIgnored = policy.HasFlag(Ignore.InBson),
                IsSticky = viewAttr?.Sticky ?? false,
                ReadOnly = viewAttr?.ReadOnly ?? false,
                OrderPath = orderPath,
                IsTimestamp = prop.PropertyType == typeof(long) || prop.PropertyType == typeof(long?),
                IsBool = prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?),
                IsNested = !string.IsNullOrEmpty(prefix),
                IsComplex = prop.PropertyType.IsComplex()
            };

            ColumnDefinitions[fullPath] = definition;

            if (prop.Name == nameof(MinqDocument.Id) && (flexKeys == null || string.IsNullOrWhiteSpace(flexKeys.Json)))
            {
                string idPath1 = string.IsNullOrEmpty(prefix) ? "_id" : $"{prefix}._id";
                string idPath2 = string.IsNullOrEmpty(prefix) ? "id" : $"{prefix}.id";
                string idPath3 = string.IsNullOrEmpty(prefix) ? "Id" : $"{prefix}.Id";
                ColumnDefinitions[idPath1] = definition;
                ColumnDefinitions[idPath2] = definition;
                ColumnDefinitions[idPath3] = definition;
            }

            if (definition.IsComplex && !policy.HasFlag(Ignore.InBson))
                BuildColumnDefinitions(prop.PropertyType, fullPath, orderPath, new HashSet<Type>(visitedTypes), newPath);
        }
    }

    private void ParseData(Array records)
    {
        Columns.Clear();
        Rows.Clear();

        if (records.Length == 0)
            return;

        Type modelType = records.GetType().GetElementType() ?? records.GetValue(0)!.GetType();
        ColumnDefinitions.Clear();
        BuildColumnDefinitions(modelType, string.Empty, [], [], []);

        foreach (MinqColumnDefinition def in ColumnDefinitions.Values.Where(d => !d.IsIgnored))
            if (State.FlattenJsonProperties)
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
                Log.Error($"Could not find method {nameof(FlexModel.ToJson)} on record. Is it not a {nameof(FlexModel)}?", record);
                return;
            }
            
            FlexJson document = (string)toJsonMethod.Invoke(record, null) ?? "{}";
            Dictionary<string, string> rowData = [];
            
            if (State.FlattenJsonProperties)
                FlattenObject(document, string.Empty, rowData);
            else
                foreach (string key in document.Keys)
                {
                    string toAdd = ColumnDefinitions.TryGetValue(key, out MinqColumnDefinition def)
                        ? def.Name
                        : key;
                    if (def?.IsIgnored ?? false)
                        continue;
                    if (!Columns.Contains(toAdd))
                        Columns.Add(toAdd);
                    else if (State.HideDefaultValues && (document[key]?.IsDefault() ?? true))
                        continue;
                    rowData[toAdd] = document[key]?.ToString() ?? string.Empty;
                }

            Rows.Add(rowData);
        }
        
        Columns.Sort(new MinqViewColumnComparer(ColumnDefinitions));
    }

    private void FlattenObject(FlexJson element, string prefix, Dictionary<string, string> rowData)
    {
        if (element == null) 
            return;

        foreach (string key in element.Keys)
        {
            if (State.HideDefaultValues && (element[key]?.IsDefault() ?? true))
                continue;
            
            string rawPropName = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
            string propName = rawPropName;
            
            if (ColumnDefinitions.TryGetValue(rawPropName, out MinqColumnDefinition def))
            {
                if (def.IsIgnored) 
                    continue;
                propName = def.Name;
            }
            
            if (element[key] is FlexJson nested)
                FlattenObject(nested, rawPropName, rowData);
            else
            {
                if (!Columns.Contains(propName))
                    Columns.Add(propName);
                rowData[propName] = element[key]?.ToString() ?? string.Empty;
            }
        }
    }
}