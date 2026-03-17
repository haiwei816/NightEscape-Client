using UnityEngine;
using UnityEngine.UI;
using Youdu;

public class MatchUIHandler : MonoBehaviour
{
    [Header("UI 绑定")]
    [SerializeField] private Button _startMatchButton;
    [SerializeField] private Text _matchCountText;
    [SerializeField] private GameObject _uiRoot;

    [Header("匹配参数")]
    [Tooltip("1: 阳派(逃生者)  2: 阴派(守卫)")]
    [SerializeField] private int _camp = 1;

    [Header("协议类型号 (msgType)")]
    [Tooltip("C2S_StartMatch 的 msgType（需与服务端一致）")]
    [SerializeField] private byte _msgTypeStartMatch = 3;

    [Header("开局控制")]
    [SerializeField] private PlayerController _playerController;

    private void Awake()
    {
        if (_uiRoot == null) _uiRoot = gameObject;
    }

    private void OnEnable()
    {
        if (_startMatchButton != null)
            _startMatchButton.onClick.AddListener(OnClickStartMatch);

        if (TcpClientManager.Instance != null)
        {
            TcpClientManager.Instance.OnMatchStatusReceived += OnMatchStatus;
            TcpClientManager.Instance.OnGameStartReceived += OnGameStart;
        }
    }

    private void OnDisable()
    {
        if (_startMatchButton != null)
            _startMatchButton.onClick.RemoveListener(OnClickStartMatch);

        if (TcpClientManager.Instance != null)
        {
            TcpClientManager.Instance.OnMatchStatusReceived -= OnMatchStatus;
            TcpClientManager.Instance.OnGameStartReceived -= OnGameStart;
        }
    }

    private void Start()
    {
        RefreshMatchText(null);
        UpdateButtonInteractable();
    }

    private void UpdateButtonInteractable()
    {
        if (_startMatchButton == null) return;
        _startMatchButton.interactable = TcpClientManager.Instance != null && TcpClientManager.Instance.IsConnected;
    }

    private void OnClickStartMatch()
    {
        if (TcpClientManager.Instance == null || !TcpClientManager.Instance.IsConnected)
        {
            RefreshMatchText("未连接服务器");
            UpdateButtonInteractable();
            return;
        }

        var req = new C2S_StartMatch { Camp = _camp };
        TcpClientManager.Instance.SendTyped(_msgTypeStartMatch, req);
        RefreshMatchText("正在匹配...");
    }

    private void OnMatchStatus(S2C_MatchStatus status)
    {
        if (status == null) return;
        if (_matchCountText == null) return;

        string msg = $"{status.CurrentPlayers}/{status.TotalRequired}";
        if (!string.IsNullOrEmpty(status.StatusMsg))
            msg += $"  {status.StatusMsg}";

        _matchCountText.text = msg;
    }

    private void OnGameStart(S2C_GameStart start)
    {
        if (_uiRoot != null) _uiRoot.SetActive(false);

        if (_playerController != null)
        {
            _playerController.gameObject.SetActive(true);
            _playerController.enabled = true;
        }
    }

    private void RefreshMatchText(string fallback)
    {
        if (_matchCountText == null) return;
        _matchCountText.text = string.IsNullOrEmpty(fallback) ? "未开始匹配" : fallback;
    }
}

