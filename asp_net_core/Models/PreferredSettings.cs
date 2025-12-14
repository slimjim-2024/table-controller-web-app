using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace asp_net_core.Models
{
    public class PreferredSettings
    {
        [Key]
        [ForeignKey("ApplicationUser")]
        public Guid User { get; set; }
        public int LowerHeight { get; set; }
        public int UpperHeight { get; set; }
        public ApplicationUser ApplicationUser { get; set;}
    }
}
