using System;                                                   // Exception, etc.
using System.Threading.Tasks;                                   // Task / async
using Microsoft.AspNetCore.Authorization;                       // [Authorize]
using Microsoft.AspNetCore.Mvc;                                 // MVC base/types
using Microsoft.EntityFrameworkCore;                            // DbUpdateConcurrencyException
using Microsoft.Extensions.Logging;                             // ILogger
using QuizApp.Data.Repositories.Interfaces;                     // IOptionRepository, IQuestionRepository
using QuizApp.Models;                                           // Option, Question

namespace QuizApp.Controllers
{
    // ADMIN ONLY: Only Admins can manage options
    [Authorize(Roles = "Admin")]
    public class OptionsController : Controller
    {
        private readonly IOptionRepository _options;              // Option repository
        private readonly IQuestionRepository _questions;          // Question repository
        private readonly ILogger<OptionsController> _logger;      // Logger for this controller

        public OptionsController(
            IOptionRepository options,
            IQuestionRepository questions,
            ILogger<OptionsController> logger)
        {
            _options = options;
            _questions = questions;
            _logger = logger;
        }

        // GET: /Options/ByQuestion/5
        public async Task<IActionResult> ByQuestion(int questionId)
        {
            try
            {
                // Load parent question via repository
                var question = await _questions.GetByIdAsync(questionId);

                if (question == null)
                {
                    _logger.LogWarning("Question {QuestionId} not found in ByQuestion.", questionId);
                    return NotFound();
                }

                // Load options list via repository
                var options = await _options.GetByQuestionIdAsync(questionId);

                ViewBag.Question = question;
                return View(options);                                   // ByQuestion.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading options for question {QuestionId}.", questionId);
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: /Options/Create?questionId=5
        public async Task<IActionResult> Create(int questionId)
        {
            try
            {
                var question = await _questions.GetByIdAsync(questionId);

                if (question == null)
                {
                    _logger.LogWarning("Question {QuestionId} not found in Create (GET).", questionId);
                    return NotFound();
                }

                ViewBag.Question = question;
                return View(new Option { QuestionId = questionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing Create option form for question {QuestionId}.", questionId);
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: /Options/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Text,IsCorrect,QuestionId")] Option option)
        {
            try
            {
                var questionExists = await _questions.ExistsAsync(option.QuestionId);

                if (!questionExists)
                {
                    _logger.LogWarning("Question {QuestionId} not found in Create (POST).", option.QuestionId);
                    ModelState.AddModelError("", "Selected question does not exist.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Question = await _questions.GetByIdAsync(option.QuestionId);
                    return View(option);
                }

                await _options.AddAsync(option);

                _logger.LogInformation("Option {OptionId} created for Question {QuestionId}.", option.Id, option.QuestionId);

                return RedirectToAction(nameof(ByQuestion), new { questionId = option.QuestionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating option for Question {QuestionId}.", option.QuestionId);
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: /Options/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit called with null id.");
                return NotFound();
            }

            try
            {
                var option = await _options.GetByIdAsync(id.Value);  // includes Question + Quiz via repo

                if (option == null)
                {
                    _logger.LogWarning("Option {OptionId} not found in Edit (GET).", id);
                    return NotFound();
                }

                return View(option);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit form for Option {OptionId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: /Options/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Text,IsCorrect,QuestionId")] Option option)
        {
            if (id != option.Id)
            {
                _logger.LogWarning("Edit (POST) called with mismatched id. Route id: {RouteId}, Model id: {ModelId}", id, option.Id);
                return NotFound();
            }

            try
            {
                if (!await _questions.ExistsAsync(option.QuestionId))
                {
                    _logger.LogWarning("Question {QuestionId} not found in Edit (POST).", option.QuestionId);
                    ModelState.AddModelError("", "Selected question does not exist.");
                }

                if (!ModelState.IsValid)
                    return View(option);

                await _options.UpdateAsync(option);

                _logger.LogInformation("Option {OptionId} updated for Question {QuestionId}.", option.Id, option.QuestionId);

                return RedirectToAction(nameof(ByQuestion), new { questionId = option.QuestionId });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await _options.ExistsAsync(option.Id))
                {
                    _logger.LogWarning(ex, "Concurrency error: Option {OptionId} no longer exists.", option.Id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating Option {OptionId}.", option.Id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing Option {OptionId}.", option.Id);
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: /Options/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete (GET) called with null id.");
                return NotFound();
            }

            try
            {
                var option = await _options.GetByIdAsync(id.Value);  // includes Question + Quiz via repo

                if (option == null)
                {
                    _logger.LogWarning("Option {OptionId} not found in Delete (GET).", id);
                    return NotFound();
                }

                return View(option);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing Delete confirmation for Option {OptionId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // POST: /Options/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var option = await _options.GetByIdAsync(id);

                if (option == null)
                {
                    _logger.LogWarning("Option {OptionId} not found in DeleteConfirmed.", id);
                    return NotFound();
                }

                var questionId = option.QuestionId;

                await _options.DeleteAsync(id);

                _logger.LogInformation("Option {OptionId} deleted for Question {QuestionId}.", id, questionId);

                return RedirectToAction(nameof(ByQuestion), new { questionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Option {OptionId}.", id);
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
