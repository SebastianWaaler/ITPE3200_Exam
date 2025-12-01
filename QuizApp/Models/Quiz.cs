using System.ComponentModel.DataAnnotations;
namespace QuizApp.Models
{
    public class Quiz
    {
        public int QuizId { get; set; }

        [Required, StringLength(100)]
        public String Title { get; set; } = "";
        
        [StringLength(500)] public String? Description { get; set; } = "";
        public List<Question> Questions { get; set; } = new();
    }

}