using Business.Interfaces;
using Business.Interfaces.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using WebGESCOMPH.Infrastructure;
using WebGESCOMPH.Controllers.Module.SecurityAuthentication;

namespace Test.Modulo.Web;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _auth = new();
    private readonly Mock<IToken> _token = new();
    private readonly Mock<IAuthCookieFactory> _cookies = new();
    private readonly Mock<IOptions<JwtSettings>> _jwt = new();
    private readonly Mock<IOptions<CookieSettings>> _cookieOpts = new();
    private readonly Mock<ILogger<AuthController>> _logger = new();

    private AuthController Create()
    {
        _jwt.Setup(x => x.Value).Returns(new JwtSettings
        {
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        _cookieOpts.Setup(x => x.Value).Returns(new CookieSettings
        {
            AccessTokenName = "at",
            RefreshTokenName = "rt",
            CsrfCookieName = "csrf"
        });

        _cookies.Setup(c => c.AccessCookieOptions(It.IsAny<DateTimeOffset>())).Returns(new CookieOptions());
        _cookies.Setup(c => c.RefreshCookieOptions(It.IsAny<DateTimeOffset>())).Returns(new CookieOptions());
        _cookies.Setup(c => c.CsrfCookieOptions(It.IsAny<DateTimeOffset>())).Returns(new CookieOptions());

        return new AuthController(
            _auth.Object,
            _token.Object,
            _cookies.Object,
            _jwt.Object,
            _cookieOpts.Object,
            _logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task Refresh_Unauthorized_WhenNoRefreshCookie()
    {
        var ctrl = Create();
        var res = await ctrl.Refresh();
        Assert.IsType<UnauthorizedObjectResult>(res);
    }

    [Fact]
    public async Task Refresh_Forbid_WhenCsrfHeaderMissing()
    {
        var ctrl = Create();
        // ? Simular cookies reales mediante el header "Cookie"
        ctrl.ControllerContext.HttpContext.Request.Headers["Cookie"] = "rt=value; csrf=abc";

        var res = await ctrl.Refresh();
        Assert.IsType<ForbidResult>(res);
    }

    [Fact]
    public async Task Login_Ok_SetsCookies()
    {
        // Arrange
        var tokenResponse = new TokenResponseDto
        {
            AccessToken = "acc",
            RefreshToken = "ref",
            CsrfToken = "csrf"
        };

        _auth.Setup(a => a.LoginAsync(It.IsAny<LoginDto>()))
             .ReturnsAsync(tokenResponse);


        var ctrl = Create();

        // Act
        var res = await ctrl.Login(new LoginDto { Email = "a@mail", Password = "x" });
        var ok = Assert.IsType<OkObjectResult>(res);

        // Assert
        var headers = ctrl.ControllerContext.HttpContext.Response.Headers["Set-Cookie"].ToString();

        Assert.Contains("at=acc", headers);
        Assert.Contains("rt=ref", headers);
        Assert.Contains("csrf=csrf", headers);
        Assert.True(((dynamic)ok.Value).isSuccess);
    }

    [Fact]
    public async Task Refresh_Ok_ReturnsNewTokens_AndSetsCookies()
    {
        // Arrange
        var refreshResponse = new TokenRefreshResponseDto
        {
            AccessToken = "newAcc",
            RefreshToken = "newRef",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        _token.Setup(t => t.RefreshAsync(It.IsAny<TokenRefreshRequestDto>()))
              .ReturnsAsync(refreshResponse);

        var ctrl = Create();

        // ? Simular cookies y header CSRF
        ctrl.ControllerContext.HttpContext.Request.Headers["Cookie"] = "rt=oldRefresh; csrf=abc";
        ctrl.ControllerContext.HttpContext.Request.Headers["X-XSRF-TOKEN"] = "abc";

        // Act
        var res = await ctrl.Refresh();
        var ok = Assert.IsType<OkObjectResult>(res);

        // Assert
        var headers = ctrl.ControllerContext.HttpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("at=newAcc", headers);
        Assert.Contains("rt=newRef", headers);

        var value = (dynamic)ok.Value;
        Assert.True(value.isSuccess);
    }
}
