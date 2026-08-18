using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CircleConfirmEffect : MonoBehaviour
{
    [Header("徐々に表示する丸のImage")]
    [SerializeField] private Image circleImage;

    [Header("丸が完成した後に表示するOKパネル")]
    [SerializeField] private GameObject okPanel;

    [Header("丸が完成するまでの時間")]
    [SerializeField] private float drawDuration = 3.0f;
    [Header("円を書き始めるまでの待ち時間")]
[SerializeField] private float startDelay = 3.0f;
    [Header("丸完成後、OKを出すまでの待ち時間")]
    [SerializeField] private float okDelay = 1.1f;

    private Coroutine showCoroutine;

    private void Start()
    {
        // 最初は丸とOKを非表示にする
        circleImage.fillAmount = 0f;
        circleImage.gameObject.SetActive(false);

       
    }

    /// <summary>
    /// 選択したときに呼ぶ
    /// </summary>
    public void ShowCircleAndConfirm()
    {
        // 連打された場合は、前の処理を止める
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
        }

        showCoroutine = StartCoroutine(ShowCircleCoroutine());
    }

    private IEnumerator ShowCircleCoroutine()
    {
        if (circleImage == null || okPanel == null)
        {
            Debug.LogWarning("Circle ImageまたはOK Panelが設定されていません");
            showCoroutine = null;
            yield break;
        }

        // 最初の状態に戻す
        okPanel.SetActive(false);

        circleImage.gameObject.SetActive(true);
        circleImage.fillAmount = 0f;
        yield return new WaitForSeconds(startDelay);

        float elapsedTime = 0f;

        while (elapsedTime < drawDuration)
        {
            elapsedTime += Time.deltaTime;

            circleImage.fillAmount =
                Mathf.Clamp01(elapsedTime / drawDuration);

            yield return null;
        }

        circleImage.fillAmount = 1f;

        yield return new WaitForSeconds(okDelay);

        // 丸が完成してからOKを表示
        okPanel.SetActive(true);

        showCoroutine = null;
    }

    /// <summary>
    /// 丸とOKを消して初期状態に戻す
    /// </summary>
    public void ResetEffect()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        circleImage.fillAmount = 0f;
        circleImage.gameObject.SetActive(false);

        okPanel.SetActive(false);
    }
}