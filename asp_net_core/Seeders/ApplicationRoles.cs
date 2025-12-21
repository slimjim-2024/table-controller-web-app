using asp_net_core.Data;
using asp_net_core.Models;
using Microsoft.AspNetCore.Http.HttpResults;
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
                try
                {
                    if (await roleManager.RoleExistsAsync(role)) continue;
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    Console.WriteLine($"Error creating role {role}: {ex.Message}");
                }
            }

            var _userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
            if (_userManager is null)
            {
                throw new ArgumentNullException(nameof(_userManager));
            }

            // Check if SuperAdmin user already exists
            var existingUser = await _userManager.FindByNameAsync("SuperAdmin");
            if (existingUser == null)
            {
                // Create the SuperAdmin user
                var newUser = new ApplicationUser() 
                { 
                    UserName = "SuperAdmin", 
                    Email = "Admin@admin.corp",
                    EmailConfirmed = true
                };
                
                var result = await _userManager.CreateAsync(newUser, "superadminPass1234!");

                if (!result.Succeeded)
                {
                    // Log the actual errors
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Error creating SuperAdmin user: {errors}");
                    throw new InvalidOperationException($"Failed to create SuperAdmin user: {errors}");
                }

                Console.WriteLine("SuperAdmin user created successfully.");
            }
            else
            {
                Console.WriteLine("SuperAdmin user already exists.");
            }

            //await AssignRoles(_userManager, "Admin12", roles);
        }
        public static async Task<IdentityResult> AssignRoles(UserManager<ApplicationUser> _userManager, string userName, string[] roles, string confirmationString)
        {
            IdentityResult result = IdentityResult.Failed();

            ApplicationUser? adminUser = await _userManager.FindByNameAsync("SuperAdmin");

            if (adminUser is null) return result;

            if (!await _userManager.CheckPasswordAsync(adminUser, confirmationString)) return result;



            //var _userManager = services.GetService<UserManager<ApplicationUser>>();
            ApplicationUser? user = await _userManager.FindByNameAsync(userName);
            if (user is not null)
            {
                result = await _userManager.AddToRolesAsync(user, roles);
            }

            return result;
        }
        public static async Task<IdentityResult> AssignRoles(UserManager<ApplicationUser> _userManager, ApplicationUser user, string[] roles, string confirmationString)
        {
            IdentityResult result = IdentityResult.Failed();

            ApplicationUser? adminUser = await _userManager.FindByNameAsync("SuperAdmin");

            if (adminUser is null) return result;

            if (!await _userManager.CheckPasswordAsync(adminUser, confirmationString)) return result;



            //var _userManager = services.GetService<UserManager<ApplicationUser>>();
            result = await _userManager.AddToRolesAsync(user, roles);


            return result;
        }
    }
}
