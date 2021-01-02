﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XCBatch.Interfaces;
using XCBatch.Interfaces.Adapters;

namespace XCBatch.Core
{
    /// <summary>
    /// Parallel Queue client featuring a dual queue system for handling network delay and retry to
    /// a secondary queue that may be remote or slower than a memory queue.
    /// </summary>
    /// <remarks>
    /// <para></para>
    /// </remarks>
    public class BufferedQueueFrontend : ParallelQueueFrontend
    {
        /// <summary>
        /// fast thread safe queue
        /// </summary>
        protected BlockingCollection<ISource>[] bufferQueue;

        protected List<Thread> flushThreads;

        protected int timeout;

        /// <summary>
        /// construct fronted with a fast bufferQueue and a slower queue
        /// </summary>
        /// <param name="backendQueue">thread safe queue adaptor</param>
        /// <param name="maxEnqueue"></param>
        /// <param name="timeoutSeconds"></param>
        /// <param name="collectionNodes"></param>
        public BufferedQueueFrontend(IQueueBackend backendQueue, int timeoutSeconds = 1, int collectionNodes = 3) : base(backendQueue)
        {
            var nodes = new List<BlockingCollection<ISource>>();
            for (int i = 0; i < collectionNodes; i++)
            {
                nodes.Add(new BlockingCollection<ISource>());
            }
            bufferQueue = nodes.ToArray();
            timeout = timeoutSeconds;
        }

        public void Flush(int timeoutSeconds)
        {
            while (!bufferQueue.All(o => o.IsCompleted))
            {
                ISource source;
                BlockingCollection<ISource>.TryTakeFromAny(bufferQueue, out source, TimeSpan.FromSeconds(timeoutSeconds));
                base.backend.Enqueue(source);
            }
        }

        new public void Enqueue(ISource source)
        {
            BlockingCollection<ISource>.TryAddToAny(bufferQueue, source, timeout);
        }

        new public void EnqueueRange(IEnumerable<ISource> sources)
        {
            foreach(var source in sources)
            {
                BlockingCollection<ISource>.TryAddToAny(bufferQueue, source, timeout);
            }
        }
    }
}
