using Journals.Api;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;

namespace Journals
{
    /// <summary>
    /// Reads a journal file and submits data to buffer.
    /// </summary>
    public class FlyingReader : IDisposable
    {
        readonly string _token;
        readonly FileStream _stream;
        readonly FlyingBuffer _buffer;

        readonly Thread _thread;
        bool _stopped;

        public bool Paused; // pauses all readers

        public FlyingReader(Journal journal, FlyingBuffer buffer)
        {
            var path = journal.Path;
            
            _token = journal.Token;
            _buffer = buffer;

            _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            (_thread = new Thread(BackgroundRead) { IsBackground = true }).Start();
        }

        public void Stop()
        {
            _stopped = true;
            _thread.Join();
        }

        void BackgroundRead()
        {
            using (var reader = new StreamReader(_stream))
            {
                while (!_stopped)
                {
                    var line = reader.ReadLine();

                    if (null == line || Paused)
                    {
                        Thread.Sleep(20);
                    }

                    else
                        OnLine(line);
                }
            }
        }

        void OnLine(string line)
        {
            var seriline = new FastSerilogLine(line) {["@log"] = _token};

            _buffer.AddBuffer(seriline);
        }

        public void Dispose()
        {
            _stream?.Close();
            _stream?.Dispose();
        }
    }
}
