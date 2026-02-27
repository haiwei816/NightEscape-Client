using UnityEngine;
using System;
using System.Net.Sockets;
using System.Collections.Generic;

public class TcpClientManager : MonoBehaviour
{
    // --- 第一部分：基础定义与单例 ---
    public static TcpClientManager Instance;
    private TcpClient _client;
    private NetworkStream _stream;
    private byte[] _readBuffer = new byte[8192];
    private List<byte> _cacheBuffer = new List<byte>();

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
        // 暴力调试：不管三七二十一，先打印原始收到的字节数
        Debug.Log($"<color=white>[网络调试] 收到原始数据，长度: {temp.Length}</color>");

        lock (_cacheBuffer)
        {
            _cacheBuffer.AddRange(temp);

            while (_cacheBuffer.Count >= 2)
            {
                byte[] lenBytes = _cacheBuffer.GetRange(0, 2).ToArray();

                // 看看这前两个字节到底是什么
                // Debug.Log($"[网络调试] 长度头字节: {lenBytes[0]} , {lenBytes[1]}");

                if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
                ushort bodyLen = BitConverter.ToUInt16(lenBytes, 0);

                if (_cacheBuffer.Count >= 2 + bodyLen)
                {
                    byte[] body = _cacheBuffer.GetRange(2, bodyLen).ToArray();
                    _cacheBuffer.RemoveRange(0, 2 + bodyLen);

                    string msg = System.Text.Encoding.UTF8.GetString(body);
                    Debug.Log($"<color=green>【恭喜】解包成功: {msg}</color>");
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
}