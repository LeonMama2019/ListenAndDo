using System;
using System.Collections.Generic;
using UnityEngine;

public enum Stage5Place { BookStand = 0, Odougu = 1, Pencase = 2 }

[CreateAssetMenu(fileName = "Stage5Task", menuName = "ListenAndDo/Stage5/Task Data")]
public class Stage5TaskData : ScriptableObject
{
    [Header("オブジェクト：名前・画像・名前の音声だけ")]
    [SerializeField] private List<Stage5ObjectEntry> objects = new();
    [Header("収納先別リスト：所属はここで1回だけ登録")]
    [SerializeField] private List<Stage5PlaceObjects> placeObjects = new();
    [SerializeField, HideInInspector] private bool groupsMigrated;
    [Header("質問形式：音声とObjectsを再生順に追加")]
    [SerializeField] private List<Stage5QuestionFormat> questionFormats = new();
    [Header("接続音声「と」")]
    [SerializeField] private AudioClip conjunctionVoice;
    [Header("出題設定：レベル・収納先・対象・個数")]
    [SerializeField] private List<Stage5LevelSettings> levels = new();

    private class Target
    {
        public Stage5ObjectEntry item;
        public Stage5TargetSetting setting;

    }

    public bool TryValidate(out string error)
    {
        error = null;
        if (objects == null || objects.Count == 0 || questionFormats == null || questionFormats.Count == 0
            || levels == null || levels.Count == 0)
        { error = "オブジェクト・質問形式・出題設定を登録してください。"; return false; }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in objects)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id) || !ids.Add(item.id)
                || item.sprite == null || item.voice == null)
            { error = "オブジェクトの名前・画像・音声を確認してください。名前は重複できません。"; return false; }
        }
        var memberships = new HashSet<string>(StringComparer.Ordinal);
        var placeIds = new HashSet<Stage5Place>();
        if (placeObjects == null || placeObjects.Count == 0)
        { error = "Place Objectsに収納先別の所属を登録してください。"; return false; }
        foreach (var group in placeObjects)
        {
            if (group == null || !Enum.IsDefined(typeof(Stage5Place), group.place) || !placeIds.Add(group.place)
                || group.objectIds == null)
            { error = "Place Objectsの収納先が重複または未設定です。"; return false; }
            foreach (var id in group.objectIds)
                if (!ids.Contains(id ?? "") || !memberships.Add(id))
                { error = "Place ObjectsのIDが未登録か、所属が重複しています。"; return false; }
        }
        var levelIds = new HashSet<int>();
        foreach (var config in levels)
        {
            if (config == null || config.level < 1 || config.level > 8 || !levelIds.Add(config.level)
                || !ValidPlaces(config.Places) || config.questionCount < 1
                || config.minObjectKinds < 1 || config.maxObjectKinds < config.minObjectKinds
                || config.quantityOverrides == null)
            { error = "出題設定のLevel（1〜8）・Places・問題数を確認してください。"; return false; }
            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var target in config.quantityOverrides)
            {
                if (target == null || !ids.Contains(target.objectId ?? "") || target.count < 1
                    || !targets.Add(target.objectId + "#" + target.count)
                    || (target.count > 1 && target.quantityVoice == null))
                { error = "Quantity OverridesのID・個数・数量音声を確認してください（例：2本）。"; return false; }
            }
            if (config.maxObjectKinds > 1 && conjunctionVoice == null)
            { error = "複数種類を出題する場合は接続音声「と」が必要です。"; return false; }
        }
        var formatIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var format in questionFormats)
        {
            if (format == null || string.IsNullOrWhiteSpace(format.id) || !formatIds.Add(format.id)
                || format.level < 0 || format.level > 8 || format.parts == null || format.parts.Count == 0
                || format.objectIds == null)
            { error = "質問形式の名前・レベル・再生順を確認してください。"; return false; }
            foreach (var id in format.objectIds)
                if (!ids.Contains(id ?? "")) { error = format.id + ": 未登録の対象名です。"; return false; }
            bool insertsObjects = false;
            foreach (var part in format.parts)
            {
                if (part == null || !Enum.IsDefined(typeof(Stage5VoicePartKind), part.kind)
                    || (part.kind == Stage5VoicePartKind.Audio && part.voice == null))
                { error = format.id + ": 音声パーツが未設定です。"; return false; }
                if (part.kind == Stage5VoicePartKind.Objects) insertsObjects = true;
            }
            if (!insertsObjects && format.objectIds.Count != 1) { error = format.id + ": Objectsを再生順に入れるか、文脈で決まる対象IDを1つ指定してください。"; return false; }
        }
        return true;
    }

    public bool NeedsGroupMigration => !groupsMigrated;
    public bool MigrateFromScene(UnityEngine.SceneManagement.Scene scene)
    {
        if (groupsMigrated || !scene.IsValid() || !scene.isLoaded) return false;
        var homes = new Dictionary<Sprite, Stage5Place>();
        var ambiguous = new HashSet<Sprite>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var parent in root.GetComponentsInChildren<Transform>(true))
            {
                Stage5Place home;
                if (parent.name == "BookStand") home = Stage5Place.BookStand;
                else if (parent.name == "Pencase") home = Stage5Place.Pencase;
                else if (parent.name == "Odougu") home = Stage5Place.Odougu;
                else continue;
                foreach (Transform child in parent)
                {
                    var image = home == Stage5Place.BookStand
                        ? child.Find("BookFront")?.GetComponent<UnityEngine.UI.Image>()
                        : child.name == "OdouguArea" ? null : child.GetComponent<UnityEngine.UI.Image>();
                    if (image == null || image.sprite == null) continue;
                    if (homes.TryGetValue(image.sprite, out var previous) && previous != home) ambiguous.Add(image.sprite);
                    else homes[image.sprite] = home;
                }
            }
        foreach (var sprite in ambiguous) homes.Remove(sprite);
        return MigratePlaceObjects(homes);
    }

    // Used by the inspector and game before generating questions. Never delete legacy input.
    public bool MigratePlaceObjects(IReadOnlyDictionary<Sprite, Stage5Place> homes)
    {
        if (groupsMigrated) return false;
        if (homes == null || homes.Count == 0) return false;
        if (placeObjects == null) placeObjects = new List<Stage5PlaceObjects>();
        var assigned = new HashSet<string>();
        foreach (var group in placeObjects)
            foreach (var id in group.objectIds) assigned.Add(id);
        foreach (var item in objects)
        {
            if (item == null || item.sprite == null || assigned.Contains(item.id)) continue;
            if (!homes.TryGetValue(item.sprite, out var home)) continue;
            var group = placeObjects.Find(entry => entry.place == home);
            if (group == null) { group = new Stage5PlaceObjects { place = home }; placeObjects.Add(group); }
            group.objectIds.Add(item.id); assigned.Add(item.id);
        }
        foreach (var config in levels)
        {
            if (config == null) continue;
            if (config.quantityOverrides == null) config.quantityOverrides = new List<Stage5TargetSetting>();
            foreach (var target in config.LegacyTargets)
            {
                // Single-item candidates are now implicit; 0 in unfinished legacy entries also uses that default.
                if (target == null || target.count <= 1) continue;
                var existing = config.quantityOverrides.Find(entry => entry.objectId == target.objectId && entry.count == target.count);
                if (existing == null) config.quantityOverrides.Add(new Stage5TargetSetting
                    { objectId = target.objectId, count = target.count, quantityVoice = target.quantityVoice });
                else if (existing.quantityVoice == null) existing.quantityVoice = target.quantityVoice;
                else if (target.quantityVoice != null && target.quantityVoice != existing.quantityVoice)
                    Debug.LogWarning("数量音声に異なる旧設定があります: " + target.objectId + " / " + target.count, this);
            }
        }
        // Unmatched objects stay available in Objects and can be assigned manually.
        groupsMigrated = true;
        return true;
    }

    public bool HasLevel(int level) => levels != null && levels.Exists(entry => entry != null && entry.level == level);

    public List<Stage5Question> GetQuestions(int targetLevel)
        => BuildQuestions(targetLevel, null);

    public List<Stage5Question> GetQuestions(int targetLevel, UnityEngine.SceneManagement.Scene scene)
    {
        var config = levels?.Find(entry => entry != null && entry.level == targetLevel);
        if (config == null) { Debug.LogError("選択レベルの出題設定がありません。", this); return new(); }
        var counts = new Dictionary<Sprite, int>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var parent in root.GetComponentsInChildren<Transform>(true))
            {
                Stage5Place home;
                if (parent.name == "BookStand") home = Stage5Place.BookStand;
                else if (parent.name == "Pencase") home = Stage5Place.Pencase;
                else if (parent.name == "Odougu") home = Stage5Place.Odougu;
                else continue;
                if (!ContainsPlace(config.Places, home)) continue;
                foreach (Transform child in parent)
                {
                    var image = home == Stage5Place.BookStand
                        ? child.Find("BookFront")?.GetComponent<UnityEngine.UI.Image>()
                        : child.name == "OdouguArea" ? null : child.GetComponent<UnityEngine.UI.Image>();
                    if (image == null || image.sprite == null) continue;
                    counts.TryGetValue(image.sprite, out int count);
                    counts[image.sprite] = count + 1;
                }
            }
        return BuildQuestions(targetLevel, counts);
    }

    private bool PlayableFormat(Stage5QuestionFormat format)
    {
        if (format == null || string.IsNullOrWhiteSpace(format.id) || format.level < 0 || format.level > 8
            || format.parts == null || format.parts.Count == 0 || format.objectIds == null) return false;
        bool hasObjects = false;
        foreach (var part in format.parts)
        {
            if (part == null || !Enum.IsDefined(typeof(Stage5VoicePartKind), part.kind)
                || (part.kind == Stage5VoicePartKind.Audio && part.voice == null)) return false;
            if (part.kind == Stage5VoicePartKind.Objects) hasObjects = true;
        }
        // A context-only instruction is unambiguous only with one fixed target.
        return hasObjects || (format.objectIds.Count == 1);
    }
    private static bool InsertsObjects(Stage5QuestionFormat format)
        => format.parts.Exists(part => part.kind == Stage5VoicePartKind.Objects);

    private List<Stage5Question> BuildQuestions(int targetLevel, IReadOnlyDictionary<Sprite, int> counts)
    {
        var result = new List<Stage5Question>();
        var config = levels?.Find(entry => entry != null && entry.level == targetLevel);
        if (config == null || !ValidPlaces(config.Places) || config.questionCount < 1
            || config.minObjectKinds < 1 || config.maxObjectKinds < config.minObjectKinds
            || objects == null || placeObjects == null || questionFormats == null)
        { Debug.LogError("選択レベルの出題設定を確認してください。", this); return result; }
        var formats = new List<Stage5QuestionFormat>();
        var seenFormats = new HashSet<string>();
        foreach (var format in questionFormats)
        {
            if (format == null || format.level > targetLevel) continue;
            if (!PlayableFormat(format))
            { Debug.LogWarning("Stage5: 未完成の質問形式を除外: " + format?.id, this); continue; }
            var pool = Eligible(format, config, counts);
            if (pool.Count < config.minObjectKinds || (!InsertsObjects(format) && config.minObjectKinds != 1)) continue;
            if (seenFormats.Add(format.id)) formats.Add(format);
        }
        if (formats.Count == 0)
        { Debug.LogError("音声と対象が揃った質問形式がありません。", this); return result; }
        // Report unfinished or impossible quantity conditions without changing the authored data.
        if (config.quantityOverrides != null)
            foreach (var setting in config.quantityOverrides)
            {
                var item = setting == null ? null : objects.Find(entry => entry != null && entry.id == setting.objectId);
                bool impossible = counts != null && item != null && item.sprite != null
                    && (!counts.TryGetValue(item.sprite, out var available) || setting.count > available);
                if (setting == null || item == null || setting.count < 1 || impossible
                    || (setting.count > 1 && setting.quantityVoice == null))
                    Debug.LogWarning("Stage5: 未完成または実物の個数を超える数量条件は出題しません。ID=" + setting?.objectId, this);
            }
        for (int n = 0; n < config.questionCount; n++)
        {
            var format = formats[UnityEngine.Random.Range(0, formats.Count)];
            var pool = Eligible(format, config, counts);
            int maximum = Math.Min(config.maxObjectKinds, pool.Count);
            if (conjunctionVoice == null) maximum = Math.Min(maximum, 1);
            if (maximum < config.minObjectKinds)
            { Debug.LogError("複数種類の出題には接続音声が必要です。", this); return new(); }
            int kinds = InsertsObjects(format) ? UnityEngine.Random.Range(config.minObjectKinds, maximum + 1) : 1;
            var selected = new List<Target>();
            var sprites = new List<Sprite>();
            for (int i = 0; i < kinds; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                var variants = pool[index];
                // Implicit context must not silently ask for a random quantity.
                var target = InsertsObjects(format) ? variants[UnityEngine.Random.Range(0, variants.Count)] : variants[0];
                pool.RemoveAt(index); selected.Add(target);
                for (int j = 0; j < target.setting.count; j++) sprites.Add(target.item.sprite);
            }
            var clips = new List<AudioClip>();
            var text = new System.Text.StringBuilder();
            foreach (var part in format.parts)
            {
                if (part.kind == Stage5VoicePartKind.Audio) { clips.Add(part.voice); text.Append(part.text); }
                else
                    for (int i = 0; i < selected.Count; i++)
                    {
                        if (i > 0) { clips.Add(conjunctionVoice); text.Append("と"); }
                        clips.Add(selected[i].item.voice);
                        if (selected[i].setting.count > 1) clips.Add(selected[i].setting.quantityVoice);
                        text.Append(selected[i].item.id).Append("×").Append(selected[i].setting.count);
                    }
            }
            var question = new Stage5Question("Random_Lv" + targetLevel + "_" + (n + 1) + "_" + format.id,
                targetLevel, text.ToString(), null, config.Places, sprites);
            question.RuntimeVoices = clips; result.Add(question);
        }
        return result;
    }

    private static bool ValidPlaces(IReadOnlyList<Stage5Place> places)
    {
        if (places == null || places.Count == 0) return false;
        var seen = new HashSet<Stage5Place>();
        foreach (var place in places)
            if (!Enum.IsDefined(typeof(Stage5Place), place) || !seen.Add(place)) return false;
        return true;
    }
    private static bool ContainsPlace(IReadOnlyList<Stage5Place> places, Stage5Place place)
    {
        foreach (var entry in places) if (entry == place) return true;
        return false;
    }
    private List<List<Target>> Eligible(Stage5QuestionFormat format, Stage5LevelSettings config, IReadOnlyDictionary<Sprite, int> counts)
    {
        var groups = new List<List<Target>>();
        var seen = new HashSet<string>();
        foreach (var membership in placeObjects)
        {
            if (membership == null || membership.objectIds == null || !ContainsPlace(config.Places, membership.place)) continue;
            foreach (var id in membership.objectIds)
            {
                if (!seen.Add(id) || (format.objectIds.Count > 0 && !format.objectIds.Contains(id))) continue;
                var item = objects.Find(entry => entry != null && entry.id == id);
                if (item == null || item.sprite == null || item.voice == null) continue;
                int available = int.MaxValue;
                if (counts != null && (!counts.TryGetValue(item.sprite, out available) || available < 1)) continue;
                var group = groups.Find(entries => entries[0].item.sprite == item.sprite);
                if (group == null) { group = new List<Target>(); groups.Add(group); }
                group.Add(new Target { item = item, setting = new Stage5TargetSetting { objectId = id, count = 1 } });
                if (config.quantityOverrides != null)
                    foreach (var setting in config.quantityOverrides)
                        if (setting != null && setting.objectId == id && setting.count > 1 && setting.count <= available
                            && setting.quantityVoice != null && !group.Exists(target => target.setting.count == setting.count))
                            group.Add(new Target { item = item, setting = setting });
            }
        }
        return groups;
    }
}

[Serializable]
public class Stage5ObjectEntry
{
    [Tooltip("オブジェクト名。出題側からこの名前で参照します")]
    public string id;
    public Sprite sprite;
    [Tooltip("オブジェクト名だけを読む音声")]
    public AudioClip voice;
}

[Serializable]
public class Stage5LevelSettings : ISerializationCallbackReceiver
{
    [Range(1, 8)] public int level = 1;
    [SerializeField, HideInInspector] private Stage5Place place;
    [SerializeField, HideInInspector] private bool placesMigrated;
    [Tooltip("散らかして元に戻す収納先を複数指定できます")]
    public List<Stage5Place> places = new();
    public IReadOnlyList<Stage5Place> Places { get { MigratePlace(); return places; } }
    private void MigratePlace()
    {
        if (placesMigrated) return;
        if (places == null) places = new List<Stage5Place>();
        if (places.Count == 0) places.Add(place);
        placesMigrated = true;
    }
    public void OnBeforeSerialize() => MigratePlace();
    public void OnAfterDeserialize() => MigratePlace();
    [Min(1)] public int questionCount = 7;
    [Min(1)] public int minObjectKinds = 1;
    [Min(1)] public int maxObjectKinds = 1;
    [SerializeField, HideInInspector] private List<Stage5TargetSetting> targets = new();
    public IReadOnlyList<Stage5TargetSetting> LegacyTargets => targets ?? (IReadOnlyList<Stage5TargetSetting>)Array.Empty<Stage5TargetSetting>();
    [Tooltip("通常は空欄で全て単品。鉛筆2本など追加したい数量だけ登録")]
    public List<Stage5TargetSetting> quantityOverrides = new();
}

[Serializable]
public class Stage5TargetSetting
{
    [Tooltip("Objectsに登録したオブジェクト名")]
    public string objectId;
    [Tooltip("机に残す個数。同じ名前で個数違いを登録すると、その個数も抽選します")]
    [Min(1)] public int count = 1;
    [Tooltip("2個以上の場合だけ指定。例：2本。名前の音声の後ろに続けて再生します")]
    public AudioClip quantityVoice;
}

public enum Stage5VoicePartKind { Audio, Objects }

[Serializable]
public class Stage5VoicePart
{
    public Stage5VoicePartKind kind;
    public AudioClip voice;
    public string text;
}

[Serializable]
public class Stage5QuestionFormat
{
    public string id;
    [Tooltip("使用開始レベル。1ならレベル1以降、3ならレベル3以降。0も全レベルで使用できます。")] [Range(0, 8)] public int level;
    [Tooltip("空欄は出題設定の全対象。塗り絵などでは対応する名前だけを指定")]
    public List<string> objectIds = new();
    public List<Stage5VoicePart> parts = new();
}



[Serializable]
public class Stage5PlaceObjects
{
    public Stage5Place place;
    [Tooltip("この収納先に所属するObjectsのID。登録は1回だけ")]
    public List<string> objectIds = new();
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(Stage5TaskData))]
public class Stage5TaskDataInspector : UnityEditor.Editor
{
    private void OnEnable()
    {
        var data = target as Stage5TaskData;
        if (data == null || !data.NeedsGroupMigration) return;
        const string path = "Assets/Scenes/Stage5.unity";
        if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path) == null) return;
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
        try
        {
            UnityEditor.Undo.RecordObject(data, "Migrate Stage5 place memberships");
            if (data.MigrateFromScene(scene)) UnityEditor.EditorUtility.SetDirty(data);
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        UnityEditor.EditorGUILayout.HelpBox("Place Objectsで所属を1回だけ登録。LevelsはPlacesを選ぶだけです。単品は自動で候補になり、数量の追加条件だけQuantity Overridesに入れます。", UnityEditor.MessageType.Info);
    }
}
#endif

