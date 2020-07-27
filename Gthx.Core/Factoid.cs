using System;
using System.Collections.Generic;

namespace Gthx.Core
{
    public partial class Factoid
    {
        public int Id { get; set; }
        public string Item { get; set; }
        public bool Are { get; set; }
        public string Value { get; set; }
        public string Nick { get; set; }
        public DateTime? Dateset { get; set; }
        public bool? Locked { get; set; }
    }
}
