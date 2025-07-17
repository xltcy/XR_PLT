using System;
using System.Collections.Generic;

[Serializable]
public class SummaryData
{
    // maybe version control
    public long timestampMs;
    public List<SummaryItemData> items;
}

[Serializable]
public class SummaryItemData
{
    // sceneName
    public string sceneName;
    // for scene json/model get api
    public string sceneKey;
}
