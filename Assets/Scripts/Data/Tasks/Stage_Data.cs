using UnityEngine;

[CreateAssetMenu(menuName = "ListenAndDo/Stage")]
public class StageData : ScriptableObject
{
    public TaskData[] tasks;
}