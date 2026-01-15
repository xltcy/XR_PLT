namespace TickSystem
{
    /// <summary>
    /// 可选接口：设置Tick执行优先级
    /// </summary>
    public interface ITickerPriority
    {
        /// <summary>
        /// Tick执行优先级，数值越小越先执行
        /// </summary>
        int TickPriority { get; }
    }
}