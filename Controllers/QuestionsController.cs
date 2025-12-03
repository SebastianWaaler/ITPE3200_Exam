using Microsoft.AspNetCore.Mvc;                                   // MVC base classes / attributes
using QuizApp.Models;                                             // Question, Quiz, Option
using System.Threading.Tasks;                                     // Task/async
using System.Linq;
using System.Collections.Generic;                                 // List<>
using Microsoft.Extensions.Logging;                               // ILogger
using QuizApp.Data.Repositories.Interfaces;                       // IQuestionRepository, IQuizRepository
using Microsoft.AspNetCore.Authorization;                         // [Authorize]

namespace QuizApp.Controllers
{
    // ADMIN ONLY: Only Admins can manage questions
    [Authorize(Roles = "Admin")]
    public class QuestionsController : Controller
    {
        private readonly IQuestionRepository _questions;          // Repository for Question
        private readonly IQuizRepository _quizzes;                // Repository for Quiz
        private readonly ILogger<QuestionsController> _logger;    // Logger for this controller

        public QuestionsController(
            IQuestionRepository questions,
            IQuizRepository quizzes,
            ILogger<QuestionsController> logger)
        {
            _questions = questions;
            _quizzes = quizzes;
            _logger = logger;
        }

        // GET: /Questions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details called with null id.");
                return NotFound();
            }

            try
            {
                var question = await _questions.GetByIdAsync(id.Value);   // via repository (includes Quiz + Options)

                if (question == null)
                {
                    _logger.LogWarning("Question {QuestionId} not found in Details.", id);
                    return NotFound();
                }

                return View(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Details for Question {QuestionId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: /Questions/Create?quizId=5
        [HttpGet]
        public async Task<IActionResult> Create(int quizId)
        {
            try
            {
                var quiz = await _quizzes.GetByIdAsync(quizId);     // load quiz via repository
                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found in Create (GET).", quizId);
                    return NotFound();
                }

                // Prepare an empty Question for the form
                ViewBag.Quiz = quiz;

                return View(new Question
                {
                    QuizId = quizId,
                    Points = 1 // default points
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing Create question form for Quiz {QuizId}.", quizId);
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: /Questions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Question question, int? CorrectIndex)
        {
            try
            {
                var quiz = await _quizzes.GetByIdAsync(question.QuizId);
                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found in Create (POST).", question.QuizId);
                    ModelState.AddModelError("", $"No quiz found with ID: {question.QuizId}");
                }
                else
                {
                    question.Quiz = quiz;
                }

                // Ensure Options list exists
                question.Options ??= new List<Option>();

                // Remove empty options, trim text
                question.Options = question.Options
                    .Where(o => !string.IsNullOrWhiteSpace(o.Text))
                    .Select(o => new Option
                    {
                        Text = o.Text.Trim(),
                        IsCorrect = false   // set later
                    })
                    .ToList();

                // Must have at least 2 options
                if (question.Options.Count < 2)
                {
                    _logger.LogWarning("Validation failed in Create: less than 2 options for Quiz {QuizId}.", question.QuizId);
                    ModelState.AddModelError("", "Please enter at least two answer options.");
                }

                // A correct answer must be selected
                if (CorrectIndex == null || CorrectIndex < 0 || CorrectIndex >= question.Options.Count)
                {
                    _logger.LogWarning("Validation failed in Create: no correct option selected for Quiz {QuizId}.", question.QuizId);
                    ModelState.AddModelError("", "Please select which answer is correct.");
                }
                else
                {
                    question.Options[(int)CorrectIndex].IsCorrect = true;
                }

                // Redisplay form if validation failed
                if (!ModelState.IsValid)
                {
                    ViewBag.Quiz = await _quizzes.GetByIdAsync(question.QuizId);
                    return View(question);
                }

                // Link options to the question
                foreach (var o in question.Options)
                    o.Question = question;

                // Save via repository
                await _questions.AddAsync(question);

                _logger.LogInformation("Question {QuestionId} created for Quiz {QuizId}.", question.Id, question.QuizId);

                return RedirectToAction(nameof(QuizController.Details), "Quiz", new { id = question.QuizId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Question for Quiz {QuizId}.", question.QuizId);
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: /Questions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit (GET) called with null id.");
                return NotFound();
            }

            try
            {
                var question = await _questions.GetByIdAsync(id.Value);   // includes Quiz + Options

                if (question == null)
                {
                    _logger.LogWarning("Question {QuestionId} not found in Edit (GET).", id);
                    return NotFound();
                }

                return View(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit form for Question {QuestionId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: /Questions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Text,Points,QuizId")] Question question)
        {
            if (id != question.Id)
            {
                _logger.LogWarning("Edit (POST) called with mismatched id. Route id: {RouteId}, Model id: {ModelId}", id, question.Id);
                return NotFound();
            }

            try
            {
                // Validate parent still exists
                if (!await _quizzes.ExistsAsync(question.QuizId))
                {
                    _logger.LogWarning("Quiz {QuizId} not found in Edit (POST).", question.QuizId);
                    ModelState.AddModelError("", "Selected quiz does not exist.");
                }

                if (!ModelState.IsValid)
                    return View(question);

                await _questions.UpdateAsync(question);            // UPDATE via repository

                _logger.LogInformation("Question {QuestionId} updated for Quiz {QuizId}.", question.Id, question.QuizId);

                return RedirectToAction(nameof(QuizController.Details), "Quiz", new { id = question.QuizId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing Question {QuestionId}.", question.Id);
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: /Questions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete (GET) called with null id.");
                return NotFound();
            }

            try
            {
                var question = await _questions.GetByIdAsync(id.Value);   // includes Quiz

                if (question == null)
                {
                    _logger.LogWarning("Question {QuestionId} not found in Delete (GET).", id);
                    return NotFound();
                }

                return View(question);           // Views/Questions/Delete.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing Delete confirmation for Question {QuestionId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: /Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var question = await _questions.GetByIdAsync(id);         // load with Options + Quiz

                if (question == null)
                {
                    _logger.LogWarning("Question {QuestionId} not found in DeleteConfirmed.", id);
                    return NotFound();
                }

                var quizId = question.QuizId;

                await _questions.DeleteAsync(id);                         // DELETE via repository

                _logger.LogInformation("Question {QuestionId} deleted for Quiz {QuizId}.", id, quizId);

                return RedirectToAction(nameof(QuizController.Details), "Quiz", new { id = quizId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Question {QuestionId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
