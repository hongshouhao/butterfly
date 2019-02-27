using System;
using System.Collections.Generic;
using System.Text;

namespace Butterfly.DataContract.Tracing
{
    public class TraceOperationHistogram
    {
        public string OperationName { get; set; }

        public long Count { get; set; }
    }
}
