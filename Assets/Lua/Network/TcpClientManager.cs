using UnityEngine;
using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Youdu;

public class TcpClientManager : MonoBehaviour
{
    // --- 第一部分：基础定义与单例 ---
    public static TcpClientManager Instance;

    // 服务器广播玩家移动时的回调
    public Action<S2C_MoveBroadcast> OnMoveBroadcastReceived;
    // 登录结果回调
    public Action<S2C_LoginResult> OnLoginResultReceived;

    // 本机在服务器上的实体 ID
    public int LocalEntityId { get; private set; }
    private TcpClient _client;
    private NetworkStream _stream;
    private byte[] _readBuffer = new byte[8192];
    private List<byte> _cacheBuffer = new List<byte>();

    // 网络回调不在 Unity 主线程：把消息投递到主线程再触发事件
    private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

    private void Update()
    {
        while (_mainThreadActions.TryDequeue(out var act))
        {
            try
            {
                act?.Invoke();
            }
            catch (Exception e)
            {
            Debug.LogWarning("[TCP] 接收线程关闭: " + e.Message);
            }
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 保证切场景不断开
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- 第二部分：连接与接收逻辑 ---
    public void Connect(string ip, int port)
    {
        try
        {
            _client = new TcpClient();
            _client.BeginConnect(ip, port, (ar) => {
                try
                {
                    _client.EndConnect(ar);
                    _stream = _client.GetStream();
                    Debug.Log("<color=green>[TCP] 连接成功！</color>");
                    // 开启异步读取
                    _stream.BeginRead(_readBuffer, 0, _readBuffer.Length, OnReceive, null);
                }
                catch (Exception e)
                {
                    Debug.LogError("[TCP] 建立流失败: " + e.Message);
                }
            }, null);
        }
        catch (Exception e)
        {
            Debug.LogError("[TCP] 发起连接失败: " + e.Message);
        }
    }

    private void OnReceive(IAsyncResult ar)
    {
        try
        {
            int count = _stream.EndRead(ar);
            if (count <= 0) return;

            byte[] temp = new byte[count];
            Buffer.BlockCopy(_readBuffer, 0, temp, 0, count);

            // 进入解包逻辑
            ProcessBuffer(temp);

            // 持续读取
            _stream.BeginRead(_readBuffer, 0, _readBuffer.Length, OnReceive, null);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TCP] 接收线程关闭: " + e.Message);
        }
    }

    // --- 第三部分：解包与发送 ---
    private void ProcessBuffer(byte[] temp)
    {
        lock (_cacheBuffer)
        {
            _cacheBuffer.AddRange(temp);

            while (_cacheBuffer.Count >= 2)
            {
                byte[] lenBytes = _cacheBuffer.GetRange(0, 2).ToArray();

                if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
                ushort bodyLen = BitConverter.ToUInt16(lenBytes, 0);

                if (_cacheBuffer.Count >= 2 + bodyLen)
                {
                    byte[] body = _cacheBuffer.GetRange(2, bodyLen).ToArray();
                    _cacheBuffer.RemoveRange(0, 2 + bodyLen);

                    if (body.Length < 1)
                    {
                        Debug.LogWarning("[TCP] 收到空包体，丢弃");
                        continue;
                    }

                    byte msgType = body[0];
                    byte[] pbBytes = new byte[body.Length - 1];
                    Buffer.BlockCopy(body, 1, pbBytes, 0, pbBytes.Length);

                    try
                    {
                        switch (msgType)
                        {
                            case 1: // 登录结果
                                var login = S2C_LoginResult.Parser.ParseFrom(pbBytes);
                                Debug.Log($"<color=cyan>[S2C_LoginResult] entity={login.EntityId}, spawn=({login.SpawnPos.X},{login.SpawnPos.Y},{login.SpawnPos.Z}), msg={login.WelcomeMsg}</color>");
                                _mainThreadActions.Enqueue(() =>
                                {
                                    LocalEntityId = login.EntityId;
                                    OnLoginResultReceived?.Invoke(login);
                                });
                                break;
                            case 2: // 移动广播
                                var moveMsg = S2C_MoveBroadcast.Parser.ParseFrom(pbBytes);
                                Debug.Log($"<color=green>[S2C_MoveBroadcast] entity={moveMsg.EntityId}, pos=({moveMsg.Pos.X:F2},{moveMsg.Pos.Y:F2},{moveMsg.Pos.Z:F2}), rotY={moveMsg.RotY:F1}</color>");
                                _mainThreadActions.Enqueue(() =>
                                {
                                    OnMoveBroadcastReceived?.Invoke(moveMsg);
                                });
                                break;
                            default:
                                Debug.LogWarning($"[TCP] 未知消息类型: {msgType}");
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[TCP] 反序列化消息失败, type={msgType}, err={e.Message}");
                    }
                }
                else
                {
                    // 如果进到这里，说明长度头读到了，但数据包还没传完
                    Debug.Log($"[网络调试] 正在等待包体... 当前缓冲区: {_cacheBuffer.Count} / 目标: {2 + bodyLen}");
                    break;
                }
            }
        }
    }

    public void Send(byte[] data)
    {
        if (_client == null || !_client.Connected) return;
        try
        {
            ushort len = (ushort)data.Length;
            byte[] lenBytes = BitConverter.GetBytes(len);
            if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);

            byte[] full = new byte[2 + data.Length];
            Buffer.BlockCopy(lenBytes, 0, full, 0, 2);
            Buffer.BlockCopy(data, 0, full, 2, data.Length);

            _stream.Write(full, 0, full.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("[TCP] 发送异常: " + e.Message);
        }
    }

    public bool IsConnected
    {
        get { return _client != null && _client.Connected; }
    }
}