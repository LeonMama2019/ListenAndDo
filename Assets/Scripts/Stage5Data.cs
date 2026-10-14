using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Stage5Question
{
    public Stage5Question() { }

    public Stage5Question(string id, int questionLevel, string text, AudioClip voice,
        IEnumerable<Stage5Place> places, IEnumerable<Sprite> sprites)
    {
        taskId = id;
        level = questionLevel;
        questionText = text;
        questionVoice = voice;
        tidyPlaces = places == null ? new() : new List<Stage5Place>(places);
        deskObjects = sprites == null ? new() : new List<Sprite>(sprites);
    }

    [Tooltip("問題ごとの固定ID。例: Stage5_Lv1_Q1")]
    [SerializeField] private string taskId;
    [Range(1, 8)]
    [SerializeField] private int level = 1;
    [TextArea(2, 5)]
    [SerializeField] private string questionText;
    [SerializeField] private AudioClip questionVoice;
    [Tooltip("選択した親の子をDeskAreaへ散らかし、同じ場所へ片付ける。本はBookFrontを使う。")]
    [SerializeField] private List<Stage5Place> tidyPlaces = new();
    [Tooltip("机に残す画像素材を直接登録。本はBookFrontのSprite。Pencaseでは指定画像と同じ種類を1個だけ残す。")]
    [SerializeField] private List<Sprite> deskObjects = new();

    public string TaskId => taskId;
    public int Level => level;
    public string QuestionText => questionText;
    public AudioClip QuestionVoice => questionVoice;
    public IReadOnlyList<Stage5Place> TidyPlaces => tidyPlaces;
    public IReadOnlyList<Sprite> DeskObjects => deskObjects;
    [NonSerialized] public List<AudioClip> RuntimeVoices;
    public IReadOnlyList<AudioClip> GetVoices() => RuntimeVoices ?? new List<AudioClip> { questionVoice };

    public bool TryValidate(out string error)
    {
        if (string.IsNullOrWhiteSpace(taskId) || level < 1 || level > 8)
        {
            error = "Task IDとLevel（1〜8）を設定してください。";
            return false;
        }
        if (RuntimeVoices == null && (string.IsNullOrWhiteSpace(questionText) || questionVoice == null))
        {
            error = $"{taskId}: 問題文と音声を設定してください。";
            return false;
        }
        if (tidyPlaces == null || tidyPlaces.Count == 0)
        {
            error = $"{taskId}: 対象の親を1つ以上設定してください。";
            return false;
        }
        var places = new HashSet<Stage5Place>();
        foreach (var place in tidyPlaces)
        {
            if (!Enum.IsDefined(typeof(Stage5Place), place) || !places.Add(place))
            {
                error = $"{taskId}: 対象の親が無効、または重複しています。";
                return false;
            }
        }
        if (deskObjects == null)
        {
            error = $"{taskId}: 机に残す画像リストが無効です。";
            return false;
        }
        foreach (var sprite in deskObjects)
        {
            if (sprite == null)
            {
                error = $"{taskId}: 机に残す画像が未設定です。";
                return false;
            }
        }
        error = null;
        return true;
    }
}

[CreateAssetMenu(fileName = "Stage5Data", menuName = "ListenAndDo/Stage5/Question List")]
public class Stage5Data : ScriptableObject
{
    [Header("問題一覧（＋で次の問題を追加）")]
    [Tooltip("この一覧の中で全問題を設定。問題ごとの別アセットは不要です。")]
    [SerializeField] private List<Stage5Question> questions = new();

    public IReadOnlyList<Stage5Question> Questions => questions;

    public List<Stage5Question> GetQuestions(int level)
    {
        var result = new List<Stage5Question>();
        foreach (var question in questions)
            if (question != null && question.Level == level)
                result.Add(question);
        return result;
    }

    public bool TryValidate(out string error)
    {
        if (questions == null || questions.Count == 0)
        {
            error = "Questionsに問題を追加してください。";
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
}
