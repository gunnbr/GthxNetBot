using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gthx.Core
{
    public partial class Tell
    {
        public int Id { get; set; }
        [StringLength(60)]
        public string Author { get; set; }
        [StringLength(60)]
        public string Recipient { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }
}
