using System;
using System.Collections.Generic;
using UnityEngine;

public enum Stage5Place
{
    BookStand = 0,
    Odougu = 1,
    Pencase = 2
}

/// <summary>
/// 1問の最終配置条件。操作順や本の色順は正誤に含めない。
/// シーンのPanelや本の実行中の位置は保持しない。
/// </summary>
[CreateAssetMenu(fileName = "Stage5Task", menuName = "ListenAndDo/Stage5/Task Data")]
public class Stage5TaskData : ScriptableObject
{
    [Header("オブジェクト")]
    [SerializeField] private List<Stage5ObjectEntry> objects = new();
    [Header("質問形式：音声とObjectsを再生順に追加")]
    [SerializeField] private List<Stage5QuestionFormat> questionFormats = new();
    [Header("接続音声")]
    [SerializeField] private AudioClip conjunctionVoice;
    [Header("出題設定")]
    [Min(1)] [SerializeField] private int questionsPerLevel = 7;
    [Min(1)] [SerializeField] private int minObjectKinds = 1;
    [Min(1)] [SerializeField] private int maxObjectKinds = 1;

    [Header("以前の固定問題（入力済みデータを保持）")]
    [SerializeField] private List<Stage5Question> questions = new();
    public IReadOnlyList<Stage5Question> Questions => questions;

    // 以前の1問形式から、入力済みの設定を失わずに移行する。
    private void OnEnable() => MigrateLegacyQuestion();
    private void OnValidate() => MigrateLegacyQuestion();

    private void MigrateLegacyQuestion()
    {
        if (questions == null)
            questions = new List<Stage5Question>();
        if (questions.Count == 0 && !string.IsNullOrWhiteSpace(taskId))
        {
            questions.Add(new Stage5Question(taskId, level, questionText,
                questionVoice, tidyPlaces, deskObjects));
            taskId = null;
        }
    }

    [SerializeField, HideInInspector] private string taskId;
    [SerializeField, HideInInspector] private int level = 1;
    [SerializeField, HideInInspector] private string questionText;
    [SerializeField, HideInInspector] private AudioClip questionVoice;
    [SerializeField, HideInInspector] private List<Stage5Place> tidyPlaces = new();
    [SerializeField, HideInInspector] private List<Sprite> deskObjects = new();

    public List<Stage5Question> GetQuestions(int targetLevel)
    {
        if (objects.Count > 0 || questionFormats.Count > 0)
            return GenerateQuestions(targetLevel);
        var result = new List<Stage5Question>();
        foreach (var question in questions)
            if (question != null && question.Level == targetLevel)
                result.Add(question);
        return result;
    }

    public bool TryValidate(out string error)
    {
        if (objects.Count > 0 || questionFormats.Count > 0)
            return ValidateRandom(out error);
        if (questions == null || questions.Count == 0)
        {
            error = "問題を追加してください。";
            return false;
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var question in questions)
        {
            if (question == null)
            {
                error = "未設定の問題があります。";
                return false;
            }
            if (!question.TryValidate(out error))
                return false;
            if (!ids.Add(question.TaskId))
            {
                error = $"Task ID {question.TaskId} が重複しています。";
                return false;
            }
        }
        error = null;
        return true;
    }

    private bool ValidateRandom(out string error)
    {
        error = null;
        if (objects.Count == 0 || questionFormats.Count == 0 || minObjectKinds < 1
            || maxObjectKinds < minObjectKinds || questionsPerLevel < 1)
        { error = "オブジェクト・質問形式・出題数を確認してください。"; return false; }
        var ids = new HashSet<string>();
        foreach (var item in objects)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id) || !ids.Add(item.id + "#" + item.count)
                || item.sprite == null || item.level < 1 || item.level > 8
                || item.count < 1 || item.voice == null
                || !Enum.IsDefined(typeof(Stage5Place), item.place))
            { error = "オブジェクトのID・画像・個数・音声・レベル・収納先を確認してください。同じIDと個数の重複は避けてください。"; return false; }
        }
        foreach (var format in questionFormats)
        {
            if (format == null || string.IsNullOrWhiteSpace(format.id) || format.parts == null
                || format.parts.Count == 0)
            { error = "質問形式にIDと再生順を設定してください。"; return false; }
            int inserts = 0;
            foreach (var part in format.parts)
            {
                if (part == null || !Enum.IsDefined(typeof(Stage5VoicePartKind), part.kind)
                    || (part.kind == Stage5VoicePartKind.Audio && part.voice == null))
                { error = format.id + ": 音声パーツが未設定です。"; return false; }
                if (part.kind == Stage5VoicePartKind.Objects) inserts++;
            }
            if (inserts == 0) { error = format.id + ": Objectsを1つ以上入れてください。"; return false; }
        }
        if (maxObjectKinds > 1 && conjunctionVoice == null)
        { error = "複数の対象には接続音声「と」が必要です。"; return false; }
        return true;
    }

    private List<Stage5Question> GenerateQuestions(int targetLevel)
    {
        var result = new List<Stage5Question>();
        if (!ValidateRandom(out var error)) { Debug.LogError(error, this); return result; }
        var formats = new List<Stage5QuestionFormat>();
        foreach (var format in questionFormats)
        {
            if (format.level != 0 && format.level != targetLevel) continue;
            if (Eligible(format, targetLevel).Count >= minObjectKinds) formats.Add(format);
        }
        if (formats.Count == 0)
        { Debug.LogError("このレベルに対応するオブジェクトと質問形式がありません。", this); return result; }
        for (int n = 0; n < questionsPerLevel; n++)
        {
            var format = formats[UnityEngine.Random.Range(0, formats.Count)];
            var pool = Eligible(format, targetLevel);
            int count = UnityEngine.Random.Range(minObjectKinds, Math.Min(maxObjectKinds, pool.Count) + 1);
            var selected = new List<Stage5ObjectEntry>();
            var sprites = new List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                var item = pool[index]; pool.RemoveAt(index); selected.Add(item);
                for (int j = 0; j < item.count; j++) sprites.Add(item.sprite);
            }
            var clips = new List<AudioClip>();
            var text = new System.Text.StringBuilder();
            foreach (var part in format.parts)
            {
                if (part.kind == Stage5VoicePartKind.Audio)
                { clips.Add(part.voice); text.Append(part.text); }
                else
                    for (int i = 0; i < selected.Count; i++)
                    {
                        if (i > 0) { clips.Add(conjunctionVoice); text.Append("と"); }
                        clips.Add(selected[i].voice);
                        text.Append(selected[i].id).Append("×").Append(selected[i].count);
                    }
            }
            var question = new Stage5Question("Random_Lv" + targetLevel + "_" + (n + 1) + "_" + format.id,
                targetLevel, text.ToString(), null, new[] { format.place }, sprites);
            question.RuntimeVoices = clips;
            result.Add(question);
        }
        return result;
    }
    private List<Stage5ObjectEntry> Eligible(Stage5QuestionFormat format, int targetLevel)
    {
        var result = new List<Stage5ObjectEntry>();
        foreach (var item in objects)
        {
            if (item.level != targetLevel || item.place != format.place
                || (format.objectIds.Count > 0 && !format.objectIds.Contains(item.id))) continue;
            // One entry per sprite in an individual question, even if quantity variants are registered.
            if (!result.Exists(other => other.sprite == item.sprite)) result.Add(item);
            else
            {
                int index = result.FindIndex(other => other.sprite == item.sprite);
                if (UnityEngine.Random.Range(0, 2) == 0) result[index] = item;
            }
        }
        return result;
    }
}

[Serializable]
public class Stage5ObjectEntry
{
    public string id;
    [Range(1, 8)] public int level = 1;
    public Stage5Place place;
    public Sprite sprite;
    [Min(1)] public int count = 1;
    [Tooltip("指定数を含めて読む音声。例：鉛筆2本。助詞「を」は質問形式側に入れる")]
    public AudioClip voice;
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
    [Tooltip("0は全レベル共通")] [Range(0, 8)] public int level;
    public Stage5Place place;
    [Tooltip("空欄は同じ収納先の全対象。塗り絵などでは対応するオブジェクトIDを指定")]
    public List<string> objectIds = new();
    public List<Stage5VoicePart> parts = new();
}
