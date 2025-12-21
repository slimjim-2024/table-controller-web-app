using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace asp_net_core.Models
{
    public class PicoAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Required]
        [DisallowNull]
        public string? TableID { get; set; }

        [DisallowNull]
        public string? ConnectedPico { get; set; }
    }
}
