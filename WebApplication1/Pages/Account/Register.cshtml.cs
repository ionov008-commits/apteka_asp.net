using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication1.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly PharmacyContext _context;
        public RegisterModel(PharmacyContext context) => _context = context;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Список аптек для выпадающего меню фармацевта
        public List<SelectListItem> PharmacyOptions { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Придумайте логин")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email обязателен")]
            [EmailAddress(ErrorMessage = "Некорректный формат почты")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Пароль обязателен")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Подтвердите пароль")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Пароли не совпадают")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = "customer";

            // Капча
            [Required(ErrorMessage = "Решите пример")]
            public int CaptchaInput { get; set; }

            // Поля Покупателя
            public string? FullName { get; set; }
            public string? Phone { get; set; }
            public string? Address { get; set; }

            // Поля Аптеки (Владелец)
            public string? PharmacyName { get; set; }
            public string? PharmacyAddress { get; set; }
            public string? PharmacyPhone { get; set; }
            public string? DirectorName { get; set; }

            // НОВЫЕ ПОЛЯ: Поля Фармацевта
            public string? PharmacistFullName { get; set; }
            public int? PharmacyId { get; set; }
            public string? WorkingHours { get; set; }

            // Изменил на nullable double?, чтобы форма не ругалась у фармацевта
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }

        public async Task OnGetAsync()
        {
            GenerateCaptcha();
            await LoadPharmaciesAsync();
        }

        private void GenerateCaptcha()
        {
            var rnd = new Random();
            int a = rnd.Next(1, 10);
            int b = rnd.Next(1, 10);
            HttpContext.Session.SetInt32("CaptchaResult", a + b);
            ViewData["CaptchaText"] = $"{a} + {b} = ?";
        }

        // Загрузка существующих аптек для выпадающего списка
        private async Task LoadPharmaciesAsync()
        {
            var pharmacies = await _context.Pharmacies.ToListAsync();
            PharmacyOptions = pharmacies.Select(p => new SelectListItem
            {
                Value = p.PharmacyId.ToString(),
                Text = p.Name
            }).ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Проверка капчи
            int? correctCaptcha = HttpContext.Session.GetInt32("CaptchaResult");
            if (Input.CaptchaInput != correctCaptcha)
            {
                ModelState.AddModelError("Input.CaptchaInput", "Неверный ответ капчи.");
            }

            // 2. Валидация уникальности АККАУНТА (User)
            if (await _context.Users.AnyAsync(u => u.Username == Input.Username))
                ModelState.AddModelError("Input.Username", "Этот логин уже занят.");

            if (await _context.Users.AnyAsync(u => u.Email == Input.Email))
                ModelState.AddModelError("Input.Email", "Этот Email уже зарегистрирован.");

            // 3. Специфичная валидация в зависимости от роли
            if (Input.Role == "customer")
            {
                if (string.IsNullOrWhiteSpace(Input.FullName))
                    ModelState.AddModelError("Input.FullName", "ФИО обязательно.");

                if (string.IsNullOrWhiteSpace(Input.Address))
                    ModelState.AddModelError("Input.Address", "Адрес обязателен.");

                if (!string.IsNullOrEmpty(Input.Phone) && await _context.Customers.AnyAsync(c => c.Phone == Input.Phone))
                    ModelState.AddModelError("Input.Phone", "Этот номер телефона уже используется другим покупателем.");

                if (Input.Latitude == null || Input.Longitude == null)
                    ModelState.AddModelError("Input.Latitude", "Укажите местоположение на карте.");
            }
            else if (Input.Role == "owner")
            {
                if (string.IsNullOrWhiteSpace(Input.PharmacyName))
                    ModelState.AddModelError("Input.PharmacyName", "Название аптеки обязательно.");

                if (string.IsNullOrWhiteSpace(Input.DirectorName))
                    ModelState.AddModelError("Input.DirectorName", "ФИО руководителя обязательно.");

                if (!string.IsNullOrEmpty(Input.PharmacyPhone) && await _context.Pharmacies.AnyAsync(p => p.Phone == Input.PharmacyPhone))
                    ModelState.AddModelError("Input.PharmacyPhone", "Этот номер телефона уже закреплен за другой аптекой.");

                if (Input.Latitude == null || Input.Longitude == null)
                    ModelState.AddModelError("Input.Latitude", "Укажите местоположение на карте.");
            }
            else if (Input.Role == "pharmacist")
            {
                if (string.IsNullOrWhiteSpace(Input.PharmacistFullName))
                    ModelState.AddModelError("Input.PharmacistFullName", "ФИО фармацевта обязательно.");

                if (Input.PharmacyId == null)
                    ModelState.AddModelError("Input.PharmacyId", "Необходимо выбрать аптеку из списка.");

                if (string.IsNullOrWhiteSpace(Input.WorkingHours))
                    ModelState.AddModelError("Input.WorkingHours", "Часы работы обязательны.");
            }

            // 4. Если есть ошибки — перезагружаем страницу
            if (!ModelState.IsValid)
            {
                GenerateCaptcha();
                await LoadPharmaciesAsync(); // Чтобы список не пропал при ошибке валидации
                return Page();
            }

            // 5. Создаем базового юзера
            var newUser = new User
            {
                Username = Input.Username,
                Email = Input.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                Role = Input.Role
            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // 6. Заполняем связанную таблицу в зависимости от роли
            if (Input.Role == "owner")
            {
                var pharmacyEntry = new WebApplication1.Models.Pharmacy
                {
                    UserId = newUser.UserId,
                    Name = Input.PharmacyName!,
                    Address = Input.PharmacyAddress!,
                    Phone = Input.PharmacyPhone!,
                    DirectorName = Input.DirectorName!,
                    WorkingHours = "08:00-20:00",
                    Latitude = Input.Latitude ?? 0,
                    Longitude = Input.Longitude ?? 0
                };
                _context.Pharmacies.Add(pharmacyEntry);
            }
            else if (Input.Role == "customer")
            {
                var customerEntry = new Customer
                {
                    UserId = newUser.UserId,
                    FullName = Input.FullName!,
                    Phone = Input.Phone!,
                    DefaultAddress = Input.Address!,
                    Latitude = Input.Latitude ?? 0,
                    Longitude = Input.Longitude ?? 0
                };
                _context.Customers.Add(customerEntry);
            }
            else if (Input.Role == "pharmacist")
            {
                // ВАЖНО: Предполагается, что в Models есть сущность Pharmacist
                var pharmacistEntry = new Pharmacist
                {
                    UserId = newUser.UserId,
                    FullName = Input.PharmacistFullName!,
                    PharmacyId = Input.PharmacyId.Value,
                    WorkingHours = Input.WorkingHours!
                };
                _context.Pharmacists.Add(pharmacistEntry);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("/Account/Login");
        }
    }
}