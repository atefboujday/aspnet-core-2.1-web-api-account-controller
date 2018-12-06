using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AspNetCoreWebApp.Data;
using AspNetCoreWebApp.Helpers;
using AspNetCoreWebApp.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Pages.Account.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AspNetCoreWebApp.Controllers
{
    [EnableCors("CorsPolicy")]
    [Route("api/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _SignInManager;
        private readonly ILogger _Logger;

        private readonly UserManager<ApplicationUser> _UserManager;
        private readonly RoleManager<IdentityRole> _RoleManager;
        public IConfiguration _Configuration { get; }
        private IPasswordHasher<ApplicationUser> _PasswordHasher;

        public AccountController(SignInManager<ApplicationUser> signInManager,
                                 ILogger<AccountController> logger, 
                                 UserManager<ApplicationUser> userManager,
                                 IPasswordHasher<ApplicationUser> passwordHasher,
                                 RoleManager<IdentityRole> roleManager, 
                                 IConfiguration configuration)
        {
            _SignInManager = signInManager;
            _Logger = logger;
            _UserManager = userManager;
            _PasswordHasher = passwordHasher;
            _RoleManager = roleManager;
            _Configuration = configuration;
        }
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        FullName = model.FullName,
                        Email = model.Email
                    };
                    var result = await _UserManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        _Logger.LogInformation("User created a new account with password.");

                        return Ok(true);
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return BadRequest(ApiResponse.GetError(ModelState));
                    }
                }
                else
                    return BadRequest(ApiResponse.GetError(ModelState));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.GetError(ex.Message.ToString()));
            }
        }
        [HttpPost]
        //public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        public async Task<IActionResult> Login([FromBody] LoginModel.InputModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _UserManager.FindByNameAsync(model.Email);
                    if (user == null)
                        return BadRequest(ApiResponse.GetError("UserName is incorrect"));
                    else if (_PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Success)
                    {
                        return await GetToken(user);
                    }
                    else
                        return BadRequest(ApiResponse.GetError("Password is incorrect!"));
                }
                else
                {
                    return BadRequest(ApiResponse.GetError(ModelState));
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError($"error while creating token: {ex}");
                return StatusCode((int)HttpStatusCode.InternalServerError, "error while creating token");
            }
        }

        
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var _User = await _UserManager.FindByNameAsync(User.Identity.Name);
                if (_User == null)
                {
                    return NotFound($"Unable to load user with ID '{_UserManager.GetUserId(User)}'.");
                }

                var changePasswordResult = await _UserManager.ChangePasswordAsync(_User, model.OldPassword, model.NewPassword);
                if (changePasswordResult.Succeeded)
                {
                    return await GetToken(_User);
                }
                else
                    return BadRequest(ApiResponse.GetError(changePasswordResult));
            }
            else
                return BadRequest(ApiResponse.GetError(ModelState));


        }

        #region private
        private async Task<IActionResult> GetToken(ApplicationUser user)
        {
            try
            {
                var _UserClaims = await _UserManager.GetClaimsAsync(user);

                List<Claim> _Claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.UserName),
                            new Claim(ClaimTypes.Name, user.UserName),
                            new Claim(ClaimTypes.NameIdentifier, user.UserName),
                            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(JwtRegisteredClaimNames.Email, user.Email),
                            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName)
                        };
                _Claims.Union(_UserClaims);

                IList<string> _Roles = await _UserManager.GetRolesAsync(user);

                foreach (var userRole in _Roles)
                {
                    _Claims.Add(new Claim(ClaimTypes.Role, userRole));
                    var role = await _RoleManager.FindByNameAsync(userRole);
                    if (role != null)
                    {
                        var roleClaims = await _RoleManager.GetClaimsAsync(role);
                        foreach (Claim roleClaim in roleClaims)
                        {
                            _Claims.Add(roleClaim);
                        }
                    }
                }
                string _TokenKey = _Configuration["JwtSecurityToken:Key"];
                string _TokenAudience = _Configuration["JwtSecurityToken:Audience"];
                string _TokenIssuer = _Configuration["JwtSecurityToken:Issuer"];

                var _SymmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_TokenKey));
                var _SigningCredentials = new SigningCredentials(_SymmetricSecurityKey, SecurityAlgorithms.HmacSha256);

                var _JwtSecurityToken = new JwtSecurityToken(issuer: _TokenIssuer,
                                                            audience: _TokenAudience,
                                                            claims: _Claims,
                                                            expires: DateTime.UtcNow.AddDays(180),
                                                            signingCredentials: _SigningCredentials);

                UserAccessModel _UserAccess = new UserAccessModel(fullName: user.FullName,
                                                                  token: new JwtSecurityTokenHandler().WriteToken(_JwtSecurityToken),
                                                                  expirationDate: _JwtSecurityToken.ValidTo);
                return Ok(_UserAccess);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
