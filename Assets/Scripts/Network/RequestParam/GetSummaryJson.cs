namespace Network.RequestParam
{
    /// <summary>
    /// 获取场景SummaryJson
    /// </summary>
    public static class GetSummaryJson
    {
        public class RequestParam : BaseRequestParam
        {
            public RequestParam()
            {
                url = ManagerRefer.NetworkServiceManager.BuildUrl(NetworkUtil.GET_SCENE_LIST_INTERFACE);
                networkConstant = NetworkConstant.SUMMARY_JSON;
            }
        }
    }
}