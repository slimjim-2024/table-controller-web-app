using asp_net_core.Data;
using asp_net_core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace asp_net_core.Seeders
{
    public class ApplicationRoles
    {
        public static async Task InitializeRoles(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetService<ApplicationDbContext>();
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            //string[] roles = { "Admin", "Employee", "Cleaner" };
            //var roleStore = new RoleStore<IdentityRole, ApplicationDbContext>(context);

            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            string[] roles = { "Admin", "Employee", "Cleaner" };
            foreach (var role in roles)
            {
                if (await roleManager.RoleExistsAsync(role)) continue;
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
            var _userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

            await AssignRoles(serviceProvider, "Admin12", roles);
        }
        public static async Task<IdentityResult> AssignRoles(IServiceProvider services, string userName, string[] roles)
        {
            IdentityResult result = IdentityResult.Failed();
            var _userManager = services.GetService<UserManager<ApplicationUser>>();
            ApplicationUser? user = await _userManager.FindByNameAsync(userName);
            if (user is not null)
            {
                result = await _userManager.AddToRolesAsync(user, roles);
            }

            return result;
        }
    }
}
