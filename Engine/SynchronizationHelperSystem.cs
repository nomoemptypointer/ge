namespace Engine
{
    public class SynchronizationHelperSystem : GameSystem
    {
        private Queue<Action> _activeQueue = new();
        private Queue<Action> _bufferedQueue = new();

        public void QueueMainThreadAction(Action a)
        {
            _activeQueue.Enqueue(a);
        }

        protected override void UpdateCore(float deltaSeconds)
        {
            Queue<Action> queue = Interlocked.Exchange(ref _activeQueue, _bufferedQueue);
            while (queue.Count > 0)
            {
                queue.Dequeue()();
            }
        }
    }
}
