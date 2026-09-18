using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Result画面のレベル別アコーディオン。
/// Level1〜7の参照をHierarchy名から自動取得し、詳細を開閉する。
/// </summary>
public class ResultAccordion : MonoBehaviour
{
    private const int LevelCount = 7;

    private RectTransform contentRoot;
    private Vector2[] closedPositions = new Vector2[LevelCount];
    private float closedContentHeight;

    [Serializable]
    public class LevelItem
    {
        [Tooltip("クリックするToggleArrow Button")]
        public Button toggleButton;

        [Tooltip("開閉するレベル詳細パネル")]
        public GameObject detail;

        [Tooltip("▶ / ▼ を表示するTextMeshPro（画像矢印なら空欄でOK）")]
        public TMP_Text arrowText;

        [Tooltip("詳細を含むレベル全体のRectTransform")]
        public RectTransform levelRoot;

        [Tooltip("詳細を閉じたときの高さ")]
        public float closedHeight = 50f;

        [Tooltip("詳細サイズを取得できない場合に使う予備の高さ")]
        public float openHeight = 183f;

        [NonSerialized] public Image toggleImage;
        [NonSerialized] public Sprite closedArrowSprite;
        [NonSerialized] public LayoutElement layoutElement;
        [NonSerialized] public bool isOpen;
    }

    [Header("レベル1〜7（起動時に自動で再接続）")]
    [SerializeField] private LevelItem[] levels = new LevelItem[LevelCount];

    [Header("起動時")]
    [SerializeField] private bool closeAllOnStart = true;

    [Header("Detailを開いた時の▼画像")]
    [SerializeField] private Sprite openArrowSprite;

    [Header("Detail下の余白")]
    [SerializeField] private float expandedBottomPadding = 20f;

    [Header("開いたDetailが下のLevelを押し下げる距離")]
    [SerializeField] private float openedRowShift = 85f;

    private void Awake()
    {
        AutoWireLevels();
        ConfigureParentLayout();

        for (int i = 0; i < levels.Length; i++)
        {
            int index = i;
            LevelItem item = levels[i];
            if (item == null || item.toggleButton == null)
                continue;

            item.toggleButton.onClick.AddListener(() => Toggle(index));
        }

        if (closeAllOnStart)
            CloseAll();
        else
            RefreshAll();
    }

    public void Toggle(int index)
    {
        if (!IsValid(index))
            return;

        SetOpen(index, !levels[index].detail.activeSelf);
    }

    public void SetOpen(int index, bool open)
    {
        if (!IsValid(index))
            return;

        LevelItem item = levels[index];
        item.detail.SetActive(open);

        if (item.arrowText != null)
            item.arrowText.text = open ? "▼" : "▶";

        if (item.toggleImage != null && item.closedArrowSprite != null)
        {
            item.toggleImage.sprite =
                open && openArrowSprite != null
                    ? openArrowSprite
                    : item.closedArrowSprite;
        }

        // Detailの高さ計算には頼らず、開閉状態をそのまま配置に使う。
        // levelRoot自体の高さは変えないので、見出しと矢印の位置は動かない。
        item.isOpen = open;

        ReflowLevels();
    }

    public void CloseAll()
    {
        for (int i = 0; i < levels.Length; i++)
            SetOpen(i, false);
    }

    private void AutoWireLevels()
    {
        if (levels == null || levels.Length != LevelCount)
            Array.Resize(ref levels, LevelCount);

        for (int i = 0; i < LevelCount; i++)
        {
            if (levels[i] == null)
                levels[i] = new LevelItem();

            int levelNumber = i + 1;
            LevelItem item = levels[i];
            Transform levelRoot = FindInScene("level" + levelNumber);

            if (levelRoot == null)
            {
                Debug.LogWarning("ResultAccordion: level" + levelNumber + " が見つかりません。");
                continue;
            }

            item.levelRoot = levelRoot as RectTransform;
            item.layoutElement = levelRoot.GetComponent<LayoutElement>();
            if (item.layoutElement == null)
                item.layoutElement = levelRoot.gameObject.AddComponent<LayoutElement>();

            Transform panel = FindChild(
                levelRoot,
                "level" + levelNumber + "panel");

            if (panel != null)
                item.detail = panel.gameObject;
            else
                Debug.LogWarning(
                    "ResultAccordion: level" + levelNumber + "panel が見つかりません。");

            Transform toggle = FindChild(levelRoot, "ToggleArrow");
            if (toggle != null)
            {
                item.toggleButton = toggle.GetComponent<Button>();
                item.arrowText = toggle.GetComponentInChildren<TMP_Text>(true);

                if (item.toggleButton != null)
                {
                    item.toggleImage = item.toggleButton.targetGraphic as Image;
                    if (item.toggleImage == null)
                        item.toggleImage = toggle.GetComponent<Image>();

                    if (item.toggleImage != null)
                        item.closedArrowSprite = item.toggleImage.sprite;
                }
            }

            if (item.toggleButton == null)
                Debug.LogWarning(
                    "ResultAccordion: level" + levelNumber + " のToggleArrow Buttonが見つかりません。");
        }
    }


    private void ConfigureParentLayout()
    {
        if (levels == null || levels.Length == 0 || levels[0] == null ||
            levels[0].levelRoot == null)
            return;

        contentRoot = levels[0].levelRoot.parent as RectTransform;
        if (contentRoot == null)
            return;

        // まず既存のVerticalLayoutGroupに、閉じた状態の正しい位置を計算させる。
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null && levels[i].levelRoot != null)
                closedPositions[i] = levels[i].levelRoot.anchoredPosition;
        }

        closedContentHeight = contentRoot.rect.height;

        // ここから先は位置を直接制御する。
        // VerticalLayoutGroupに次のフレームで戻されないよう停止する。
        VerticalLayoutGroup verticalLayout =
            contentRoot.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout != null)
            verticalLayout.enabled = false;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            fitter.enabled = false;
    }

    private void ReflowLevels()
    {
        if (contentRoot == null)
            return;

        float downwardShift = 0f;

        for (int i = 0; i < levels.Length; i++)
        {
            LevelItem item = levels[i];
            if (item == null || item.levelRoot == null)
                continue;

            Vector2 position = closedPositions[i];
            position.y -= downwardShift;
            item.levelRoot.anchoredPosition = position;

            // このLevelのDetailが開いていれば、
            // 次のLevel以降を青い枠の外まで確実に下げる。
            if (item.isOpen)
                downwardShift += openedRowShift;
        }

        contentRoot.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            closedContentHeight + downwardShift);

        Canvas.ForceUpdateCanvases();
    }

    private bool IsValid(int index)
    {
        bool valid =
            index >= 0 &&
            index < levels.Length &&
            levels[index] != null &&
            levels[index].detail != null;

        if (!valid)
            Debug.LogWarning("ResultAccordion: Level" + (index + 1) + " の参照が不足しています。");

        return valid;
    }

    private void RefreshAll()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (IsValid(i))
                SetOpen(i, levels[i].detail.activeSelf);
        }
    }

    private Transform FindInScene(string objectName)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            Transform found = FindChild(root.transform, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        string targetName = NormalizeName(objectName);
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (NormalizeName(child.name) == targetName)
                return child;
        }

        return null;
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace(" ", string.Empty)
                   .Replace("'", string.Empty)
                   .ToLowerInvariant();
    }


}
