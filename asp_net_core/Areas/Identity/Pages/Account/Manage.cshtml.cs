using asp_net_core.Data;
using asp_net_core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace asp_net_core.Areas.Identity.Pages.Account
{
    public class ManageModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private static int MAX_HEIGHT = 1320;
        private static int MIN_HEIGHT = 680;
        public ManageModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
        }
        [BindProperty]
        public HeightChangeForm PreferredHeight { get; set; }
        public async Task OnGetAsync()
        {
            var userID = Guid.TryParse(_userManager.GetUserId(User), out Guid parsedGuid) ? parsedGuid : Guid.Empty;
            var existingSettings = await _context.PreferredSettings.FindAsync(userID);
            PreferredHeight = (existingSettings is not null) ? new HeightChangeForm
            {
                LowerHeight = existingSettings.LowerHeight,
                UpperHeight = existingSettings.UpperHeight
            } : new();
            
        }



        public async Task<IActionResult> OnPostSetPreferredHeightAsync()
        {
            bool HasErrors = (PreferredHeight.LowerHeight > PreferredHeight.UpperHeight) || (PreferredHeight.LowerHeight < MIN_HEIGHT || PreferredHeight.UpperHeight > MAX_HEIGHT);
            if (HasErrors)
            {
                if (PreferredHeight.LowerHeight > PreferredHeight.UpperHeight)
                {
                    ModelState.AddModelError("PreferredHeight", "Lower height cannot be greater than upper height.");
                }
                if (PreferredHeight.LowerHeight < MIN_HEIGHT || PreferredHeight.UpperHeight > MAX_HEIGHT)
                {
                    ModelState.AddModelError("PreferredHeight", $"Height must be between {MIN_HEIGHT} and {MAX_HEIGHT}.");
                }
                return Page();
            }

            var userID = Guid.TryParse(_userManager.GetUserId(User), out Guid parsedGuid) ? parsedGuid : Guid.Empty;
            var existingSettings = await _context.PreferredSettings.FindAsync(userID);

            if (existingSettings != null)
            {
                // Update existing entry
                existingSettings.LowerHeight = PreferredHeight.LowerHeight;
                existingSettings.UpperHeight = PreferredHeight.UpperHeight;
            }
            else
            {
                // Add new entry if doesn't exist
                var newSettings = new PreferredSettings
                {
                    User = userID,
                    LowerHeight = PreferredHeight.LowerHeight,
                    UpperHeight = PreferredHeight.UpperHeight
                };
                _context.PreferredSettings.Add(newSettings);
            }

            await _context.SaveChangesAsync();
            ViewData["SuccessMessage"] = "Preferred height settings updated successfully.";
            return RedirectToPage();
        }
    }
    public class HeightChangeForm
    {
        [Required(ErrorMessage = "Please input the lower height")]
        [Display(Name = "Lower Height:")]
        public int LowerHeight { get; set; } = 680;
        [Required(ErrorMessage = "Please input the upper height")]
        [Display(Name = "Upper height:")]
        public int UpperHeight { get; set; } = 1320;
    }

    
}
