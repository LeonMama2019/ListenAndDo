using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class StageMenuBanner : MonoBehaviour
{
    [Tooltip("Element 0 = Stage1, Element 1 = Stage2, and so on. Add stage artwork here.")]
    [SerializeField] private Sprite[] stageBanners = new Sprite[6];

    private void Start()
    {
        int index = StageLevelMenu.SelectedStage - 1;
        var image = GetComponent<Image>();
        Sprite banner = stageBanners != null && index >= 0 && index < stageBanners.Length
            ? stageBanners[index] : null;
        image.enabled = banner != null;
        if (banner != null) image.sprite = banner;
    }
}
