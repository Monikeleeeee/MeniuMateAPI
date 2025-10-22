using MeniuMate_API.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace MeniuMate_API.Data.Entities
{
    public class Dish
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required DateTime CreationDate { get; set; }
        public required double Price { get; set; }
        public required string Ingredients { get; set; }
        public bool IsAvailable { get; set; }
        public string? ImageUrl { get; set; }

        public required Meniu Meniu { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ForumRestUser User { get; set; }

    }
    public record DishDto(int Id, string Name, string Description, double Price, string Ingredients, bool IsAvailable, string? ImageUrl);
    public record CreateDishDto(string Name, string Description, double Price, string Ingredients, bool IsAvailable, string? ImageUrl);
    public record UpdateDishDto(string Description, double Price, string Ingredients, bool IsAvailable, string? ImageUrl);

}
