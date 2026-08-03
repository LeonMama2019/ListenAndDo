using UnityEngine;
using UnityEngine.UI;

public class HandListSelector : MonoBehaviour
{
    [Header("リスト内で手を表示するImage")]
    [SerializeField] private Image handImage;

    [Header("マウスについてくる手のImage")]
    [SerializeField] private Image handCursorImage;

    [Header("手カーソルのAnimator")]
    [SerializeField] private Animator handCursorAnimator;

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
        if (!cursorEnabled)
            return;

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

        // 手の一覧側
        if (handImage != null)
        {
            handImage.sprite = selectedSprite;
            handImage.preserveAspect = true;
        }

        // マウスカーソル側
        if (handCursorImage != null)
        {
            handCursorImage.sprite = selectedSprite;
            handCursorImage.preserveAspect = true;
        }

        PlayCurrentAnimation();
    }

    private void PlayCurrentAnimation()
    {
        if (handCursorAnimator == null)
            return;

        switch (currentIndex)
        {
            case 0:
                handCursorAnimator.Play("Touch", 0, 0f);
                break;

            case 1:
                handCursorAnimator.Play("Hit", 0, 0f);
                break;

            case 2:
              
                handCursorAnimator.Play("Pick", 0, 0f);
                break;

            case 3:
             
                handCursorAnimator.Play("Point", 0, 0f);
                break;
        }
    }

    public Sprite GetSelectedHand()
    {
        if (handSprites == null ||
            currentIndex < 0 ||
            currentIndex >= handSprites.Length)
        {
            return null;
        }

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
    private bool cursorEnabled = false;

    public void SetCursorEnabled(bool enabled)
    {
        cursorEnabled = enabled;

        if (handCursorImage != null)
            handCursorImage.enabled = enabled;

        Cursor.visible = !enabled;
    }
}