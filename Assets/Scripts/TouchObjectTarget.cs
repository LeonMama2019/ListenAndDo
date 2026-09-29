using UnityEngine;

// シーン上の画像と、その画像の指示音声・IDを結び付ける。
public class TouchObjectTarget : MonoBehaviour
{
    [SerializeField] private TouchObjectData data;

    public TouchObjectData Data => data;
}
