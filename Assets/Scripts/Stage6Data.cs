using System;
using System.Collections.Generic;
using UnityEngine;

public enum Stage6Room { Class1, Ground }
public enum Stage6Person { Teacher, Boy, Girl }
public enum Stage6Storage { Pencase, Odougu }
public enum Stage6ScatterMode { One, Both }
public enum Stage6ItemPool { Scattered, Unscattered, All }

[Serializable]
public class Stage6Item
{
    [Tooltip("実物ごとの一意なID。例：Pencil1")]
    public string id;
    public Sprite sprite;
    public AudioClip nameVoice;
    public Stage6Storage home;
}
[Serializable]
public class Stage6PersonVoice
{
    public Stage6Person person;
    [Tooltip("人物名だけ。例：先生／〇〇ちゃん")]
    public AudioClip nameVoice;
}
[Serializable]
public class Stage6RoomVoice
{
    public Stage6Room room;
    [Tooltip("Class1：1組にいる／Ground：校庭にいる")]
    public AudioClip locationVoice;
}
[Serializable]
public class Stage6StorageVoice
{
    public Stage6Storage storage;
    [Tooltip("収納先の名前だけ。例：筆箱／お道具箱")]
    public AudioClip nameVoice;
}
[Serializable]
public class Stage6LevelSettings
{
    [Range(1, 8)] public int level = 1;
    [Min(1)] public int questionCount = 7;
    public Stage6ScatterMode scatterMode;
    public Stage6ItemPool itemPool;
    [Range(1, 3)] public int peoplePerRoom = 1;
    [Range(1, 3)] public int deliveryCount = 1;
    public bool specifyRoom = true;

    public static Stage6LevelSettings Default(int level) => new()
    {
        level = level,
        scatterMode = level <= 2 ? Stage6ScatterMode.One : Stage6ScatterMode.Both,
        itemPool = level == 1 ? Stage6ItemPool.Scattered : level == 2 ? Stage6ItemPool.Unscattered : Stage6ItemPool.All,
        peoplePerRoom = level <= 3 ? 1 : 3,
        deliveryCount = level == 8 ? 3 : level >= 6 ? 2 : 1,
        specifyRoom = level != 5 && level != 7 && level != 8
    };
}
[Serializable]
public class Stage6PersonPlacement
{
    public Stage6Room room;
    public Stage6Person person;
}
[Serializable]
public class Stage6Delivery
{
    public string itemId;
    public Stage6Room room;
    public Stage6Person person;
}

// Generated only at runtime: these are not authored question assets.
public class Stage6Question
{
    public string taskId;
    public int level;
    public bool specifyRoom;
    public List<Stage6Storage> scatterPlaces = new();
    public List<Stage6PersonPlacement> people = new();
    public List<Stage6Delivery> deliveries = new();
    public List<AudioClip> instructionVoices = new();
}

[CreateAssetMenu(fileName = "Stage6Data", menuName = "ListenAndDo/Stage6/Random Task Data")]
public class Stage6Data : ScriptableObject
{
    [Header("アイテム：ID・画像・名前の音声・収納先")]
    [SerializeField] private List<Stage6Item> items = new();
    [Header("人物：先生・男の子・女の子の名前音声")]
    [SerializeField] private List<Stage6PersonVoice> personVoices = new();
    [Header("場所：1組にいる／校庭にいる")]
    [SerializeField] private List<Stage6RoomVoice> roomVoices = new();
    [Header("収納先：筆箱／お道具箱の名前音声")]
    [SerializeField] private List<Stage6StorageVoice> storageVoices = new();
    [Header("接続音声")]
    [Tooltip("を片付けたら")]
    [SerializeField] private AudioClip tidyAfterVoice;
    [Tooltip("に")]
    [SerializeField] private AudioClip recipientParticleVoice;
    [Tooltip("を渡してきて")]
    [SerializeField] private AudioClip giveVoice;
    [Tooltip("と：収納先や複数のお使いをつなぐ")]
    [SerializeField] private AudioClip conjunctionVoice;
    [Header("レベル設定：問題はこのルールからランダム生成")]
    [SerializeField] private List<Stage6LevelSettings> levels = new()
    {
        Stage6LevelSettings.Default(1), Stage6LevelSettings.Default(2),
        Stage6LevelSettings.Default(3), Stage6LevelSettings.Default(4),
        Stage6LevelSettings.Default(5), Stage6LevelSettings.Default(6),
        Stage6LevelSettings.Default(7), Stage6LevelSettings.Default(8)
    };
    // Preserve any manually authored questions from the first version; never use them for generation.
    [SerializeField, HideInInspector] private List<Stage6LegacyQuestion> questions = new();
    public IReadOnlyList<Stage6Item> Items => items;
    public IReadOnlyList<Stage6LevelSettings> Levels => levels;
    public Stage6Item FindItem(string id) => items?.Find(x => x != null && x.id == id);
    public bool HasLevel(int level) => levels != null && levels.Exists(x => x != null && x.level == level);

    public bool TryValidate(out string error)
    {
        error = null;
        if (items == null || items.Count == 0 || levels == null || levels.Count == 0)
            return Fail("ItemsとLevelsを登録してください。", out error);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
            if (item == null || string.IsNullOrWhiteSpace(item.id) || !ids.Add(item.id)
                || item.sprite == null || item.nameVoice == null || !Valid(item.home))
                return Fail("ItemsのID・画像・名前音声・収納先を確認してください。", out error);
        if (personVoices == null || roomVoices == null || storageVoices == null)
            return Fail("人物・場所・収納先の音声を登録してください。", out error);
        var persons = new HashSet<Stage6Person>();
        foreach (var p in personVoices)
            if (p == null || !Valid(p.person) || !persons.Add(p.person) || p.nameVoice == null)
                return Fail("人物の重複・名前音声を確認してください。", out error);
        var rooms = new HashSet<Stage6Room>();
        foreach (var r in roomVoices)
            if (r == null || !Valid(r.room) || !rooms.Add(r.room) || r.locationVoice == null)
                return Fail("場所の重複・場所音声を確認してください。", out error);
        var storages = new HashSet<Stage6Storage>();
        foreach (var s in storageVoices)
            if (s == null || !Valid(s.storage) || !storages.Add(s.storage) || s.nameVoice == null)
                return Fail("収納先の重複・名前音声を確認してください。", out error);
        if (persons.Count != 3 || rooms.Count != 2 || storages.Count != 2)
            return Fail("人物3種類・場所2種類・収納先2種類を登録してください。", out error);
        if (tidyAfterVoice == null || recipientParticleVoice == null || giveVoice == null || conjunctionVoice == null)
            return Fail("接続音声：を片付けたら／に／を渡してきて／と を登録してください。", out error);
        var levelIds = new HashSet<int>();
        foreach (var config in levels)
        {
            if (config == null || config.level < 1 || config.level > 8 || !levelIds.Add(config.level)
                || config.questionCount < 1 || config.peoplePerRoom < 1 || config.peoplePerRoom > 3
                || config.deliveryCount < 1 || config.deliveryCount > 3
                || !Valid(config.scatterMode) || !Valid(config.itemPool))
                return Fail("Levelsのレベル重複・個数・抽選条件を確認してください。", out error);
            if (config.deliveryCount > (config.specifyRoom ? config.peoplePerRoom * 2 : Math.Min(3, config.peoplePerRoom * 2)))
                return Fail("届け先の人数が配置人数を超えています。", out error);
            if (config.scatterMode == Stage6ScatterMode.Both && config.itemPool == Stage6ItemPool.Unscattered)
                return Fail("両方を散らかす場合、散らかっていない側は選べません。", out error);
            foreach (Stage6Storage home in Enum.GetValues(typeof(Stage6Storage)))
            {
                if (!items.Exists(x => x.home == home))
                    return Fail("両方の収納先にアイテムを登録してください。", out error);
                int eligible = items.FindAll(x => Eligible(x, config, home)).Count;
                if (eligible < config.deliveryCount)
                    return Fail($"Lv{config.level}: お使い候補のアイテム数が足りません。", out error);
            }
        }
        return true;
    }

    public List<Stage6Question> GetQuestions(int level)
    {
        if (!TryValidate(out var error)) { Debug.LogError(error, this); return new(); }
        var config = levels.Find(x => x.level == level);
        if (config == null) { Debug.LogError("Stage6: 選択レベルがありません。", this); return new(); }
        var result = new List<Stage6Question>();
        for (int n = 0; n < config.questionCount; n++) result.Add(Generate(config, n + 1));
        return result;
    }
    private Stage6Question Generate(Stage6LevelSettings config, int index)
    {
        var q = new Stage6Question { taskId = $"Stage6_Lv{config.level}_Q{index}", level = config.level, specifyRoom = config.specifyRoom };
        var first = (Stage6Storage)UnityEngine.Random.Range(0, 2);
        q.scatterPlaces.Add(first);
        if (config.scatterMode == Stage6ScatterMode.Both)
            q.scatterPlaces.Add(first == Stage6Storage.Pencase ? Stage6Storage.Odougu : Stage6Storage.Pencase);
        if (config.specifyRoom)
        {
            foreach (Stage6Room room in Enum.GetValues(typeof(Stage6Room)))
            {
                var pool = new List<Stage6Person> { Stage6Person.Teacher, Stage6Person.Boy, Stage6Person.Girl };
                for (int i = 0; i < config.peoplePerRoom; i++)
                    q.people.Add(new Stage6PersonPlacement { room = room, person = Take(pool) });
            }
        }
        else
        {
            // Unique names across both rooms make the location-free instruction unambiguous.
            var pool = new List<Stage6Person> { Stage6Person.Teacher, Stage6Person.Boy, Stage6Person.Girl };
            int total = Math.Min(3, config.peoplePerRoom * 2);
            var start = (Stage6Room)UnityEngine.Random.Range(0, 2);
            for (int i = 0; i < total; i++)
                q.people.Add(new Stage6PersonPlacement { room = i % 2 == 0 ? start : start == Stage6Room.Class1 ? Stage6Room.Ground : Stage6Room.Class1, person = Take(pool) });
        }
        var recipients = new List<Stage6PersonPlacement>(q.people);
        var candidates = items.FindAll(x => Eligible(x, config, first));
        for (int i = 0; i < config.deliveryCount; i++)
        {
            var item = Take(candidates);
            var recipient = Take(recipients);
            q.deliveries.Add(new Stage6Delivery { itemId = item.id, room = recipient.room, person = recipient.person });
        }
        for (int i = 0; i < q.scatterPlaces.Count; i++)
        {
            if (i > 0) q.instructionVoices.Add(conjunctionVoice);
            q.instructionVoices.Add(storageVoices.Find(x => x.storage == q.scatterPlaces[i]).nameVoice);
        }
        q.instructionVoices.Add(tidyAfterVoice);
        // 「先生に消しゴムと、〇〇ちゃんに鉛筆を渡してきて」
        for (int i = 0; i < q.deliveries.Count; i++)
        {
            var d = q.deliveries[i];
            if (i > 0) q.instructionVoices.Add(conjunctionVoice);
            if (config.specifyRoom) q.instructionVoices.Add(roomVoices.Find(x => x.room == d.room).locationVoice);
            q.instructionVoices.Add(personVoices.Find(x => x.person == d.person).nameVoice);
            q.instructionVoices.Add(recipientParticleVoice);
            q.instructionVoices.Add(FindItem(d.itemId).nameVoice);
        }
        q.instructionVoices.Add(giveVoice);
        return q;
    }
    private static bool Eligible(Stage6Item item, Stage6LevelSettings config, Stage6Storage scattered)
        => config.itemPool == Stage6ItemPool.All || config.scatterMode == Stage6ScatterMode.Both
            || (config.itemPool == Stage6ItemPool.Scattered ? item.home == scattered : item.home != scattered);
    private static T Take<T>(List<T> pool)
    {
        int i = UnityEngine.Random.Range(0, pool.Count);
        var value = pool[i]; pool.RemoveAt(i); return value;
    }
    private static bool Valid<T>(T value) where T : struct => Enum.IsDefined(typeof(T), value);
    private static bool Fail(string message, out string error) { error = message; return false; }
}

// Retain the serialized first-version input without exposing manual question editing.
[Serializable]
public class Stage6LegacyQuestion
{
    public string taskId;
    public int level;
    public List<Stage6Storage> scatterPlaces;
    public List<Stage6PersonPlacement> people;
    public List<Stage6Delivery> deliveries;
    public List<AudioClip> instructionVoices;
    public string instructionMemo;
}
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(Stage6Data))]
public class Stage6DataInspector : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        UnityEditor.EditorGUILayout.HelpBox("素材とレベル条件から音声・正解を同時生成します。場所を省略する場合は人物を2部屋で重複させず、各場所の人数は上限として扱います。", UnityEditor.MessageType.Info);
        if (GUILayout.Button("設定を確認"))
        {
            var data = (Stage6Data)target;
            if (data.TryValidate(out var error)) Debug.Log("Stage6: 設定OK", data);
            else Debug.LogWarning(error, data);
        }
    }
}
#endif
