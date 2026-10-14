using System;
using System.Collections.Generic;
using UnityEngine;

public enum Stage5Place { BookStand = 0, Odougu = 1, Pencase = 2 }

[CreateAssetMenu(fileName = "Stage5Task", menuName = "ListenAndDo/Stage5/Task Data")]
public class Stage5TaskData : ScriptableObject
{
    [Header("オブジェクト：名前・画像・名前の音声だけ")]
    [SerializeField] private List<Stage5ObjectEntry> objects = new();
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
        public AudioClip Voice => setting.count == 1 ? item.voice : setting.quantityVoice;
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
        var levelIds = new HashSet<int>();
        foreach (var config in levels)
        {
            if (config == null || config.level < 1 || config.level > 8 || !levelIds.Add(config.level)
                || !Enum.IsDefined(typeof(Stage5Place), config.place) || config.questionCount < 1
                || config.minObjectKinds < 1 || config.maxObjectKinds < config.minObjectKinds
                || config.targets == null || config.targets.Count == 0)
            { error = "出題設定のレベル・収納先・問題数・対象を確認してください。"; return false; }
            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var target in config.targets)
            {
                if (target == null || !ids.Contains(target.objectId ?? "") || target.count < 1
                    || !targets.Add(target.objectId + "#" + target.count)
                    || (target.count > 1 && target.quantityVoice == null))
                { error = "出題対象の名前・個数を確認してください。複数個は個数込みの音声も登録します。"; return false; }
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
            if (!insertsObjects) { error = format.id + ": Objectsを再生順に入れてください。"; return false; }
        }
        return true;
    }

    public List<Stage5Question> GetQuestions(int targetLevel)
    {
        var result = new List<Stage5Question>();
        if (!TryValidate(out var error)) { Debug.LogError(error, this); return result; }
        var config = levels.Find(entry => entry.level == targetLevel);
        if (config == null) { Debug.LogError("選択レベルの出題設定がありません。", this); return result; }
        var formats = questionFormats.FindAll(format => (format.level <= targetLevel)
            && Eligible(format, config).Count >= config.minObjectKinds);
        if (formats.Count == 0) { Debug.LogError("対象数に合う質問形式がありません。", this); return result; }
        for (int n = 0; n < config.questionCount; n++)
        {
            var format = formats[UnityEngine.Random.Range(0, formats.Count)];
            var pool = Eligible(format, config);
            int kinds = UnityEngine.Random.Range(config.minObjectKinds, Math.Min(config.maxObjectKinds, pool.Count) + 1);
            var selected = new List<Target>();
            var sprites = new List<Sprite>();
            for (int i = 0; i < kinds; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                var variants = pool[index];
                var target = variants[UnityEngine.Random.Range(0, variants.Count)];
                pool.RemoveAt(index);
                selected.Add(target);
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
                        clips.Add(selected[i].Voice);
                        text.Append(selected[i].item.id).Append("×").Append(selected[i].setting.count);
                    }
            }
            var question = new Stage5Question("Random_Lv" + targetLevel + "_" + (n + 1) + "_" + format.id,
                targetLevel, text.ToString(), null, new[] { config.place }, sprites);
            question.RuntimeVoices = clips;
            result.Add(question);
        }
        return result;
    }

    private List<List<Target>> Eligible(Stage5QuestionFormat format, Stage5LevelSettings config)
    {
        var groups = new List<List<Target>>();
        foreach (var setting in config.targets)
        {
            if (format.objectIds.Count > 0 && !format.objectIds.Contains(setting.objectId)) continue;
            var item = objects.Find(entry => entry.id == setting.objectId);
            var group = groups.Find(entries => entries[0].item.sprite == item.sprite);
            if (group == null) { group = new List<Target>(); groups.Add(group); }
            group.Add(new Target { item = item, setting = setting });
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
public class Stage5LevelSettings
{
    [Range(1, 8)] public int level = 1;
    public Stage5Place place;
    [Min(1)] public int questionCount = 7;
    [Min(1)] public int minObjectKinds = 1;
    [Min(1)] public int maxObjectKinds = 1;
    public List<Stage5TargetSetting> targets = new();
}

[Serializable]
public class Stage5TargetSetting
{
    [Tooltip("Objectsに登録したオブジェクト名")]
    public string objectId;
    [Tooltip("机に残す個数。同じ名前で個数違いを登録すると、その個数も抽選します")]
    [Min(1)] public int count = 1;
    [Tooltip("2個以上の場合だけ指定。例：鉛筆2本。1個ならObjectsの名前の音声を使います")]
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
