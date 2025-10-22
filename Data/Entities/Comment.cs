using MeniuMate_API.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace MeniuMate_API.Data.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public required int Rating { get; set; }
        public required DateTime CreationDate { get; set; }

        public required Dish Dish { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ForumRestUser User { get; set; }

    }
    public record CommentDto(int Id, string Content, int Rating, int DishId, string UserId);
    public record CreateCommentDto(string Content, int Rating);
    public record UpdateCommentDto(string Content, int Rating);
}
