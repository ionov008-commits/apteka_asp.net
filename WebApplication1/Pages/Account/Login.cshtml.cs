using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WebApplication1.Models;

namespace WebApplication1.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly PharmacyContext _context;
        public LoginModel(PharmacyContext context) => _context = context;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Введите логин или Email")]
            public string LoginHandle { get; set; } = string.Empty;

            [Required(ErrorMessage = "Введите пароль")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Решите пример")]
            public int CaptchaInput { get; set; }
        }

        public void OnGet() => GenerateCaptcha();

        private void GenerateCaptcha()
        {
            var rnd = new Random();
            int a = rnd.Next(1, 10);
            int b = rnd.Next(1, 10);
            HttpContext.Session.SetInt32("LoginCaptcha", a + b);
            ViewData["CaptchaText"] = $"{a} + {b} = ?";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? correctCaptcha = HttpContext.Session.GetInt32("LoginCaptcha");
            if (Input.CaptchaInput != correctCaptcha)
            {
                ModelState.AddModelError("Input.CaptchaInput", "Капча решена неверно.");
            }

            if (!ModelState.IsValid) { GenerateCaptcha(); return Page(); }

            // Ищем по логину ИЛИ по почте
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == Input.LoginHandle || u.Email == Input.LoginHandle);

            if (user == null || !BCrypt.Net.BCrypt.Verify(Input.Password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Неверные данные для входа.");
                GenerateCaptcha();
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToPage("/Index");
        }
    }
}