using UnityEngine;
using Youdu; // 确保与你的 Proto 生成类一致

public class PlayerController : MonoBehaviour
{
    private CharacterController _controller;

    [Header("移动设置")]
    [SerializeField] private float _moveSpeed = 5.0f;
    [SerializeField] private float _gravity = -9.81f;
    private Vector3 _velocity; // 处理重力速度

    [Header("同步设置")]
    private float _lastSendTime = 0;
    private float _sendInterval = 0.1f;
    private Vector3 _lastSentPos; // 记录上次发送的位置，用于减负

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _lastSentPos = transform.position;

        if (TcpClientManager.Instance != null)
        {
            TcpClientManager.Instance.OnLoginResultReceived += OnLoginResult;
        }
    }

    private void OnDestroy()
    {
        if (TcpClientManager.Instance != null)
            TcpClientManager.Instance.OnLoginResultReceived -= OnLoginResult;
    }

    void Update()
    {
        HandleMovement();

        // 定时发送逻辑
        if (Time.time - _lastSendTime > _sendInterval)
        {
            // 只有当位置发生显著变化时才上报 (节省流量)
            if (Vector3.Distance(transform.position, _lastSentPos) > 0.01f)
            {
                SendMoveToSrv();
                _lastSentPos = transform.position;
            }
            _lastSendTime = Time.time;
        }
    }

    private void HandleMovement()
    {
        // 1. 处理重力（确保小人贴地）
        if (_controller.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        // 2. 获取输入并移动
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized; // 归一化防止斜向移动过快

        if (inputDir.magnitude > 0.1f)
        {
            // 移动
            _controller.Move(inputDir * _moveSpeed * Time.deltaTime);
            // 转向：面向移动方向
            transform.forward = Vector3.Slerp(transform.forward, inputDir, Time.deltaTime * 10f);
        }

        // 应用重力
        _velocity.y += _gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void OnLoginResult(S2C_LoginResult res)
    {
        // 禁用 Controller 才能直接改 Position，否则会被物理系统拉回
        _controller.enabled = false;
        transform.position = new Vector3(res.SpawnPos.X, res.SpawnPos.Y, res.SpawnPos.Z);
        _controller.enabled = true;

        Debug.Log($"[PlayerController] 设置出生点到 {transform.position}");
    }

    private void SendMoveToSrv()
    {
        if (TcpClientManager.Instance == null || !TcpClientManager.Instance.IsConnected) return;

        // 构造 Proto 消息
        C2S_Move msg = new C2S_Move
        {
            Pos = new Vec3 { X = transform.position.x, Y = transform.position.y, Z = transform.position.z },
            RotY = transform.eulerAngles.y
        };

        // 序列化
        byte[] body = Google.Protobuf.MessageExtensions.ToByteArray(msg);

        // 【关键改动】：在这里打印日志，确认发送的内容
        Debug.Log($"[Network] 发送位置: ({msg.Pos.X:F2}, {msg.Pos.Z:F2})，长度: {body.Length}");

        // 注意：建议在 TcpClientManager.Send 内部统一添加 2 字节长度头
        TcpClientManager.Instance.Send(body);
    }
}