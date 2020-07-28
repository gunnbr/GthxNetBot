using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gthx.Core
{
    public partial class Ref
    {
        [Key]
        [StringLength(191)] // TODO: Why 191? Is this leftover from the MySQL/Python implementation?
        public string Item { get; set; }
        public int Count { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
