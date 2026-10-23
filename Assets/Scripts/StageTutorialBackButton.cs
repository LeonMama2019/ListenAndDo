using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the scene's Back button available above tutorial overlays and returns
/// directly to the selected stage's menu while a tutorial is running.
/// </summary>
public static class StageTutorialBackButton
{
    public static void Bind(Canvas canvas, int stage)
    {
        if (canvas == null) return;

        Transform back = null;
        foreach (var child in canvas.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, "Back", System.StringComparison.OrdinalIgnoreCase))
            {
                back = child;
                break;
            }
        }

        if (back == null) return;

        var button = back.GetComponent<Button>();
        if (button == null) button = back.gameObject.AddComponent<Button>();

        var graphic = back.GetComponent<Graphic>();
        if (graphic == null) graphic = back.GetComponentInChildren<Graphic>(true);
        if (graphic != null)
        {
            graphic.raycastTarget = true;
            button.targetGraphic = graphic;
        }

        // Detach the serialized gameplay/confirmation callback for the tutorial.
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => StageLevelMenu.OpenStageMenu(stage));

        // Stage2 has a full-screen gesture catcher, and other tutorials draw
        // overlays above the panel. Keep Back above those elements without
        // changing its on-screen position.
        back.SetParent(canvas.transform, true);
        back.SetAsLastSibling();
    }
}
