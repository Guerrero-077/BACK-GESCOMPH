using Business.Interfaces.IBusiness;
using Entity.DTOs.Implements.SecurityAuthentication.User;

namespace Business.Interfaces.Implements.SecurityAuthentication
{
    public interface IUserService : IBusiness<UserSelectDto, UserCreateDto, UserUpdateDto>
    {
        Task<(int userId, bool created, string? tempPassword)> EnsureUserForPersonAsync(int personId, string email);

    }
}
