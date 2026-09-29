using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
// Object1～3は表示枠。選ばれたデータのSpriteと指示音声をここへ割り当てる。
public class TouchObjectTarget : MonoBehaviour
{
    [SerializeField] private TouchObjectData data;
    private Image image;

    public TouchObjectData Data => data;

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

    public void SetData(TouchObjectData newData)
    {
        data = newData;
        if (image == null) image = GetComponent<Image>();
        RefreshImage();
    }

    private void RefreshImage()
    {
        if (data != null)
            image.sprite = data.Sprite;
    }
}
