using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ArtContentManager.Static
{
    internal static class Log
    {
        /// <summary>  
        /// Initiates a Tracer which will print to both  
        /// the Console and to a log file, log.txt  
        /// </summary>  
        public static void InitiateTracer()
        {
            Trace.Listeners.Clear();
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var twtl = new TextWriterTraceListener("ACM_log.txt")
            {
                Name = "TextLogger",
                TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
            };
            var ctl = new ConsoleTraceListener(false) { TraceOutputOptions = TraceOptions.DateTime };
            Trace.Listeners.Add(twtl);
            Trace.Listeners.Add(ctl);
            Trace.AutoFlush = true;
        }
    }
}
