using System;
using System.Collections.Generic;

namespace Gthx.Core
{
    public partial class FactoidHistory
    {
        public int Id { get; set; }
        public string Item { get; set; }
        public string Value { get; set; }
        public string Nick { get; set; }
        public DateTime? Dateset { get; set; }
    }
}
