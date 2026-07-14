using UnityEngine;

[CreateAssetMenu(fileName = "New Task", menuName = "ListenAndDo/Task")]
public class TaskData : ScriptableObject
{
    public VerbData verb;
    public ObjectData targetObject;

    [Header("•\Ž¦•¶")]
    public string englishSentence;
    public string hiraganaSentence;
    public string kanjiSentence;
}