using System;
using RimAI.Framework.Core;
using RimWorld;
using Verse;

namespace RimAI.Framework.LLM.Configuration
{
    /// <summary>
    /// Manages loading and caching of RimAI settings with thread-safe access
    /// </summary>
    public class SettingsManager
    {
        private RimAISettings _cachedSettings;
        private readonly object _lock = new object();

        /// <summary>
        /// Gets the current settings, loading them if necessary
        /// </summary>
        /// <returns>Current RimAI settings</returns>
        public RimAISettings GetSettings()
        {
            lock (_lock)
            {
                if (_cachedSettings == null)
                {
                    LoadSettings();
                }
                return _cachedSettings;
            }
        }

        /// <summary>
        /// Forces a reload of settings from the mod configuration
        /// </summary>
        public void RefreshSettings()
        {
            lock (_lock)
            {
                LoadSettings();
            }
        }

        /// <summary>
        /// Internal method to load settings from ModManager
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                var rimAIMod = LoadedModManager.GetMod<RimAIMod>();
                if (rimAIMod != null)
                {
                    _cachedSettings = rimAIMod.settings;
                    Log.Message("RimAI Framework: Settings loaded successfully.");
                }
                else
                {
                    _cachedSettings = new RimAISettings();
                    Log.Message("RimAI Framework: Using default settings - mod not found.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"RimAI Framework: Could not load settings: {ex.Message}");
                _cachedSettings = new RimAISettings();
            }
        }
    }
}
