using System;

namespace TickSystem
{
    // 尽量不使用Update的tick
    
    /// <summary>
    /// 实现此接口的类会自动注册到Tick系统中
    /// 支持MonoBehaviour和纯C#类
    /// </summary>
    public interface ITickerUpdate
    {
        /// <summary>
        /// 每帧调用一次
        /// </summary>
        void Tick();
    }
}
