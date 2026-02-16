using System;
using System.Collections.Generic;
using System.Linq;

namespace Maynard.Minq.Blazor.Themes;

public static class ThemeManager
{
    private static List<ThemeProvider> _cachedThemes;

    /// <summary>
    /// Scans the current application domain for any non-abstract classes 
    /// that inherit from ThemeProvider and instantiates them.
    /// </summary>
    public static IReadOnlyList<ThemeProvider> GetAvailableThemes()
    {
        // Cache the themes so we don't pay the reflection cost more than once per application lifecycle
        if (_cachedThemes != null)
            return _cachedThemes;

        var themes = new List<ThemeProvider>();

        // 1. Grab all loaded assemblies in the current AppDomain
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                // If an assembly is missing dependencies, grab whatever types successfully loaded
                types = e.Types.Where(t => t != null).ToArray();
            }
            catch
            {
                // Ignore dynamic assemblies or other read exceptions
                continue;
            }

            // 2. Filter for valid ThemeProviders
            var themeTypes = types.Where(t => 
                t.IsClass && 
                !t.IsAbstract && 
                t.IsSubclassOf(typeof(ThemeProvider)) &&
                t.GetConstructor(Type.EmptyTypes) != null // Must have a parameterless constructor
            );

            // 3. Instantiate and collect
            foreach (var type in themeTypes)
            {
                if (Activator.CreateInstance(type) is ThemeProvider instance)
                {
                    themes.Add(instance);
                }
            }
        }

        // Optional: Sort them so Light and Dark appear at the top, followed by user custom themes alphabetically
        _cachedThemes = themes
            .OrderByDescending(t => t is LightThemeProvider)
            .ThenByDescending(t => t is DarkThemeProvider)
            .ThenBy(t => t.Name)
            .ToList();

        return _cachedThemes;
    }
}