using System;
using System.Collections.Generic;
using UnityEngine;

public enum Stage6Room { Class1, Ground }
public enum Stage6Person { Teacher, Boy, Girl }
public enum Stage6Storage { Pencase, Odougu }

[Serializable]
public class Stage6Item
{
    [Tooltip("アイテムごとの一意な名前。例：Pencil1")]
    public string id;
    public Sprite sprite;
    public Stage6Storage home;
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
    [Tooltip("Itemsに登録したID。1件につき1個を届けます")]
    public string itemId;
    public Stage6Room room;
    public Stage6Person person;
}

[Serializable]
public class Stage6Question
{
    public string taskId;
    [Range(1, 8)] public int level = 1;
    public List<Stage6Storage> scatterPlaces = new() { Stage6Storage.Pencase };
    public List<Stage6PersonPlacement> people = new();
    public List<Stage6Delivery> deliveries = new();
    [Tooltip("音声を再生順に登録。場所を言わない問題も、Deliveriesには正解の部屋を登録")]
    public List<AudioClip> instructionVoices = new();
    [TextArea(2, 5), Tooltip("制作・ログ用。ゲーム画面には表示しません")]
    public string instructionMemo;

    public bool SpecifiesRoom => level != 5 && level != 7 && level != 8;
    public int RequiredDeliveryCount => level == 8 ? 3 : level >= 6 ? 2 : 1;
    public int PeoplePerRoomLimit => level <= 3 ? 1 : 3;
}

[CreateAssetMenu(fileName = "Stage6Data", menuName = "ListenAndDo/Stage6/Question List")]
public class Stage6Data : ScriptableObject
{
    [Header("アイテム一覧：収納先を1回だけ登録")]
    [SerializeField] private List<Stage6Item> items = new();
    [Header("問題一覧：Level＋Task IDごとに追加")]
    [SerializeField] private List<Stage6Question> questions = new();
    public IReadOnlyList<Stage6Item> Items => items;
    public IReadOnlyList<Stage6Question> Questions => questions;
    public Stage6Item FindItem(string id) => items?.Find(x => x != null && x.id == id);
    public List<Stage6Question> GetQuestions(int level)
        => questions == null ? new() : questions.FindAll(x => x != null && x.level == level);

    public bool TryValidate(out string error)
    {
        error = null;
        if (items == null || items.Count == 0 || questions == null || questions.Count == 0)
            return Fail("ItemsとQuestionsを登録してください。", out error);
        var itemIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
            if (item == null || string.IsNullOrWhiteSpace(item.id) || !itemIds.Add(item.id)
                || item.sprite == null || !Enum.IsDefined(typeof(Stage6Storage), item.home))
                return Fail("ItemsのID重複・画像・収納先を確認してください。", out error);
        var taskIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var q in questions)
        {
            if (q == null || string.IsNullOrWhiteSpace(q.taskId) || !taskIds.Add(q.taskId)
                || q.level < 1 || q.level > 8)
                return Fail("Task IDは一意、Levelは1〜8で設定してください。", out error);
            string prefix = q.taskId + ": ";
            var places = new HashSet<Stage6Storage>();
            if (q.scatterPlaces == null) return Fail(prefix + "散らかす収納先が未設定です。", out error);
            foreach (var place in q.scatterPlaces)
            {
                if (!Enum.IsDefined(typeof(Stage6Storage), place) || !places.Add(place))
                    return Fail(prefix + "散らかす収納先が無効または重複しています。", out error);
                if (!items.Exists(x => x.home == place))
                    return Fail(prefix + "散らかす収納先にアイテムがありません。", out error);
            }
            if (places.Count != (q.level <= 2 ? 1 : 2))
                return Fail(prefix + "Lv1〜2は片方、Lv3〜8は両方を散らかします。", out error);
            var people = new HashSet<string>();
            var personCounts = new Dictionary<Stage6Person, int>();
            int classCount = 0, groundCount = 0;
            if (q.people == null) return Fail(prefix + "人物配置が未設定です。", out error);
            foreach (var p in q.people)
            {
                if (p == null || !Enum.IsDefined(typeof(Stage6Room), p.room)
                    || !Enum.IsDefined(typeof(Stage6Person), p.person) || !people.Add(Key(p.room, p.person)))
                    return Fail(prefix + "人物配置が無効または重複しています。", out error);
                if (p.room == Stage6Room.Class1) classCount++; else groundCount++;
                personCounts.TryGetValue(p.person, out int count);
                personCounts[p.person] = count + 1;
            }
            if (classCount < 1 || groundCount < 1 || classCount > q.PeoplePerRoomLimit || groundCount > q.PeoplePerRoomLimit
                || (q.level == 4 && (classCount != 3 || groundCount != 3)))
                return Fail(prefix + "Lv1〜3は各場所1人、Lv4は各3人、Lv5以降は各1〜3人にしてください。", out error);
            if (q.deliveries == null || q.deliveries.Count != q.RequiredDeliveryCount)
                return Fail(prefix + "お使いの件数がレベルに合いません。", out error);
            var deliveredItems = new HashSet<string>(StringComparer.Ordinal);
            var recipients = new HashSet<string>();
            foreach (var d in q.deliveries)
            {
                var item = d == null ? null : FindItem(d.itemId);
                if (item == null || !deliveredItems.Add(d.itemId) || !people.Contains(Key(d.room, d.person))
                    || !recipients.Add(Key(d.room, d.person)))
                    return Fail(prefix + "お使いのアイテム・相手・配置を確認してください。物と届け先の重複はできません。", out error);
                if ((q.level == 1 && !places.Contains(item.home)) || (q.level == 2 && places.Contains(item.home)))
                    return Fail(prefix + "Lv1は散らかった側、Lv2は散らかっていない側の物を届けます。", out error);
                if (!q.SpecifiesRoom && personCounts[d.person] != 1)
                    return Fail(prefix + "場所を言わない問題の相手は、全体で1人だけにしてください。", out error);
            }
            if (q.instructionVoices == null || q.instructionVoices.Count == 0 || q.instructionVoices.Exists(x => x == null))
                return Fail(prefix + "指示音声を再生順に登録してください。", out error);
        }
        return true;
    }
    private static string Key(Stage6Room room, Stage6Person person) => room + "/" + person;
    private static bool Fail(string message, out string error) { error = message; return false; }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(Stage6Data))]
public class Stage6DataInspector : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        UnityEditor.EditorGUILayout.HelpBox("Lv1:片方→同じ側 / Lv2:片方→反対側 / Lv3:両方 / Lv4:各3人 / Lv5:場所なし / Lv6:2件・場所あり / Lv7:2件・場所なし / Lv8:3件・場所なし。場所なしでは対象人物を全体で1人にします。", UnityEditor.MessageType.Info);
        if (GUILayout.Button("問題設定を確認"))
        {
            var data = (Stage6Data)target;
            if (data.TryValidate(out var error)) Debug.Log("Stage6: 問題設定OK", data);
            else Debug.LogWarning(error, data);
        }
    }
}
#endif
