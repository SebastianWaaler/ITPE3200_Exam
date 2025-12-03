using System;                                                  // Exception, etc.
using System.Linq;
using System.Threading.Tasks;                                  // Task / async
using Microsoft.AspNetCore.Authorization;                      // [Authorize], [AllowAnonymous]
using Microsoft.AspNetCore.Mvc;                                // MVC base classes / attributes
using Microsoft.EntityFrameworkCore;                           // For DbUpdateConcurrencyException
using Microsoft.Extensions.Logging;                            // ILogger<T>
using QuizApp.Data.Repositories.Interfaces;                    // IQuizRepository
using QuizApp.Models;                                          // Quiz, Question, Option models

namespace QuizApp.Controllers
{
    // Require login by default for this controller
    [Authorize]
    public class QuizController : Controller
    {
        private readonly IQuizRepository _quizzes;             // Repository instead of DbContext
        private readonly ILogger<QuizController> _logger;       // Logger for this controller

        public QuizController(IQuizRepository quizzes, ILogger<QuizController> logger)
        {
            _quizzes = quizzes;
            _logger = logger;
        }

        // Everyone (even not logged in) can see list of quizzes
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                var quizzes = await _quizzes.GetAllAsync();
                return View(quizzes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading quiz list in Index.");
                return RedirectToAction("Error", "Home");
            }
        }

        // Everyone can see details
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details called with null id.");
                return NotFound();
            }

            try
            {
                var quiz = await _quizzes.GetByIdAsync(id.Value); // quiz with questions + options

                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found in Details.", id);
                    return NotFound();
                }

                return View(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Details for Quiz {QuizId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // 🔒 Admin only: create quiz (GET)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // 🔒 Admin only: create quiz (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Title, Description")] Quiz quiz)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _quizzes.AddAsync(quiz);

                    _logger.LogInformation("Quiz {QuizId} created.", quiz.QuizId);

                    return RedirectToAction(nameof(Index));
                }

                return View(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quiz.");
                return RedirectToAction("Error", "Home");
            }
        }

        // 🔒 Admin only: edit quiz (GET)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit (GET) called with null id.");
                return NotFound();
            }

            try
            {
                var quiz = await _quizzes.GetByIdAsync(id.Value);
                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found in Edit (GET).", id);
                    return NotFound();
                }

                return View("~/Views/Quiz/Edit.cshtml", quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit form for Quiz {QuizId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // 🔒 Admin only: edit quiz (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("QuizId,Title,Description")] Quiz quiz)
        {
            if (id != quiz.QuizId)
            {
                _logger.LogWarning("Edit (POST) called with mismatched id. Route id: {RouteId}, Model id: {ModelId}", id, quiz.QuizId);
                return NotFound();
            }

            if (!ModelState.IsValid)
                return View(quiz);

            try
            {
                await _quizzes.UpdateAsync(quiz);

                _logger.LogInformation("Quiz {QuizId} updated.", quiz.QuizId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await _quizzes.ExistsAsync(quiz.QuizId))
                {
                    _logger.LogWarning(ex, "Concurrency error: Quiz {QuizId} no longer exists.", quiz.QuizId);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating Quiz {QuizId}.", quiz.QuizId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing Quiz {QuizId}.", quiz.QuizId);
                return RedirectToAction("Error", "Home");
            }

            return RedirectToAction(nameof(Index));
        }

        // 🔒 Admin only: delete quiz (GET)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete (GET) called with null id.");
                return NotFound();
            }

            try
            {
                var quiz = await _quizzes.GetByIdAsync(id.Value);

                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found in Delete (GET).", id);
                    return NotFound();
                }

                return View(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing Delete confirmation for Quiz {QuizId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // 🔒 Admin only: delete quiz (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var quiz = await _quizzes.GetByIdAsync(id);

                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found in DeleteConfirmed.", id);
                    return NotFound();
                }

                await _quizzes.DeleteAsync(id);

                _logger.LogInformation("Quiz {QuizId} deleted.", id);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Quiz {QuizId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // 🔐 Login required to take quiz
        [Authorize]
        public async Task<IActionResult> Take(int id)
        {
            try
            {
                var quiz = await _quizzes.GetByIdAsync(id);

                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found in Take.", id);
                    return NotFound();
                }

                return View(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Take view for Quiz {QuizId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // 🔐 Login required to submit answers
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Submit(int QuizId)
        {
            try
            {
                var quiz = await _quizzes.GetByIdAsync(QuizId);

                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found in Submit.", QuizId);
                    return NotFound();
                }

                int totalPoints = 0;
                int earnedPoints = 0;

                foreach (var question in quiz.Questions)
                {
                    totalPoints += question.Points;

                    string formKey = $"question_{question.Id}";

                    if (!Request.Form.ContainsKey(formKey))
                        continue;

                    int selectedOptionId = int.Parse(Request.Form[formKey]!);

                    var selectedOption =
                        question.Options.First(o => o.Id == selectedOptionId);

                    if (selectedOption.IsCorrect)
                    {
                        earnedPoints += question.Points;
                    }
                }

                var result = new QuizResultViewModel
                {
                    QuizTitle = quiz.Title,
                    TotalPoints = totalPoints,
                    EarnedPoints = earnedPoints
                };

                _logger.LogInformation("Quiz {QuizId} submitted. Score: {Earned}/{Total}.",
                    QuizId, earnedPoints, totalPoints);

                return View("QuizResult", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting Quiz {QuizId}.", QuizId);
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
