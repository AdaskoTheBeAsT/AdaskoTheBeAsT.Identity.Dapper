using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AntiforgeryController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public AntiforgeryController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    [HttpGet("token")]
    [AllowAnonymous]
    [ProducesResponseType<CsrfToken>(200)]
    public IActionResult GenerateToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

#pragma warning disable SCS0008, SCS0009
        Response.Cookies.Append(
            "CSRF-TOKEN",
            tokens.RequestToken ?? string.Empty,
            new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.None,
                Secure = true,
            });
#pragma warning restore SCS0008, SCS0009

        return Ok(new CsrfToken { Token = tokens.RequestToken });
    }
}
