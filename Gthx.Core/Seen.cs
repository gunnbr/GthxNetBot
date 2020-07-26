using System;
using System.Collections.Generic;

namespace Gthx.Core
{
    public partial class Seen
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Channel { get; set; }
        public DateTime? Timestamp { get; set; }
        public string Message { get; set; }
    }
}
