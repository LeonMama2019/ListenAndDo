using UnityEngine;

[CreateAssetMenu(fileName = "ObjectID_", menuName = "ListenAndDo/ObjectID")]
public class TouchObjectData : ScriptableObject
{
    [SerializeField] private string objectId;
    [SerializeField] private Sprite sprite;
    [SerializeField] private AudioClip touchInstruction;

    public string ObjectId => objectId;
    public Sprite Sprite => sprite;
    public AudioClip TouchInstruction => touchInstruction;
}
