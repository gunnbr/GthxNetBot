using System;
using System.ComponentModel.DataAnnotations;

namespace Gthx.Core
{
    public partial class FactoidHistory
    {
        public int Id { get; set; }
        [StringLength(255)]
        public string Item { get; set; }
        [StringLength(512)]
        public string Value { get; set; }
        [StringLength(30)]
        public string User { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
