using Journals.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Journals
{
    public class FlyingBuffer
    {
        public readonly long BufferSize;
        public readonly List<FastSerilogLine> Buffer;

        Processor _defaultProcessor = new Processor { Process = x => x };
        Processor _processor;

        public long Number = 1;
        readonly object _csection = new object();

        public Action<FastSerilogLine> Added;

        public FlyingBuffer(long bufferSize, Processor processor = null)
        {
            BufferSize = bufferSize;
            Buffer = new List<FastSerilogLine>(100);

            Processor = processor;
        }

        public Processor Processor
        {
            get
            {
                lock (_csection)
                {
                    return _processor;
                }
            }
            set
            {
                lock (_csection)
                {
                    _processor = value ?? _defaultProcessor;

                    var filtered = Buffer.Where(x => null != _processor.Process(x)).ToArray();

                    Buffer.Clear();
                    Buffer.AddRange(filtered);
                }
            }
        }

        public void AddBuffer(FastSerilogLine line)
        {
            lock (_csection)
            {
                while (Buffer.Count >= BufferSize)
                    Buffer.RemoveAt(0);

                if (null != (line = _processor.Process(line)))
                {
                    line.Number = AllocateNumber();
                    Buffer.Add(line);

                    if (null != Added)
                        Added(line);
                }
            }
        }

        public long AllocateNumber() => Interlocked.Increment(ref Number);

        public FastSerilogLine[] GetBuffer(out long number)
        {
            lock (_csection)
            {
                number = Number;
                return Buffer.ToArray();
            }
        }
    }
}