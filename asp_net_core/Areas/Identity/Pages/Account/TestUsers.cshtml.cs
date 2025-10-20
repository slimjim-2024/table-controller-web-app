using asp_net_core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace asp_net_core.Areas.Identity.Pages.Account
{
    public class TestUsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public TestUsersModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public List<ApplicationUser> Users { get; set; } = new();
        public string Message { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            Users = await _userManager.Users.ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateTestUserAsync()
        {
            var existingUser = await _userManager.FindByNameAsync("testuser");
            if (existingUser != null)
            {
                Message = "Test user already exists!";
            }
            else
            {
                var testUser = new ApplicationUser
                {
                    UserName = "testuser",
                    Email = "test@test.com"
                };

                var result = await _userManager.CreateAsync(testUser, "Test123!");
                if (result.Succeeded)
                {
                    Message = "Test user created successfully! Username: testuser, Password: Test123!";
                }
                else
                {
                    Message = $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }

            Users = await _userManager.Users.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, "Test123!");
                
                if (result.Succeeded)
                {
                    Message = $"Password reset for user {user.UserName}. New password: Test123!";
                }
                else
                {
                    Message = $"Failed to reset password: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            else
            {
                Message = "User not found.";
            }

            Users = await _userManager.Users.ToListAsync();
            return Page();
        }
    }
}