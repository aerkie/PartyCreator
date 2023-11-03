﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartyCreatorWebApi.Dtos;
using PartyCreatorWebApi.Entities;
using PartyCreatorWebApi.Repositories.Contracts;

namespace PartyCreatorWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly IUsersRepository _usersRepository;

        public AuthController(IAuthRepository authRepository ,IUsersRepository usersRepository)
        {
            _authRepository = authRepository;
            _usersRepository = usersRepository;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto request)
        {   
            var result = await _usersRepository.GetUserByEmail(request.Email);

            if (result != null)
            {
                return BadRequest("Użytkownik o takim email już istnieje");
            }

            _authRepository.CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            User user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Description = request.Description,
                Birthday = request.Birthday,
                Email = request.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt
            };

            var addedUser = await _usersRepository.AddUser(user);

            if(addedUser==null)
            {
                return BadRequest("Wystapil problem, nie udalo sie zarejestrowac");
            }

            UserDto userDto = new UserDto
            {
                Id = addedUser.Id,
                FirstName = addedUser.FirstName,
                LastName = addedUser.LastName,
                Description = addedUser.Description,
                Birthday = addedUser.Birthday,
                Email = addedUser.Email
            };

            return Ok(userDto);
        }

        [HttpGet, Authorize]
        public ActionResult<string> GetMe()
        {
            var userId = _usersRepository.GetUserIdFromContext();
            return Ok(userId);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto request)
        {
            var result = await _usersRepository.GetUserByEmail(request.Email);

            if(result == null)
            {
                return BadRequest("Nie znaleziono użytkownika");
            }

            if(!_authRepository.VerifyPasswordHash(request.Password, result.PasswordHash, result.PasswordSalt))
            {
                return BadRequest("Podane haslo sie nie zgadza");
            }

            string token = _authRepository.CreateToken(result);

            LoginResponseDto loginResponseDto = new LoginResponseDto
            {
                Token = token
            };

            return Ok(loginResponseDto);
        }
    }
}
