using System.Collections.Generic;
using UnityEngine;
using Youdu; // 确保这是你的 Proto 命名空间

public class RoomManager : MonoBehaviour
{
    [Header("替身预制体")]
    public GameObject remotePlayerPrefab;

    // 存放房间里其他玩家的字典：实体ID -> 游戏物体
    private Dictionary<int, GameObject> _remotePlayers = new Dictionary<int, GameObject>();

    private void Start()
    {
        // 监听网络层的广播事件
        if (TcpClientManager.Instance != null)
        {
            TcpClientManager.Instance.OnMoveBroadcastReceived += OnMoveBroadcast;
        }
    }

    private void OnDestroy()
    {
        // 移除监听，防止切场景时报错
        if (TcpClientManager.Instance != null)
        {
            TcpClientManager.Instance.OnMoveBroadcastReceived -= OnMoveBroadcast;
        }
    }

    // 当收到服务器下发的移动广播时触发
    private void OnMoveBroadcast(S2C_MoveBroadcast msg)
    {
        // 【防覆盖检查】：如果广播的是自己，直接忽略（因为本地玩家已经自己移动了）
        if (msg.EntityId == TcpClientManager.Instance.LocalEntityId)
        {
            return;
        }

        // 解析 Protobuf 里的坐标和旋转
        Vector3 targetPos = new Vector3(msg.Pos.X, msg.Pos.Y, msg.Pos.Z);
        Quaternion targetRot = Quaternion.Euler(0, msg.RotY, 0);

        // 检查字典：这个玩家是不是已经在场景里了？
        if (_remotePlayers.TryGetValue(msg.EntityId, out GameObject playerObj))
        {
            // 如果在，获取他身上的 RemotePlayer 脚本，并设置目标点
            RemotePlayer rp = playerObj.GetComponent<RemotePlayer>();
            if (rp != null)
            {
                rp.SetTargetPosition(targetPos, msg.RotY);
            }
        }
        else
        {
            // 如果不在，说明是刚进视野/刚进房间的新玩家，生成他！
            if (remotePlayerPrefab != null)
            {
                GameObject newPlayer = Instantiate(remotePlayerPrefab, targetPos, targetRot);
                newPlayer.name = "RemotePlayer_" + msg.EntityId;
                _remotePlayers.Add(msg.EntityId, newPlayer);

                Debug.Log($"<color=yellow>[RoomManager] 视野内生成了新玩家: {msg.EntityId}</color>");
            }
            else
            {
                Debug.LogError("[RoomManager] 替身预制体没有赋值！请在 Inspector 面板把 Prefab 拖进去！");
            }
        }
    }
}