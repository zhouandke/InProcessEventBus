namespace InProcessEventBus
{
    /// <summary>
    /// 实现类需要什么东西(包括 IServiceProvider), 就在构造函数中添加
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class EventHandle<T>
        where T : EventData
    {
        public abstract void Handle(T eventData);
    }
}