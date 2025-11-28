using System.ComponentModel.DataAnnotations.Schema;

namespace asp_net_core.Models
{
    public class PreferredSettings
    {
        [ForeignKey("ApplicationUser")]
        public int User { get; set; }
        public int LowerHeight { get; set; }
        public int UpperHeight { get; set; }
    }
}
