namespace Entity.DTOs.Implements.SecurityAuthentication.Auth
{
    public class UserAuthDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = default!;
        public int? PersonId { get; set; }
        public bool Active { get; set; }
        public bool IsDeleted { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}
