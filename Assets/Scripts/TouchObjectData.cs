using UnityEngine;

[CreateAssetMenu(fileName = "TouchObject", menuName = "ListenAndDo/Touch Object")]
public class TouchObjectData : ScriptableObject
{
    [SerializeField] private string objectId;
    [SerializeField] private AudioClip touchInstruction;

    public string ObjectId => objectId;
    public AudioClip TouchInstruction => touchInstruction;
}
