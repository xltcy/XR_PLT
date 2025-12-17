using UnityEngine;

public class EventManagerExample : MonoBehaviour
{
    private string _playerDamageListenerId;
    private string _uiUpdateListenerId;

    private void Start()
    {
        // 1. 基本事件监听
        EventManager.Instance.AddListener("Player.Damage", OnPlayerDamaged);
        
        // 2. 泛型事件监听
        EventManager.Instance.AddListener<PlayerData>("Player.Update", OnPlayerUpdated);
        
        // 3. 带优先级和选项的监听器
        _playerDamageListenerId = EventManager.Instance.AddListener(
            "Player.Damage.Critical",
            OnCriticalDamage,
            EventPriority.Critical,
            EventOptions.Once); // 只执行一次
        
        // 4. 为对象添加监听器（自动清理）
        this.AddEventListener("UI.Button.Click", OnButtonClick);
        
        // 5. 分发事件
        EventManager.Instance.Dispatch("Game.Start", this, new { Level = 1 });
        
        // 6. 分发泛型事件
        var playerData = new PlayerData { Health = 100, Score = 0 };
        EventManager.Instance.Dispatch("Player.Spawn", playerData, this);
        
        // 7. 使用扩展方法分发事件
        this.TriggerEvent("Enemy.Destroyed", new { EnemyType = "Goblin", Score = 100 });
        
        // 8. 异步分发事件
        EventManager.Instance.DispatchAsync("Background.Task", this, new { Task = "Processing" });
        
        // 9. 等待事件（协程）
        StartCoroutine(WaitForLevelComplete());
        
        // 10. 系统消息
        EventManager.Instance.SendSystemMessage("游戏初始化完成");
    }

    private void OnPlayerDamaged(EventData evt)
    {
        var damageData = evt.GetData<DamageData>();
        Debug.Log($"玩家受到 {damageData.Amount} 点伤害，来源: {damageData.Source}");
    }

    private void OnPlayerUpdated(EventData<PlayerData> evt)
    {
        Debug.Log($"玩家数据更新 - 生命: {evt.Data.Health}, 分数: {evt.Data.Score}");
    }

    private void OnCriticalDamage(EventData evt)
    {
        Debug.Log("致命伤害事件触发！");
    }

    private void OnButtonClick(EventData evt)
    {
        Debug.Log($"按钮点击: {evt.GetData<string>()}");
    }

    private System.Collections.IEnumerator WaitForLevelComplete()
    {
        yield return EventManager.Instance.WaitForEvent("Level.Complete", evt =>
        {
            Debug.Log("关卡完成事件触发！");
        });
        
        Debug.Log("继续执行后续逻辑...");
    }

    private void OnDestroy()
    {
        // 清理监听器
        EventManager.Instance.RemoveListener("Player.Damage", OnPlayerDamaged);
        
        // 通过ID移除
        EventManager.Instance.RemoveListenerById(_playerDamageListenerId);
        
        // 移除对象所有监听器
        this.RemoveAllEventListener();
        
        // 打印状态
        EventManager.Instance.PrintStatus();
    }

    // 示例数据类
    public class PlayerData
    {
        public int Health;
        public int Score;
        public string Name;
    }

    public class DamageData
    {
        public int Amount;
        public string Source;
    }
}