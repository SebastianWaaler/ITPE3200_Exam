using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuizApp.Data.Repositories.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuizApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]   // => /api/QuizApi
    public class QuizApiController : ControllerBase
    {
        private readonly IQuizRepository _quizzes;
        private readonly ILogger<QuizApiController> _logger;

        public QuizApiController(IQuizRepository quizzes, ILogger<QuizApiController> logger)
        {
            _quizzes = quizzes;
            _logger = logger;
        }

        // GET /api/QuizApi
        // List all quizzes (for the Index page)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var quizzes = await _quizzes.GetAllAsync();

                var dto = quizzes.Select(quiz => new
                {
                    quiz.QuizId,
                    quiz.Title,
                    quiz.Description
                });

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QuizApiController.GetAll()");
                return StatusCode(500, "Server error");
            }
        }

        // GET /api/QuizApi/5
        // Single quiz with questions + options (for Take page)
        [HttpGet("{id}")]
        [Authorize] // must be logged in to load quiz via API
        public async Task<IActionResult> GetQuiz(int id)
        {
            try
            {
                var quiz = await _quizzes.GetByIdAsync(id);

                if (quiz == null)
                    return NotFound();

                // JSON-friendly projection
                var dto = new
                {
                    quiz.QuizId,
                    quiz.Title,
                    quiz.Description,
                    Questions = quiz.Questions.Select(q => new
                    {
                        q.Id,
                        q.Text,
                        q.Points,
                        Options = q.Options.Select(o => new
                        {
                            o.Id,
                            o.Text,
                            o.IsCorrect
                        })
                    })
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QuizApiController.GetQuiz({Id})", id);
                return StatusCode(500, "Server error");
            }
        }
    }
}
