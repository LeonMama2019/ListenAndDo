using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Stage4PersonEntry
{
    [Tooltip("記録に使う固定ID。例: girl1 / girl2")]
    [SerializeField] private string personId;
    [SerializeField] private string displayName;
    [Tooltip("正面の画像")]
    [SerializeField] private Sprite frontSprite;
    [Tooltip("後ろ姿の画像")]
    [SerializeField] private Sprite backSprite;
    [Tooltip("人物名の音声。例: Aちゃん")]
    [SerializeField] private AudioClip nameVoice;

    public string PersonId => personId;
    public string DisplayName => displayName;
    public Sprite FrontSprite => frontSprite;
    public Sprite BackSprite => backSprite;
    public AudioClip NameVoice => nameVoice;
    public bool IsReady => !string.IsNullOrWhiteSpace(personId)
                           && frontSprite != null
                           && backSprite != null
                           && nameVoice != null;
}

[Serializable]
public class Stage4ObjectEntry
{
    [Tooltip("記録に使う固定ID。例: red_book / apple")]
    [SerializeField] private string objectId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite sprite;
    [Tooltip("持ち物名の音声。例: あかいほん")]
    [SerializeField] private AudioClip nameVoice;

    public string ObjectId => objectId;
    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public AudioClip NameVoice => nameVoice;
    public bool IsReady => !string.IsNullOrWhiteSpace(objectId)
                           && sprite != null
                           && nameVoice != null;
}

[CreateAssetMenu(fileName = "Stage4Data", menuName = "ListenAndDo/Stage4/Stage4 Data")]
public class Stage4Data : ScriptableObject
{
    [Header("レベル設定")]
    [Tooltip("Lv1〜8。提示時間は全レベル共通で5秒。")]
    [SerializeField] private List<Stage4LevelSetting> levels = new()
    {
        new Stage4LevelSetting(),
        new Stage4LevelSetting(),
        new Stage4LevelSetting(),
        new Stage4LevelSetting(),
        new Stage4LevelSetting(),
        new Stage4LevelSetting(),
        new Stage4LevelSetting(),
        new Stage4LevelSetting()
    };

    [SerializeField] private float memorizeSeconds = 5f;

    [Header("人物")]
    [SerializeField] private List<Stage4PersonEntry> persons = new();

    [Header("持ち物")]
    [SerializeField] private List<Stage4ObjectEntry> objects = new();

    [Header("質問文テンプレート")]
    [Tooltip("{Object}を持っていたのは誰？")]
    [SerializeField] private string whoQuestionText = "{Object}を持っていたのは誰？";

    [Tooltip("{Person}は何を持っていましたか？")]
    [SerializeField] private string whatQuestionText = "{Person}は何を持っていましたか？";

    [Tooltip("{Person}が持っていなかったのはどれ？")]
    [SerializeField] private string notQuestionText = "{Person}が持っていなかったのはどれ？";

    [Tooltip("CheckパターンA")]
    [SerializeField] private string checkPatternAText = "{Person}が持っていたのは{Object}ですか？";

    [Tooltip("CheckパターンB")]
    [SerializeField] private string checkPatternBText = "{Person}は{Object}を持っていましたか？";

    [Header("Check質問の音声パーツ")]
    [Tooltip("パターンA: ○○ちゃん +「が持っていたのは」+ アイテム +「ですか？」")]
    [SerializeField] private AudioClip checkHadItemWasVoice;
    [SerializeField] private AudioClip checkIsItVoice;

    [Tooltip("パターンB: ○○ちゃん +「は」+ アイテム +「を持っていましたか？」")]
    [SerializeField] private AudioClip checkPersonWaVoice;
    [SerializeField] private AudioClip checkHadItemQuestionVoice;

    public IReadOnlyList<Stage4LevelSetting> Levels => levels;
    public float MemorizeSeconds => memorizeSeconds;

    public IReadOnlyList<Stage4PersonEntry> Persons => persons;
    public IReadOnlyList<Stage4ObjectEntry> Objects => objects;

    public string WhoQuestionText => whoQuestionText;
    public string WhatQuestionText => whatQuestionText;
    public string NotQuestionText => notQuestionText;
    public string CheckPatternAText => checkPatternAText;
    public string CheckPatternBText => checkPatternBText;

    public AudioClip CheckHadItemWasVoice => checkHadItemWasVoice;
    public AudioClip CheckIsItVoice => checkIsItVoice;
    public AudioClip CheckPersonWaVoice => checkPersonWaVoice;
    public AudioClip CheckHadItemQuestionVoice => checkHadItemQuestionVoice;
}
