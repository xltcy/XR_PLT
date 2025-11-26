using System;
using System.Collections.Generic;

[Serializable]
public class SummaryData
{
    // maybe version control
    public List<SummaryItemData> items;
}

[Serializable]
public class SummaryItemData
{
    // sceneName
    public string sceneName;
    // for scene json/model get api
    public string sceneKey;
    //场景重定位算法，临时方案
    public string sceneRelocateAlgo;
}
