namespace Network.RequestParam
{
    /// <summary>
    /// 获取单一场景具体数据
    /// </summary>
    public static class GetSceneData
    {
        public class RequestParam : BaseRequestParam
        {
            public RequestParam(SummaryItemData sceneItemData)
            {
                localData = sceneItemData;
                url = ManagerRefer.NetworkServiceManager.BuildUrl(NetworkUtil.GET_SCENE_CONFIG_INTERFACE);
                queryParams.Add("sceneKey", sceneItemData.sceneKey);
                networkConstant = NetworkConstant.SCENE_DATA;
            }
        }
    }
}