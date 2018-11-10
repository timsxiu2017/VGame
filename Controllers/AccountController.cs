using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.Text;
using VGame.AccountCenter;

namespace VGame.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController:Controller
    {
        private IConfiguration _configuration;
        private IAccountCenter _accountCenter;
        private readonly ILogger _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();
        public AccountController(IConfiguration configuration,ILogger<AccountController> logger,IAccountCenter accountCenter){
            _configuration = configuration;
            _accountCenter = accountCenter;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Ping()
        {
            return Json("PONG");
        }

        [HttpPost]
        public async Task<IActionResult> Token([FromBody]AccountModel account)
        {
            try
            {
                string token = _accountCenter.GetToken(account.Name,account.Password);
                return Json(new ServiceResult(token));
            }
            catch(Exception ex)
            {
                return Json(new ServiceResult(ex));
            }
            
        }


    }
}


