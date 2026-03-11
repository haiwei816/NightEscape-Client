using System.Collections.Generic;
using UnityEngine;
using Youdu;

public class NetworkEntityManager : MonoBehaviour
{
    public static NetworkEntityManager Instance;

    [Header("远程玩家预制体")]
    public GameObject remotePlayerPrefab;

    // entityId -> Transform
    private readonly Dictionary<int, Transform> _entities = new Dictionary<int, Transform>();
    private bool _bound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        TryBind();
    }

    private void Start()
    {
        // 兜底：如果 TcpClientManager 稍后才初始化，这里再尝试一次
        TryBind();
    }

    private void OnDestroy()
    {
        if (TcpClientManager.Instance != null)
        {
            TcpClientManager.Instance.OnMoveBroadcastReceived -= OnMoveBroadcast;
        }
    }

    private void TryBind()
    {
        if (_bound) return;
        if (TcpClientManager.Instance == null) return;
        TcpClientManager.Instance.OnMoveBroadcastReceived += OnMoveBroadcast;
        _bound = true;
    }

    private void OnMoveBroadcast(S2C_MoveBroadcast msg)
    {
        if (remotePlayerPrefab == null)
        {
            Debug.LogWarning("[NetworkEntityManager] remotePlayerPrefab 未设置，无法实例化远程玩家。");
            return;
        }

        // 不为本机实体创建远程影子
        if (TcpClientManager.Instance != null && msg.EntityId == TcpClientManager.Instance.LocalEntityId)
        {
            return;
        }

        if (!_entities.TryGetValue(msg.EntityId, out var t))
        {
            var go = Instantiate(remotePlayerPrefab);
            go.name = $"RemotePlayer_{msg.EntityId}";
            t = go.transform;
            _entities[msg.EntityId] = t;
        }

        t.position = new Vector3(msg.Pos.X, msg.Pos.Y, msg.Pos.Z);
        t.rotation = Quaternion.Euler(0f, msg.RotY, 0f);
    }
}

