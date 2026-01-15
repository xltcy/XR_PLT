using System;

namespace TickSystem
{
    /// <summary>
    /// 实现此接口的类会在FixedUpdate中调用（用于物理相关逻辑）
    /// </summary>
    public interface ITickerFixedUpdate
    {
        /// <summary>
        /// 在FixedUpdate中调用
        /// </summary>
        void TickFixed();
    }
}
