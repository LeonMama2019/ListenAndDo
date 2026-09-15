using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Result画面のレベル1〜7用アコーディオン。
/// 各レベルの▼/▶ボタンを押すと詳細を開閉する。
/// Content / Level に VerticalLayoutGroup + ContentSizeFitter を使えば、
/// 開いた分だけ下のレベルが自動で押し下げられる。
/// </summary>
public class ResultAccordion : MonoBehaviour
{
    [System.Serializable]
    public class LevelItem
    {
        [Tooltip("クリックするボタン（▼/▶を含むButton）")]
        public Button toggleButton;

        [Tooltip("開閉する詳細GameObject")]
        public GameObject detail;

        [Tooltip("▶ / ▼ を表示するTextMeshPro。Button自身が文字ならそのTMPを指定")]
        public TMP_Text arrowText;
    }

    [Header("レベル1〜7を順番に登録")]
    [SerializeField] private LevelItem[] levels = new LevelItem[7];

    [Header("起動時")]
    [SerializeField] private bool closeAllOnStart = true;

    private void Awake()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            int index = i;
            LevelItem item = levels[i];
            if (item == null || item.toggleButton == null) continue;

            item.toggleButton.onClick.AddListener(() => Toggle(index));
        }

        if (closeAllOnStart)
        {
            for (int i = 0; i < levels.Length; i++)
                SetOpen(i, false);
        }
        else
        {
            RefreshArrows();
        }
    }

    public void Toggle(int index)
    {
        if (!IsValid(index)) return;
        SetOpen(index, !levels[index].detail.activeSelf);
    }

    public void SetOpen(int index, bool open)
    {
        if (!IsValid(index)) return;

        LevelItem item = levels[index];
        item.detail.SetActive(open);

        if (item.arrowText != null)
            item.arrowText.text = open ? "▼" : "▶";

        // LayoutGroupの再計算を即時反映。
        RectTransform rect = item.detail.transform.parent as RectTransform;
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    public void CloseAll()
    {
        for (int i = 0; i < levels.Length; i++)
            SetOpen(i, false);
    }

    private bool IsValid(int index)
    {
        return index >= 0 &&
               index < levels.Length &&
               levels[index] != null &&
               levels[index].detail != null;
    }

    private void RefreshArrows()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (!IsValid(i)) continue;
            LevelItem item = levels[i];
            if (item.arrowText != null)
                item.arrowText.text = item.detail.activeSelf ? "▼" : "▶";
        }
    }
}
