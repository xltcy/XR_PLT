using System;

namespace TickSystem
{
    /// <summary>
    /// 实现此接口的类会每秒调用一次Tick
    /// </summary>
    public interface ITickerSecond
    {
        /// <summary>
        /// 每秒调用一次
        /// </summary>
        void TickSecond();
    }
}
