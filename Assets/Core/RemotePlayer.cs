using UnityEngine;

public class RemotePlayer : MonoBehaviour
{
    private Vector3 _targetPos;
    private Quaternion _targetRot;

    // 平滑插值的速度，数值越大跟得越紧，数值越小越平滑但也越像在滑冰
    public float lerpSpeed = 15f;

    private void Start()
    {
        // 初始状态下，目标点就是自己的出生点
        _targetPos = transform.position;
        _targetRot = transform.rotation;
    }

    // 提供给 RoomManager 调用的公共方法，只更新目标点，不直接改坐标
    public void SetTargetPosition(Vector3 pos, float rotY)
    {
        _targetPos = pos;
        _targetRot = Quaternion.Euler(0, rotY, 0);
    }

    private void Update()
    {
        // 最核心的魔法：Vector3.Lerp 和 Quaternion.Lerp
        // 它可以让物体在当前帧，平滑地向目标点靠近一小段距离
        transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, _targetRot, Time.deltaTime * lerpSpeed);
    }
}