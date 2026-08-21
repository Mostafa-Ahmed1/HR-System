using System.Security.Claims;
using HR_System.Models;
using HR_System.Security;
using HR_System.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Controllers;

public class OperationController : Controller
{
    private static readonly TimeSpan PersistentLoginLifetime = TimeSpan.FromDays(14);

    private readonly HrSysContext _db;
    private readonly IPasswordMigrationService<Admin> _adminPasswords;
    private readonly IPasswordMigrationService<User> _userPasswords;
    private readonly HrClaimsPrincipalFactory _principalFactory;
    private readonly TimeProvider _timeProvider;

    public OperationController(
        HrSysContext db,
        IPasswordMigrationService<Admin> adminPasswords,
        IPasswordMigrationService<User> userPasswords,
        HrClaimsPrincipalFactory principalFactory,
        TimeProvider timeProvider)
    {
        _db = db;
        _adminPasswords = adminPasswords;
        _userPasswords = userPasswords;
        _principalFactory = principalFactory;
        _timeProvider = timeProvider;
    }

    [Authorize]
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");

    [AllowAnonymous]
    [HttpGet]
    public IActionResult login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var admin = await _db.Admins.FirstOrDefaultAsync(candidate => candidate.AdminName == model.Username);
        if (admin is not null)
        {
            var result = _adminPasswords.Verify(admin, admin.AdminPass, model.Password);
            if (result != PasswordCheckResult.Failed)
            {
                if (RequiresPasswordUpgrade(result))
                {
                    admin.AdminPass = _adminPasswords.Hash(admin, model.Password);
                    await _db.SaveChangesAsync();
                }

                await SignInAsync(_principalFactory.Create(admin), model.RememberMe);
                return RedirectAfterLogin(model.ReturnUrl);
            }
        }

        var user = await _db.Users.FirstOrDefaultAsync(candidate => candidate.Username == model.Username);
        if (user is not null)
        {
            var result = _userPasswords.Verify(user, user.Password, model.Password);
            if (result != PasswordCheckResult.Failed)
            {
                if (RequiresPasswordUpgrade(result))
                {
                    user.Password = _userPasswords.Hash(user, model.Password);
                    await _db.SaveChangesAsync();
                }

                await SignInAsync(_principalFactory.Create(user), model.RememberMe);
                return RedirectAfterLogin(model.ReturnUrl);
            }
        }

        ModelState.AddModelError(string.Empty, "Incorrect username or password.");
        ViewBag.status = "Incorrect username or password.";
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(login));
    }

    private static bool RequiresPasswordUpgrade(PasswordCheckResult result)
        => result is PasswordCheckResult.SucceededLegacyUpgradeNeeded
            or PasswordCheckResult.SucceededRehashNeeded;

    private async Task SignInAsync(ClaimsPrincipal principal, bool rememberMe)
    {
        var properties = new AuthenticationProperties
        {
            AllowRefresh = true,
            IsPersistent = rememberMe
        };

        if (rememberMe)
        {
            properties.ExpiresUtc = _timeProvider.GetUtcNow().Add(PersistentLoginLifetime);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }

    private IActionResult RedirectAfterLogin(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Dashboard");
}
