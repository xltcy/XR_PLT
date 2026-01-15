using System;

namespace TickSystem
{
    /// <summary>
    /// 实现此接口的类可以自定义Tick间隔时间
    /// </summary>
    public interface ITickerInterval
    {
        /// <summary>
        /// 按照TickInterval指定的间隔调用
        /// </summary>
        void TickInterval();
        
        /// <summary>
        /// Tick间隔时间（秒）
        /// </summary>
        float TickIntervalTime { get; }
    }
}
