using UnityEngine;
using Youdu; // ������� proto ��� package ����һ��

public class PlayerController : MonoBehaviour
{
    private CharacterController _controller;
    private float _moveSpeed = 5.0f;

    // ����Ƶ�ʿ���
    private float _lastSendTime = 0;
    private float _sendInterval = 0.1f; // 100ms ��һ�ΰ�

    void Start()
    {
        _controller = GetComponent<CharacterController>();

        // ��ѡ�����ݵ�¼������ó�����
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
        // 1. �����ƶ��߼�
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 dir = new Vector3(h, 0, v);

        if (dir.magnitude > 0.1f)
        {
            _controller.Move(dir * _moveSpeed * Time.deltaTime);
        }

        // 2. ��ʱ����ÿ 0.1s �������ͬ��һ��λ��
        if (Time.time - _lastSendTime > _sendInterval)
        {
            SendMoveToSrv();
            _lastSendTime = Time.time;
        }
    }

    private void OnLoginResult(S2C_LoginResult res)
    {
        // �ѱ����������������ָ��������
        transform.position = new Vector3(res.SpawnPos.X, res.SpawnPos.Y, res.SpawnPos.Z);
        Debug.Log($"[PlayerController] ���ó����㵽 ({res.SpawnPos.X},{res.SpawnPos.Y},{res.SpawnPos.Z})");
    }

    private void SendMoveToSrv()
    {
        // ������� Proto ��Ϣ
        C2S_Move msg = new C2S_Move
        {
            Pos = new Vec3 { X = transform.position.x, Y = transform.position.y, Z = transform.position.z },
            RotY = transform.eulerAngles.y
        };

        // ͨ����� TcpClientManager ����ȥ
        // ������ĵ����� Instance�����ͷ����� Send
        if (TcpClientManager.Instance != null && TcpClientManager.Instance.IsConnected)
        {
            byte[] data = Google.Protobuf.MessageExtensions.ToByteArray(msg);
            Debug.Log($"[Network] ���ڷ���λ�ð�: X={msg.Pos.X}, Z={msg.Pos.Z}, ����={data.Length}");
            TcpClientManager.Instance.Send(data);

        }
    }
}