using System;
using System.ComponentModel.DataAnnotations;

namespace Gthx.Core
{
    public partial class YoutubeRef
    {
        [Key]
        [StringLength(191)]
        public string Item { get; set; }
        [StringLength(255)]
        public string Title { get; set; }
        public int Count { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
