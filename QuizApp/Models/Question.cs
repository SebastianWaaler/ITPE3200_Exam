using System.ComponentModel.DataAnnotations;

namespace QuizApp.Models
{
    public class Question
    { 
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Text { get; set; } = "";

        [Range(0, 5)]
        public int Points { get; set; } = 1;

        public int QuizId { get; set; }
        public Quiz? Quiz { get; set; }

        public List<Option> Options { get; set; } = new();
    }
}
