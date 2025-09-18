namespace MeniuMate_API.Data.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public required int Rating { get; set; }
        public required DateTime CreationDate { get; set; }

        public required Dish Dish { get; set; }
    }
    public record CommentDto(int Id, string Content, int Rating, int DishId);
    public record CreateCommentDto(string Content, int Rating);
    public record UpdateCommentDto(string Content, int Rating);
}
