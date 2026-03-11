using UnityEngine;

public class NetStartTest : MonoBehaviour
{
    void Start()
    {
        // 优先使用这个 192 开头的地址
        //string skynetIP = "192.168.101.145";
        string skynetIP = "127.0.0.1";
        int skynetPort = 8888;

        Debug.Log("--- 正在通过局域网 IP 连接 WSL Skynet ---");
        TcpClientManager.Instance.Connect(skynetIP, skynetPort);
    }
}