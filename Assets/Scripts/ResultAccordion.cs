using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Result画面のレベル別アコーディオン。
/// 詳細を開閉し、レベル項目の高さも同時に変更する。
/// </summary>
public class ResultAccordion : MonoBehaviour
{
    [System.Serializable]
    public class LevelItem
    {
        [Tooltip("クリックするボタン（▼/▶を含むButton）")]
        public Button toggleButton;

        [Tooltip("開閉する詳細パネル。詳細の文字や画像はこの子に入れる")]
        public GameObject detail;

        [Tooltip("▶ / ▼ を表示するTextMeshPro")]
        public TMP_Text arrowText;

        [Tooltip("詳細を含むレベル全体のRectTransform")]
        public RectTransform levelRoot;

        [Tooltip("詳細を閉じたときの高さ")]
        public float closedHeight = 50f;

        [Tooltip("詳細を開いたときの高さ")]
        public float openHeight = 183f;
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
            RefreshAll();
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

        if (item.levelRoot != null)
        {
            item.levelRoot.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                open ? item.openHeight : item.closedHeight);
        }

        RebuildLayout(item);
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

    private void RefreshAll()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (!IsValid(i)) continue;
            SetOpen(i, levels[i].detail.activeSelf);
        }
    }

    private static void RebuildLayout(LevelItem item)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform target = item.levelRoot != null
            ? item.levelRoot.parent as RectTransform
            : item.detail.transform.parent as RectTransform;

        if (target != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(target);
    }
}
