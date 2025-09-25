using MeniuMate_API.Auth.Model;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace MeniuMate_API.Data.Entities
{
    public class Meniu
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required DateTime CreationDate { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ForumRestUser User {  get; set; }

    }

    public record MeniuDto (int Id, string Name, string Description);
    public record CreateMeniuDto(string Name, string Description);
    public record UpdateMeniuDto(string Description);
}
