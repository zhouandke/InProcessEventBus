namespace InProcessEventBus
{
    using System;
    public abstract class EventData
    {
        public DateTime EventTime { get; set; }

        public string TargetClassName { get; set; }
    }
}