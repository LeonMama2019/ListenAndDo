using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ImageData", menuName = "ListenAndDo/Image")]
public class ImageData : ScriptableObject
{
    [Header("オブジェクト画像")]
    public Sprite[] answerImages;
}
