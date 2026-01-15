namespace TickSystem
{
    /// <summary>
    /// 可选接口：控制Tick是否启用
    /// </summary>
    public interface ITickerEnabled
    {
        /// <summary>
        /// 是否启用Tick
        /// </summary>
        bool IsTickEnabled { get; }
    }
}