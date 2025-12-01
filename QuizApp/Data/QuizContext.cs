using Microsoft.EntityFrameworkCore;
using QuizApp.Models;

namespace QuizApp.Data
{
    public class QuizContext : DbContext
    {
        public QuizContext(DbContextOptions<QuizContext> options) : base(options) { }

        public DbSet<Quiz> Quizzes { get; set; } = default!;
        public DbSet<Question> Questions { get; set; } = default!;
        public DbSet<Option> Options { get; set; } = default!;
    
    }
}


