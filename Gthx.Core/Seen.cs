using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gthx.Core
{
    public partial class Seen
    {
        public int Id { get; set; }
        [StringLength(30)]
        public string User { get; set; }
        [StringLength(30)]
        public string Channel { get; set; }
        public DateTime Timestamp { get; set; }
        [StringLength(512)]
        public string Message { get; set; }
    }
}
