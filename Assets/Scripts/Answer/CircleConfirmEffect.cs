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
    [SerializeField] private float drawDuration = 0.3f;

    [Header("丸完成後、OKを出すまでの待ち時間")]
    [SerializeField] private float okDelay = 0.1f;

    private Coroutine showCoroutine;

    private void Start()
    {
        // 最初は丸とOKを非表示にする
        circleImage.fillAmount = 0f;
        circleImage.gameObject.SetActive(false);

        okPanel.SetActive(false);
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
        // 最初の状態に戻す
        okPanel.SetActive(false);

        circleImage.gameObject.SetActive(true);
        circleImage.fillAmount = 0f;

        float elapsedTime = 0f;

        // 丸を徐々に表示
        while (elapsedTime < drawDuration)
        {
            elapsedTime += Time.deltaTime;

            circleImage.fillAmount =
                Mathf.Clamp01(elapsedTime / drawDuration);

            yield return null;
        }

        // 確実に丸を完成させる
        circleImage.fillAmount = 1f;

        // 少し待つ
        yield return new WaitForSeconds(okDelay);

        // OK？を表示
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