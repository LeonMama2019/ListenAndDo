using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [Header("問題")]
    [Tooltip("回答ログに保存する固定の問題ID")]
    [SerializeField] private string taskId;
    [Range(1, 8)]
    [SerializeField] private int level = 1;
    [TextArea(2, 5)]
    [SerializeField] private string questionText;
    [SerializeField] private AudioClip questionVoice;

    [Header("片付ける場所（数は自由）")]
    [Tooltip("指定した場所に属する物を片付ける。ただし机に残す指定物は除く。")]
    [SerializeField] private List<Stage5Place> tidyPlaces = new();

    [Header("机に残す物（数は自由）")]
    [Tooltip("DeskAreaに残す本・道具のImageコンポーネントを登録。ProjectのPrefab内のImageを指定してください。シーンのImageはアセットに保存できません。")]
    [SerializeField] private List<Image> deskObjects = new();

    public string TaskId => taskId;
    public int Level => level;
    public string QuestionText => questionText;
    public AudioClip QuestionVoice => questionVoice;
    public IReadOnlyList<Stage5Place> TidyPlaces => tidyPlaces;
    public IReadOnlyList<Image> DeskObjects => deskObjects;

    // 出題前に設定不備を検出するための検証。実際の配置判定はQuiz側で行う。
    public bool TryValidate(out string error)
    {
        if (string.IsNullOrWhiteSpace(taskId))
        {
            error = "Task IDを設定してください。";
            return false;
        }
        if (level < 1 || level > 8)
        {
            error = "Levelは1〜8にしてください。";
            return false;
        }
        if (string.IsNullOrWhiteSpace(questionText) || questionVoice == null)
        {
            error = "問題文と問題文の音声を設定してください。";
            return false;
        }
        if (tidyPlaces == null || tidyPlaces.Count == 0)
        {
            error = "片付ける場所を1つ以上設定してください。";
            return false;
        }
        var places = new HashSet<Stage5Place>();
        foreach (var place in tidyPlaces)
        {
            if (!Enum.IsDefined(typeof(Stage5Place), place) || !places.Add(place))
            {
                error = "片付ける場所が無効、または重複しています。";
                return false;
            }
        }
        if (deskObjects == null)
        {
            error = "机に残す物のリストが無効です。";
            return false;
        }
        var objects = new HashSet<Image>();
        foreach (var item in deskObjects)
        {
            if (item == null)
            {
                error = "机に残す物のImageをアタッチしてください。";
                return false;
            }
            if (!objects.Add(item))
            {
                error = "机に残す物の同じImageが重複しています。";
                return false;
            }
        }
        error = null;
        return true;
    }
}
