using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Constants;
using WebApplication1.Data;
using WebApplication1.Data.Entities;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize("Bearer")]
    public class UserController : ControllerBase
    {


        private readonly UserManager<ManageUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ManageAppDbContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IViewRenderService _viewRenderService;
        private readonly ICacheService _cacheService;
        public UserController(UserManager<ManageUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ManageAppDbContext context, IConfiguration configuration,ILogger<UserController> logger, IEmailSender emailSender,
            IViewRenderService viewRenderService, ICacheService cacheService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _emailSender = emailSender;
            _viewRenderService = viewRenderService;
            _cacheService = cacheService;
        }

        [HttpPost]
    
        public async Task<IActionResult> PostUser(UserCreateRequest request) // vì khởi tạo lên ta dùng request
        {
            _logger.LogInformation("Begin PostUser API");

            var dob = DateTime.Parse(request.Dob);
            var user = new ManageUser() // vì tạo một User lên ta dùng User Entites luân vì nó có đủ các tường
            {
                Id = Guid.NewGuid().ToString(),
                Email = request.Email,
                BirthDay = DateTime.Parse(request.Dob),
                UserName = request.UserName,
                DisPlayName = request.LastName,
               
                PhoneNumber = request.PhoneNumber,
               
            };
            var result = await _userManager.CreateAsync(user, request.Password); // phương thức CreateAsync đã được Identity.Core, hỗ trợ , bài miên phí ta phải viết nó
            if (result.Succeeded)
            {
                _logger.LogInformation("End PostUser API - Success");
                await _cacheService.RemoveAsync(CacheConstants.GetUsers);
                // send Mail
                var repliedComment = await _context.ManageUsers.FindAsync(user.Id);

                var emailModel = new UserVm()
                {
                    LastName = request.LastName,
                    FirstName = request.FirstName,
                    PhoneNumber = request.PhoneNumber,
                    UserName = request.UserName,
                    Email = request.Email,

                };
                //https://github.com/leemunroe/responsive-html-email-template
                var htmlContent = await _viewRenderService.RenderToStringAsync("_PostUserEmail", emailModel);
                await _emailSender.SendEmailAsync(request.Email, "Bạn đã tạo tài khoản thành công", htmlContent);


                return CreatedAtAction(nameof(GetById), new { id = user.Id }, request);
            }
            else
            {
                _logger.LogInformation("End PostUser API - Fails");

                return BadRequest(new ApiBadRequestResponse(result));
            }
        }




        [HttpGet]
       
        public async Task<IActionResult> GetUsers()
        {
           

            var cachedData = await _cacheService.GetAsync<List<UserVm>>(CacheConstants.GetUsers);

            if (cachedData == null)
            {
                var users = _userManager.Users.AsNoTracking();
                var uservms = await users.Select(u => new UserVm() // vì muốn xem lên ta dùng UserVm
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Dob = u.BirthDay,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    FirstName = u.DisPlayName,

                }).ToListAsync();

                if (uservms.Count > 0)
                {
                    await _cacheService.SetAsync(CacheConstants.GetUsers, uservms, 24);

                    cachedData = uservms;
                }

            }


            return Ok(cachedData);
        }

        [HttpGet("{id}")]
       
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new ApiNotFoundResponse($"Cannot found user with id: {id}"));

            var userVm = new UserVm()
            {
                Id = user.Id,
                UserName = user.UserName,
                Dob = user.BirthDay,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FirstName = user.DisPlayName,
               
            };
            return Ok(userVm);
        }


    }
}
