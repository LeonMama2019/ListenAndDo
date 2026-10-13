using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Stage5LevelQuestions
{
    [Range(1, 8)]
    [SerializeField] private int level = 1;
    [Tooltip("このレベルの問題。＋でいくつでも追加できます。")]
    [SerializeField] private List<Stage5TaskData> questions = new();

    public int Level => level;
    public IReadOnlyList<Stage5TaskData> Questions => questions;
}

[CreateAssetMenu(fileName = "Stage5Data", menuName = "ListenAndDo/Stage5/Question List")]
public class Stage5Data : ScriptableObject
{
    [Tooltip("＋でレベルを追加し、各レベルのQuestionsに問題を登録します。")]
    [SerializeField] private List<Stage5LevelQuestions> levels = new();

    public IReadOnlyList<Stage5LevelQuestions> Levels => levels;

    public IReadOnlyList<Stage5TaskData> GetQuestions(int level)
    {
        foreach (var entry in levels)
            if (entry != null && entry.Level == level)
                return entry.Questions;
        return Array.Empty<Stage5TaskData>();
    }

    public bool TryValidate(out string error)
    {
        if (levels == null || levels.Count == 0)
        {
            error = "レベルと問題を追加してください。";
            return false;
        }
        var seenLevels = new HashSet<int>();
        var seenTaskIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in levels)
        {
            if (entry == null || entry.Level < 1 || entry.Level > 8
                || !seenLevels.Add(entry.Level))
            {
                error = "レベルが無効、または重複しています。";
                return false;
            }
            if (entry.Questions == null || entry.Questions.Count == 0)
            {
                error = $"Lv{entry.Level}に問題を登録してください。";
                return false;
            }
            foreach (var question in entry.Questions)
            {
                if (question == null)
                {
                    error = $"Lv{entry.Level}に未登録の問題があります。";
                    return false;
                }
                if (!question.TryValidate(out error))
                    return false;
                if (question.Level != entry.Level)
                {
                    error = $"{question.name}のLevelと登録先のレベルが一致しません。";
                    return false;
                }
                if (!seenTaskIds.Add(question.TaskId))
                {
                    error = $"問題ID {question.TaskId} が重複しています。";
                    return false;
                }
            }
        }
        error = null;
        return true;
    }
}
