public class UIMediatorRegisterData
{
    public string name;
    public string prefab;
}

public class UIMediatorDataCenter
{
    public static UIMediatorRegisterData DebugUIMediator = new UIMediatorRegisterData
    {
        name = "DebugUIMediator",
        prefab = "ui_debug",
    };
    
    
}