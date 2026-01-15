using System;

namespace TickSystem
{
    /// <summary>
    /// 实现此接口的类会在LateUpdate中调用（用于相机跟随等逻辑）
    /// </summary>
    public interface ITickerLateUpdate
    {
        /// <summary>
        /// 在LateUpdate中调用
        /// </summary>
        void TickLate();
    }
}
