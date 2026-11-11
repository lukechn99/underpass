using System;
using System.Collections.Generic;
using Underpass.Core;
using UnityEngine;

namespace Underpass.Prototype
{
    // PlayerPrefs uses platform-local storage, including IndexedDB in Web builds.
    // No Unity or persistence dependency is introduced into the expedition rules.
    public sealed class LocalExpeditionStore
    {
        const string Primary = "underpass.expedition.v1";
        const string Backup = "underpass.expedition.v1.backup";
        string lastSaved;
        bool blocked;
        public string Status { get; private set; } = "Local save ready";

        public Expedition Load(IEnumerable<Room> rooms, string startRoom)
        {
            bool existing = false;
            foreach (string key in new[] { Primary, Backup })
            {
                if (!PlayerPrefs.HasKey(key)) continue;
                existing = true;
                try
                {
                    string json = PlayerPrefs.GetString(key);
                    var state = Expedition.RestoreSave(rooms, startRoom, JsonUtility.FromJson<ExpeditionSave>(json));
                    // Keep the verified backup intact when recovering a broken primary.
                    if (key == Backup) PlayerPrefs.SetString(Primary, json);
                    lastSaved = json;
                    Status = key == Backup ? "Recovered backup save" : "Local adventure restored";
                    return state;
                }
                catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
                {
                    Debug.LogWarning("Underpass: could not restore " + key + ": " + e.Message);
                }
            }
            // Preserve unknown/corrupt data for recovery instead of silently overwriting it.
            blocked = existing;
            if (blocked) Status = "Save unreadable; temporary session (existing save preserved)";
            return null;
        }

        public void Save(Expedition state)
        {
            if (blocked || state == null) return;
            string json = JsonUtility.ToJson(state.CaptureSave());
            if (json == lastSaved) return;
            try
            {
                if (lastSaved != null) PlayerPrefs.SetString(Backup, lastSaved);
                PlayerPrefs.SetString(Primary, json);
                PlayerPrefs.Save();
                lastSaved = json;
                Status = "Progress saved locally";
            }
            catch (PlayerPrefsException)
            {
                Status = "Local save unavailable; keep this session open";
                Debug.LogWarning("Underpass: local storage rejected the save.");
            }
        }
    }
}
