using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Routes TMP nickname entry through a browser-native HTML input on WebGL,
/// where Unity's built-in input field does not receive Japanese IME input.
/// Other platforms keep using the regular TMP_InputField.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public sealed class WebGLImeTMPInput : MonoBehaviour
{
    private TMP_InputField input;
    private bool dialogOpen;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void ListenAndDo_ShowImeInput(string title, string initialValue, int characterLimit);
    [DllImport("__Internal")] private static extern int ListenAndDo_IsImeInputActive();
    [DllImport("__Internal")] private static extern int ListenAndDo_IsImeInputCanceled();
    [DllImport("__Internal")] private static extern int ListenAndDo_CopyImeInputValue(byte[] buffer, int capacity);
#endif

    private void Awake()
    {
        input = GetComponent<TMP_InputField>();
        input.onSelect.AddListener(OpenBrowserInput);
    }

    private void OnDestroy()
    {
        if (input != null) input.onSelect.RemoveListener(OpenBrowserInput);
    }

    private void OpenBrowserInput(string _)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (dialogOpen || !isActiveAndEnabled) return;
        StartCoroutine(ShowBrowserInput());
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator ShowBrowserInput()
    {
        dialogOpen = true;
        // Wait until TMP has finished handling its selection event before
        // removing focus from the Unity canvas input field.
        yield return new WaitForEndOfFrame();

        input.DeactivateInputField();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        bool captureAllKeyboardInput = WebGLInput.captureAllKeyboardInput;
        WebGLInput.captureAllKeyboardInput = false;
        try
        {
            ListenAndDo_ShowImeInput("ニックネームを入力してください", input.text, input.characterLimit);

            while (ListenAndDo_IsImeInputActive() != 0)
                yield return null;

            if (ListenAndDo_IsImeInputCanceled() == 0)
            {
                int capacity = input.characterLimit > 0 ? input.characterLimit * 4 + 4 : 1024;
                byte[] buffer = new byte[capacity];
                int byteCount = ListenAndDo_CopyImeInputValue(buffer, buffer.Length);
                byteCount = Mathf.Clamp(byteCount, 0, buffer.Length);
                string value = Encoding.UTF8.GetString(buffer, 0, byteCount);
                if (input.characterLimit > 0 && value.Length > input.characterLimit)
                    value = value.Substring(0, input.characterLimit);
                input.text = value;
            }
        }
        finally
        {
            WebGLInput.captureAllKeyboardInput = captureAllKeyboardInput;
            dialogOpen = false;
        }
    }
#endif
}
