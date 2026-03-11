using UnityEngine;
using Youdu; // 注意：需要和 proto 的 package 保持一致

public class PlayerController : MonoBehaviour
{
    private CharacterController _controller;
    private float _moveSpeed = 5.0f;

    // 发送频率控制
    private float _lastSendTime = 0;
    private float _sendInterval = 0.1f; // 100ms 发送一次包

    void Start()
    {
        _controller = GetComponent<CharacterController>();

        // 可选：接收登录结果，设置出生点
        if (TcpClientManager.Instance != null)
        {
            TcpClientManager.Instance.OnLoginResultReceived += OnLoginResult;
        }
    }

    private void OnDestroy()
    {
        if (TcpClientManager.Instance != null)
        {
            TcpClientManager.Instance.OnLoginResultReceived -= OnLoginResult;
        }
    }

    void Update()
    {
        // 1. 本地移动逻辑
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 dir = new Vector3(h, 0, v);

        if (dir.magnitude > 0.1f)
        {
            _controller.Move(dir * _moveSpeed * Time.deltaTime);
        }

        // 2. 定时发送：每 0.1s 同步一次位置
        if (Time.time - _lastSendTime > _sendInterval)
        {
            SendMoveToSrv();
            _lastSendTime = Time.time;
        }
    }

    private void OnLoginResult(S2C_LoginResult res)
    {
        // 设置本地玩家出生点到服务器指定位置
        transform.position = new Vector3(res.SpawnPos.X, res.SpawnPos.Y, res.SpawnPos.Z);
        Debug.Log($"[PlayerController] 设置出生点到 ({res.SpawnPos.X},{res.SpawnPos.Y},{res.SpawnPos.Z})");
    }

    private void SendMoveToSrv()
    {
        // 构造要发送的 Proto 消息
        C2S_Move msg = new C2S_Move
        {
            Pos = new Vec3 { X = transform.position.x, Y = transform.position.y, Z = transform.position.z },
            RotY = transform.eulerAngles.y
        };

        // 通过 TcpClientManager 发送
        if (TcpClientManager.Instance != null && TcpClientManager.Instance.IsConnected)
        {
            byte[] data = Google.Protobuf.MessageExtensions.ToByteArray(msg);
            Debug.Log($"[Network] 正在发送位置包: X={msg.Pos.X}, Z={msg.Pos.Z}, bytes={data.Length}");
            TcpClientManager.Instance.Send(data);

        }
    }
}