using System;

namespace TickSystem
{
    /// <summary>
    /// 实现此接口的类会每0.5秒调用一次Tick
    /// </summary>
    public interface ITickerHalfSecond
    {
        /// <summary>
        /// 每0.5秒调用一次
        /// </summary>
        void TickHalfSecond();
    }
}
