using Microsoft.AspNetCore.Identity;

namespace MeniuMate_API.Auth.Model
{
    public class ForumRestUser : IdentityUser
    {
        public bool ForceRelogin { get; set; }
    }
}
