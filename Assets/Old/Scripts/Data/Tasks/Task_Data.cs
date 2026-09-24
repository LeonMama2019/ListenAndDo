using UnityEditor.Experimental.GraphView;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Task_",
    menuName = "ListenAndDo/Task"
)]
public class TaskData : ScriptableObject
{
    [Header("動詞")]
    public VerbData verb;

    [Header("対象オブジェクト")]
    public ObjectData targetObject;

    [Header("対象の形容詞（任意）")]
    public AdjectiveData targetAdjective;

    [Header("対象の選択条件（任意）")]
    public SelectorData targetSelector;

    [Header("前置詞（任意）")]
    public PrepositionData preposition;

    [Header("基準オブジェクト（任意）")]
    public ObjectData referenceObject;

    [Header("基準オブジェクトの形容詞（任意）")]
    public AdjectiveData referenceAdjective;

    [Header("基準オブジェクトの選択条件（任意）")]
    public SelectorData referenceSelector;

    [Header("場所（任意）")]
    public PlaceData place;

    [Header("オブジェクト画像")]
    public Sprite answerImage;

    [Header("音声")]
    public AudioClip voiceClip;

    [Header("確認する情報")]
    public InspectPropertyType inspectProperty;
}

public enum TaskGoalType
{
    MoveObject,
    InteractObject,
    InspectProperty
}

public enum InspectPropertyType
{
    None,
    Time,
    Color,
    Number,
    Shape,
    Name
}