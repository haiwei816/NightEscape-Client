using UnityEngine;
using UnityEngine.UI;
using TMPro; // 必须引入 TMP 命名空间

public class MatchUIHandler : MonoBehaviour
{
    [Header("UI 控件")]
    public Button startMatchButton;
    public TextMeshProUGUI matchCountText; // 这里改成了 TMP 的组件类型
    public GameObject uiRoot;

    [Header("角色控制")]
    public PlayerController playerController;

    private void Start()
    {
        // 游戏一开始，先把玩家的移动控制关掉
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // 给按钮绑定点击事件
        if (startMatchButton != null)
        {
            startMatchButton.onClick.AddListener(OnStartBtnClicked);
        }
    }

    private void OnStartBtnClicked()
    {
        Debug.Log(">>> 玩家点击了匹配按钮！<<<");

        // 让按钮变灰，防止玩家狂点
        startMatchButton.interactable = false;

        // 改变屏幕上的文字
        if (matchCountText != null)
        {
            matchCountText.text = "正在搜寻幽都魂魄... (1/5)";
        }
    }
}