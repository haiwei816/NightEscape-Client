using UnityEngine;
using Youdu; // 必须和你 proto 里的 package 名字一致

public class PlayerController : MonoBehaviour
{
    private CharacterController _controller;
    private float _moveSpeed = 5.0f;

    // 发包频率控制
    private float _lastSendTime = 0;
    private float _sendInterval = 0.1f; // 100ms 发一次包

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1. 基础移动逻辑
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 dir = new Vector3(h, 0, v);

        if (dir.magnitude > 0.1f)
        {
            _controller.Move(dir * _moveSpeed * Time.deltaTime);
        }

        // 2. 计时器：每 0.1s 向服务器同步一次位置
        if (Time.time - _lastSendTime > _sendInterval)
        {
            SendMoveToSrv();
            _lastSendTime = Time.time;
        }
    }

    private void SendMoveToSrv()
    {
        // 构造你的 Proto 消息
        C2S_Move msg = new C2S_Move
        {
            Pos = new Vec3 { X = transform.position.x, Y = transform.position.y, Z = transform.position.z },
            RotY = transform.eulerAngles.y
        };

        // 通过你的 TcpClientManager 发出去
        // 假设你的单例叫 Instance，发送方法叫 Send
        if (TcpClientManager.Instance != null && TcpClientManager.Instance.IsConnected)
        {
            byte[] data = Google.Protobuf.MessageExtensions.ToByteArray(msg);
            Debug.Log($"[Network] 正在发送位置包: X={msg.Pos.X}, Z={msg.Pos.Z}, 长度={data.Length}");
            TcpClientManager.Instance.Send(data);

        }
    }
}