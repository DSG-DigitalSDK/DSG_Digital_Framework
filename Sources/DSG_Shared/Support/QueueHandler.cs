using DSG.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Shared
{
    /// <summary>
    /// Manages a queue of objects
    /// <para>Thread safe</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueHandler<T> where T : class
    {
        static readonly string sC = nameof(QueueHandler<T>);

        ConcurrentQueue<T> oQueueData = new ConcurrentQueue<T>();

        /// <summary>
        /// Raised when queue full after insertion operation
        /// </summary>
        public event EventHandler? OnQueueFull;

        /// <summary>
        /// Maximum objects in queue
        /// </summary>
        public int MaxQueueSize { get; set; } = 100;

        /// <summary>
        /// Queue full flag
        /// </summary>
        public bool QueueFull => (oQueueData?.Count ?? 0) >= MaxQueueSize;

        /// <summary>
        /// Queue empty flag
        /// </summary>
        public bool QueueEmpty => oQueueData?.IsEmpty ?? false;

        /// <summary>
        /// Elements in queue
        /// </summary>
        public int Count => oQueueData?.Count ?? 0;

        /// <summary>
        /// Create new queue instance
        /// </summary>
        /// <returns>true when created, false otherwise</returns>
        public bool CreateQueue()
        {
            string sM = nameof(CreateQueue);
            try
            {
                DestroyQueue();
                return true;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, ex);
                return false;
            }
        }

        /// <summary>
        /// Destroy queue 
        /// </summary>
        /// <returns></returns>
        public bool DestroyQueue()
        {
            string sM = nameof(DestroyQueue);
            //oQueueData?.Clear();
            oQueueData = new ConcurrentQueue<T>();
            return true;
        }

        /// <summary>
        /// Peek first object in queue
        /// </summary>
        /// <returns>object or null on empty queue</returns>
        public T? Peek()
        {
            if (oQueueData.TryPeek(out T? result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// Dequeue object
        /// </summary>
        /// <returns>object or null on empty queue</returns>
        public T? Dequeue()
        {
            if (oQueueData.TryDequeue(out T? result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// Enquueue object
        /// </summary>
        /// <param name="oData">object to enqueue</param>
        /// <returns>true when successful, false otherwise</returns>
        public bool Enqueue(T? oData)
        {
            string sM = nameof(Enqueue);
            if (oData == null)
            {
                LogMan.Error(sC, sM, $"NULL data");
                return false;
            }
            if (oQueueData == null)
            {
                LogMan.Error(sC, sM, $"NULL queue");
                return false;
            }
            if (QueueFull)
            {
                OnQueueFull?.Invoke(this, EventArgs.Empty);
                LogMan.Error(sC, sM, $"Queue FULL");
                return false;
            }
            oQueueData.Enqueue(oData);
            return true;
        }

        /// <summary>
        /// Removes all elements in the queue
        /// </summary>
        public void Clear()
        {
            oQueueData = new ConcurrentQueue<T>();
        }

    }
}
