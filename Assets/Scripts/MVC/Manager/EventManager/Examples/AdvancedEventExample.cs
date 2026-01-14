using System.Collections.Generic;
using UnityEngine;

// 事件系统的高级使用
public class AdvancedEventExample : MonoBehaviour
{
    private void Start()
    {
        // 1. 事件链
        ManagerRefer.EventManager.AddListener("Order.Placed", OnOrderPlaced);
        ManagerRefer.EventManager.AddListener("Payment.Processed", OnPaymentProcessed);
        ManagerRefer.EventManager.AddListener("Shipping.Scheduled", OnShippingScheduled);
        
        // 2. 事件元数据
        ManagerRefer.EventManager.AddListener("Analytics.Event", evt =>
        {
            // 获取元数据
            string userId = evt.GetMetadata<string>("UserId", "anonymous");
            string sessionId = evt.GetMetadata<string>("SessionId");
            
            Debug.Log($"分析事件: {evt.EventName}, 用户: {userId}");
        });
        
        // 3. 条件事件处理
        ManagerRefer.EventManager.AddListener("Game.State.Changed", evt =>
        {
            var stateData = evt.GetData<GameStateData>();
            
            if (stateData.NewState == GameState.Paused && stateData.OldState == GameState.Playing)
            {
                // 处理暂停逻辑
                Debug.Log("游戏暂停");
                
                // 消费事件，阻止其他监听器处理
                evt.Consume();
            }
        });
        
        // 4. 事件广播模式
        ManagerRefer.EventManager.AddListener("Broadcast.*", evt =>
        {
            Debug.Log($"收到广播: {evt.EventName}");
        });
        
        // 5. 性能监控
        ManagerRefer.EventManager.OnEventDispatched += (eventName, eventData) =>
        {
            Debug.Log($"事件分发: {eventName} at {eventData.Timestamp}");
        };
    }
    
    private void OnOrderPlaced(EventData evt)
    {
        var order = evt.GetData<OrderData>();
        Debug.Log($"订单已下: {order.Id}");
        
        // 触发下一个事件
        ManagerRefer.EventManager.Dispatch("Payment.Processed", this, order);
    }
    
    private void OnPaymentProcessed(EventData evt)
    {
        Debug.Log("支付已处理");
        ManagerRefer.EventManager.Dispatch("Shipping.Scheduled", this, evt.Data);
    }
    
    private void OnShippingScheduled(EventData evt)
    {
        Debug.Log("配送已安排");
    }
    
    // 示例数据类
    public class OrderData
    {
        public string Id;
        public List<string> Items;
        public float Total;
    }
    
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }
    
    public class GameStateData
    {
        public GameState OldState;
        public GameState NewState;
    }
}