using UnityEngine;
using UnityEngine.UI;

public class HandListSelector : MonoBehaviour
{
    [Header("リスト内で手を表示するImage")]
    [SerializeField] private Image handImage;

    [Header("マウスについてくる手のImage")]
    [SerializeField] private Image handCursorImage;

    [Header("順番に表示する手の画像")]
    [SerializeField] private Sprite[] handSprites;

    private int currentIndex = 0;

    private void Start()
    {
       
            if (handSprites == null || handSprites.Length == 0)
            {
                Debug.LogWarning("手の画像が登録されていません");
                return;
            }

            Cursor.visible = false;

            if (handCursorImage != null)
            {
                handCursorImage.rectTransform.sizeDelta =
                    new Vector2(50f, 50f);
            }

            ShowCurrentHand();
     }

    private void Update()
    {
        if (handCursorImage == null)
            return;

        handCursorImage.rectTransform.position = Input.mousePosition;
    }

    public void NextHand()
    {
        if (handSprites == null || handSprites.Length == 0)
            return;

        currentIndex++;

        if (currentIndex >= handSprites.Length)
        {
            currentIndex = 0;
        }

        ShowCurrentHand();
    }

    private void ShowCurrentHand()
    {
        Sprite selectedSprite = handSprites[currentIndex];

        if (handImage != null)
        {
            handImage.sprite = selectedSprite;
            handImage.preserveAspect = true;
        }

        if (handCursorImage != null)
        {
            handCursorImage.sprite = selectedSprite;
            handCursorImage.preserveAspect = true;
        }
    }

    public Sprite GetSelectedHand()
    {
        return handSprites[currentIndex];
    }

    public int GetSelectedIndex()
    {
        return currentIndex;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }
}