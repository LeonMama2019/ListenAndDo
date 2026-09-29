using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
// Object1～3は表示枠。抽選された行のSpriteをここへ表示する。
public class TouchObjectTarget : MonoBehaviour
{
    [SerializeField] private TouchObjectData data;
    [SerializeField] private int entryIndex = -1;
    private Image image;

    public TouchObjectData Data => data;
    public TouchObjectEntry Entry => data != null && entryIndex >= 0 && entryIndex < data.Entries.Count
        ? data.Entries[entryIndex] : null;

    private void Awake()
    {
        image = GetComponent<Image>();
        RefreshImage();
    }

    private void OnValidate()
    {
        image = GetComponent<Image>();
        RefreshImage();
    }

    public void SetEntry(TouchObjectData newData, int newIndex)
    {
        data = newData;
        entryIndex = newIndex;
        if (image == null) image = GetComponent<Image>();
        RefreshImage();
    }

    public void SetEntry(TouchObjectData source, TouchObjectEntry selected)
    {
        for (int i = 0; i < source.Entries.Count; i++)
        {
            if (ReferenceEquals(source.Entries[i], selected))
            {
                SetEntry(source, i);
                return;
            }
        }

        Debug.LogError("選ばれた行がObjectIDリストにありません。", this);
    }

    private void RefreshImage()
    {
        if (Entry != null)
            image.sprite = Entry.Sprite;
    }
}
