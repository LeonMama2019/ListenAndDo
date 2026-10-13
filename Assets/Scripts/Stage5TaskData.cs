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
    [Header("問題一覧：＋で設定一式を追加")]
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
        var result = new List<Stage5Question>();
        foreach (var question in questions)
            if (question != null && question.Level == targetLevel)
                result.Add(question);
        return result;
    }

    public bool TryValidate(out string error)
    {
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
}
