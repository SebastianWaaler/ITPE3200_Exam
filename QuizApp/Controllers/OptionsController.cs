using Microsoft.AspNetCore.Mvc;                                   // MVC base/types
using Microsoft.EntityFrameworkCore;                              // EF Core APIs
using QuizApp.Data;                                               // QuizContext
using QuizApp.Models;                                             // Option, Question
using System.Threading.Tasks;                                     // Task/async
using System.Linq;                                                // LINQ

namespace QuizApp.Controllers
{
    public class OptionsController : Controller
    {
        private readonly QuizContext _context;

        public OptionsController(QuizContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ByQuestion(int questionId) // List options for a question
        {
            var question = await _context.Questions
                .Include(q => q.Quiz)                               // Include quiz for breadcrumbs
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
                return NotFound();

            var options = await _context.Options
                .Where(o => o.QuestionId == questionId)             // Filter by FK
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Question = question;                            // Pass parent to view (title/breadcrumbs)
            return View(options);                                    // Render ByQuestion.cshtml
        }

        public async Task<IActionResult> Create(int questionId)     // Show create form for an option under a question
        {
            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
                return NotFound();

            ViewBag.Question = question;                             // For displaying context in the view
            return View(new Option { QuestionId = questionId });     // Pre-fill FK for binding
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Text,IsCorrect,QuestionId")] Option option)
        {                                                            // Bind editable fields + FK
            var questionExists = await _context.Questions
                .AnyAsync(q => q.Id == option.QuestionId);           // Validate FK

            if (!questionExists)
                ModelState.AddModelError("", "Selected question does not exist.");

            if (!ModelState.IsValid)                                 // Validation errors? redisplay
            {
                ViewBag.Question = await _context.Questions
                    .Include(q => q.Quiz)
                    .FirstOrDefaultAsync(q => q.Id == option.QuestionId);
                return View(option);
            }

            _context.Options.Add(option);                            // Stage INSERT
            await _context.SaveChangesAsync();                       // Execute INSERT
            return RedirectToAction(nameof(ByQuestion), new { questionId = option.QuestionId }); // Back to options list
        }

        public async Task<IActionResult> Edit(int? id)               // Show edit form
        {
            if (id == null)
                return NotFound();

            var option = await _context.Options
                .Include(o => o.Question)
                    .ThenInclude(q => q.Quiz)                        // For breadcrumbs in the view
                .FirstOrDefaultAsync(o => o.Id == id);

            if (option == null)
                return NotFound();

            return View(option);                                     // Render Edit.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Text,IsCorrect,QuestionId")] Option option)
        {                                                            // Bind key + editable fields
            if (id != option.Id)
                return NotFound();

            if (!await _context.Questions.AnyAsync(q => q.Id == option.QuestionId))
                ModelState.AddModelError("", "Selected question does not exist.");

            if (!ModelState.IsValid)
                return View(option);

            try
            {
                _context.Update(option);                             // Stage UPDATE
                await _context.SaveChangesAsync();                   // Execute UPDATE
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Options.AnyAsync(e => e.Id == option.Id))
                    return NotFound();                               // Deleted concurrently
                else
                    throw;
            }

            return RedirectToAction(nameof(ByQuestion), new { questionId = option.QuestionId }); // Back to options
        }

        public async Task<IActionResult> Delete(int? id)             // Confirm delete
        {
            if (id == null)
                return NotFound();

            var option = await _context.Options
                .Include(o => o.Question)
                    .ThenInclude(q => q.Quiz)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (option == null)
                return NotFound();

            return View(option);                                      // Render Delete.cshtml
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var option = await _context.Options
                .FirstOrDefaultAsync(o => o.Id == id);                // Fetch to track for removal

            if (option == null)
                return NotFound();

            var questionId = option.QuestionId;                       // Save FK for redirect
            _context.Options.Remove(option);                          // Stage DELETE
            await _context.SaveChangesAsync();                        // Execute DELETE
            return RedirectToAction(nameof(ByQuestion), new { questionId }); // Back to options list
        }
    }
}