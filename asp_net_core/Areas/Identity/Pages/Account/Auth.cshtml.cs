using asp_net_core.Models;
using asp_net_core.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace asp_net_core.Areas.Identity.Pages.Account
{
    public class AuthModel : PageModel
    {

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ILogger<AuthModel> _logger;

        public AuthModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AuthModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public LoginViewModel LoginForm { get; set; } = new();

        [BindProperty]
        public RegisterViewModel RegisterForm { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            _logger.LogInformation("Login attempt started");
            ClearRegisterFormErrors();

            if (!IsLoginFormValid())
            {
                _logger.LogWarning("Login ModelState is invalid");
                return Page();
            }

            _logger.LogInformation("Attempting login for user: {UserName}", LoginForm.UserName);

            var result = await _signInManager.PasswordSignInAsync(LoginForm.UserName, LoginForm.Password, LoginForm.RememberMe, false);

            if (result.Succeeded)
            {
                _logger.LogInformation("Login successful, redirecting to Home/Index");
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Account locked out for user: {UserName}", LoginForm.UserName);
                ModelState.AddModelError("LoginForm", "Account locked out.");
            }
            else if (result.IsNotAllowed)
            {
                _logger.LogWarning("Login not allowed for user: {UserName}", LoginForm.UserName);
                ModelState.AddModelError("LoginForm", "Login not allowed.");
            }
            else
            {
                _logger.LogWarning("Invalid login attempt for user: {UserName}", LoginForm.UserName);
                ModelState.AddModelError("LoginForm", "Invalid login attempt.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            _logger.LogInformation("Registration attempt started");
            ClearLoginFormErrors();
            if (!IsRegisterFormValid())
            {
                _logger.LogWarning("Registration ModelState is invalid");
                return Page();
            }

            var user = new ApplicationUser { UserName = RegisterForm.UserName, Email = RegisterForm.Email };
            var result = await _userManager.CreateAsync(user, RegisterForm.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created successfully: {UserName}", RegisterForm.UserName);
                await _signInManager.SignInAsync(user, isPersistent: RegisterForm.RememberMe);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                _logger.LogWarning("Registration error: {Error}", error.Description);
                ModelState.AddModelError("RegisterForm", error.Description);
            }

            return Page();
        }

        private bool IsLoginFormValid()
        {
            // Check only LoginForm fields
            var loginKeys = ModelState.Keys.Where(k => k.StartsWith("LoginForm.")).ToList();
            var isValid = true;

            foreach (var key in loginKeys)
            {
                if (ModelState[key].ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                {
                    isValid = false;
                }
            }

            // Also check for required fields manually if empty
            if (string.IsNullOrWhiteSpace(LoginForm.UserName))
            {
                ModelState.AddModelError("LoginForm.UserName", "Username is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(LoginForm.Password))
            {
                ModelState.AddModelError("LoginForm.Password", "Password is required");
                isValid = false;
            }

            return isValid;
        }

        private bool IsRegisterFormValid()
        {
            // Check only RegisterForm fields
            var registerKeys = ModelState.Keys.Where(k => k.StartsWith("RegisterForm.")).ToList();
            var isValid = true;

            foreach (var key in registerKeys)
            {
                if (ModelState[key].ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                {
                    isValid = false;
                }
            }

            // Manual validation for critical fields
            if (string.IsNullOrWhiteSpace(RegisterForm.UserName))
            {
                ModelState.AddModelError("RegisterForm.UserName", "Username is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterForm.Email))
            {
                ModelState.AddModelError("RegisterForm.Email", "Email is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(RegisterForm.Password))
            {
                ModelState.AddModelError("RegisterForm.Password", "Password is required");
                isValid = false;
            }

            if (RegisterForm.Password != RegisterForm.ConfirmPassword)
            {
                ModelState.AddModelError("RegisterForm.ConfirmPassword", "Passwords do not match");
                isValid = false;
            }

            return isValid;
        }

        private void ClearLoginFormErrors()
        {
            var loginKeys = ModelState.Keys.Where(k => k.StartsWith("LoginForm.")).ToList();
            foreach (var key in loginKeys)
            {
                ModelState.Remove(key);
            }
        }

        private void ClearRegisterFormErrors()
        {
            var registerKeys = ModelState.Keys.Where(k => k.StartsWith("RegisterForm.")).ToList();
            foreach (var key in registerKeys)
            {
                ModelState.Remove(key);
            }
        }

        private void LogValidationErrors()
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
            }
        }
    }
}
