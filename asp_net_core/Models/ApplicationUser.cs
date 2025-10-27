using Microsoft.AspNetCore.Identity;

namespace asp_net_core.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public byte? Role { get; set; } = 0b00000000;


        }
}
