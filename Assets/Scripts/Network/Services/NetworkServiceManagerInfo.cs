// 挂在节点上，查看NetworkServiceManager状态
// 调试用

using UnityEngine;

public class NetworkServiceManagerInfo : MonoBehaviour
{
    public NetworkServiceManager Manager => ManagerRefer.NetworkServiceManager;
}