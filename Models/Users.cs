using Microsoft.AspNetCore.Identity;

namespace UsersApp.Models
{
    public class Users : IdentityUser
    {
        public string? FullName { get; set; }
        public string? ThemePreference { get; set; } = "light"; // light, dark, custom
        public string? CustomThemeColors { get; set; } // JSON string for custom colors
    }
}
