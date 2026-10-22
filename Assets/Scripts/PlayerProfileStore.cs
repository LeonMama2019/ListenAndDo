using System;
using UnityEngine;

// Shared identity used by Top, game recorders and the records screen.
public static class PlayerProfileStore
{
    public const string SaveKey = "ListenAndDo.Players.v1";
    private const string LegacyIdKey = "ListenAndDo.LocalUserId";

    [Serializable] public class Profile
    {
        public string nickname;
        public string age;
        public string gender;
        public string userId;
    }
    [Serializable] public class SaveData
    {
        public Profile[] players = new Profile[2];
        public int selected;
    }

    public static string LegacyUserId
    {
        get
        {
            string id = PlayerPrefs.GetString(LegacyIdKey, "");
            if (!string.IsNullOrEmpty(id)) return id;
            id = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(LegacyIdKey, id);
            PlayerPrefs.Save();
            return id;
        }
    }

    public static SaveData Load()
    {
        SaveData data = null;
        string json = PlayerPrefs.GetString(SaveKey, "");
        try { if (!string.IsNullOrEmpty(json)) data = JsonUtility.FromJson<SaveData>(json); }
        catch (ArgumentException) { Debug.LogWarning("Player profiles could not be read."); }
        data ??= new SaveData();
        if (data.players == null || data.players.Length != 2)
            Array.Resize(ref data.players, 2);
        data.selected = Mathf.Clamp(data.selected, 0, 1);
        bool changed = false;
        for (int i = 0; i < data.players.Length; i++)
        {
            Profile profile = data.players[i];
            if (profile == null || string.IsNullOrWhiteSpace(profile.nickname)) continue;
            if (!string.IsNullOrEmpty(profile.userId)) continue;
            // The first profile inherits records from before multiple players existed.
            profile.userId = i == 0 ? LegacyUserId : Guid.NewGuid().ToString("N");
            changed = true;
        }
        if (changed) Save(data);
        return data;
    }

    public static void Save(SaveData data)
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static Profile Current
    {
        get
        {
            SaveData data = Load();
            Profile profile = data.players[data.selected];
            return profile != null && !string.IsNullOrWhiteSpace(profile.nickname) ? profile : null;
        }
    }
    public static string CurrentUserId => Current?.userId ?? LegacyUserId;
    public static string CurrentName => Current?.nickname ?? "";
}
