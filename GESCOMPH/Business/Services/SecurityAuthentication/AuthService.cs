using Business.Interfaces;
using Business.Interfaces.Implements.SecurityAuthentication;
using Data.Interfaz.IDataImplement.Persons;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Entity.Domain.Models.Implements.Persons;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Entity.DTOs.Implements.SecurityAuthentication.Auth.RestPasword;
using Entity.DTOs.Implements.SecurityAuthentication.Me;
using Entity.DTOs.Implements.SecurityAuthentication.User;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Utilities.Exceptions;
using Utilities.Messaging.Interfaces;

namespace Business.Services.SecurityAuthentication
{
    /// <summary>
    /// Servicio encargado de la autenticación, manejo de contraseñas y construcción del contexto de usuario (/me).
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IUserRepository _userRepository;
        private readonly IRolUserService _rolUserData;
        private readonly ILogger<AuthService> _logger;
        private readonly IMapper _mapper;
        private readonly ISendCode _emailService;
        private readonly IPasswordResetCodeRepository _passwordResetRepo;
        private readonly IUserContextService _userContext;
        private readonly IPersonRepository _personRepository;
        private readonly IToken _tokenService;

        public AuthService(
            IPasswordHasher<User> passwordHasher,
            IUserRepository userRepository,
            ILogger<AuthService> logger,
            IRolUserService rolUserData,
            IMapper mapper,
            ISendCode emailService,
            IPasswordResetCodeRepository passwordResetRepo,
            IUserContextService userContextService,
            IPersonRepository personRepository,
            IToken tokenService
        )
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _logger = logger;
            _rolUserData = rolUserData;
            _mapper = mapper;
            _emailService = emailService;
            _passwordResetRepo = passwordResetRepo;
            _userContext = userContextService;
            _personRepository = personRepository;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Autentica un usuario con email y contraseña, y genera los tokens correspondientes.
        /// </summary>
        public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await AuthenticateAsync(dto);
            var roles = await _rolUserData.GetRoleNamesByUserIdAsync(user.Id);

            var userDto = _mapper.Map<UserAuthDto>(user);
            userDto.Roles = roles;

            return await _tokenService.GenerateTokensAsync(userDto);
        }

        /// <summary>
        /// Valida las credenciales del usuario y devuelve la entidad correspondiente si son correctas.
        /// </summary>
        public async Task<User> AuthenticateAsync(LoginDto dto)
        {
            var user = await _userRepository.GetAuthUserByEmailAsync(dto.Email)
                ?? throw new UnauthorizedAccessException("Usuario o contraseña inválida.");

            var pwdResult = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.Password);
            if (pwdResult == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Usuario o contraseña inválida.");

            if (user.IsDeleted)
                throw new UnauthorizedAccessException("La cuenta está eliminada o bloqueada.");

            if (!user.Active)
                throw new UnauthorizedAccessException("La cuenta está inactiva. Contacta al administrador.");

            return user;
        }

        /// <summary>
        /// Solicita un código temporal de recuperación de contraseña para el usuario asociado al email.
        /// </summary>
        public async Task RequestPasswordResetAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email)
                ?? throw new ValidationException("Correo no registrado");

            var code = new Random().Next(100000, 999999).ToString();

            var resetCode = new PasswordResetCode
            {
                Email = email,
                Code = code,
                Expiration = DateTime.UtcNow.AddMinutes(10)
            };

            await _passwordResetRepo.AddAsync(resetCode);
            await SendRecoveryCodeEmailAsync(email, code);
        }

        /// <summary>
        /// Confirma un código de recuperación válido y establece una nueva contraseña para el usuario.
        /// </summary>
        public async Task ResetPasswordAsync(ConfirmResetDto dto)
        {
            var record = await _passwordResetRepo.GetValidCodeAsync(dto.Email, dto.Code)
                ?? throw new ValidationException("Código inválido o expirado");

            var user = await _userRepository.GetByEmailAsync(dto.Email)
                ?? throw new ValidationException("Usuario no encontrado");

            var hasher = new PasswordHasher<User>();
            user.Password = hasher.HashPassword(user, dto.NewPassword);

            await _userRepository.UpdateAsync(user);

            record.IsUsed = true;
            await _passwordResetRepo.UpdateAsync(record);

            _userContext.InvalidateCache(user.Id);
        }

        /// <summary>
        /// Permite a un usuario autenticado cambiar su contraseña actual.
        /// </summary>
        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId)
                       ?? throw new BusinessException("Usuario no encontrado.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
                throw new BusinessException("La contraseña actual es incorrecta.");

            user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);
            await _userRepository.UpdateAsync(user);
        }

        /// <summary>
        /// Construye el contexto (/me) del usuario autenticado, incluyendo roles y permisos.
        /// </summary>
        public Task<UserMeDto> BuildUserContextAsync(int userId)
            => _userContext.BuildUserContextAsync(userId);

        /// <summary>
        /// Envía un código de recuperación de contraseña al correo del usuario.
        /// </summary>
        private async Task SendRecoveryCodeEmailAsync(string email, string code)
        {
            try
            {
                await _emailService.SendRecoveryCodeEmail(email, code);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar el código de recuperación al correo: {Email}", email);
            }
        }
    }
}
