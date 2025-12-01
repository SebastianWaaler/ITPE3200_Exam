using Microsoft.AspNetCore.Mvc;                                   // MVC base classes / attributes
using Microsoft.EntityFrameworkCore;                              // EF Core APIs
using QuizApp.Data;                                               // QuizContext
using QuizApp.Models;                                             // Question, Quiz, Option
using System.Threading.Tasks;                                     // Task/async
using System.Linq;
using System.IO.Compression;


namespace QuizApp.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly QuizContext _context;

        public QuestionsController(QuizContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Details(int? id)          // Show a single question (with options)
        {
            if (id == null)
                return NotFound();

            var question = await _context.Questions
                .Include(q => q.Quiz)                        // Include parent for breadcrumbs
                .Include(q => q.Options)                            // Include options to display
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound();

            return View(question);                                  // Render Details.cshtml
        }

        [HttpGet]
        public async Task<IActionResult> Create(int quizId)
        {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz == null)
            return NotFound();

        // Prepare an empty Question for the form
        ViewBag.Quiz = quiz;

        return View(new Question
        {
            QuizId = quizId,
            Points = 1    // optional: default points
        });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Question question, int? CorrectIndex)
    {
      
    
        var quiz = await _context.Quizzes.FindAsync(question.QuizId);
        if (quiz == null) {
            ModelState.AddModelError("", $"No quiz found with ID: {question.QuizId}");        
        } else {
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
        if (question.Options.Count < 2) {
            ModelState.AddModelError("", "Please enter at least two answer options.");
        }

        // A correct answer must be selected
        if (CorrectIndex == null || CorrectIndex < 0 || CorrectIndex >= question.Options.Count) {
            Console.WriteLine("Adding correct erro");
            ModelState.AddModelError("", "Please select which answer is correct.");
        }
        else {
            question.Options[(int)CorrectIndex].IsCorrect = true;
        }


        // Redisplay form if validation failed
        if (!ModelState.IsValid)
        {
            ViewBag.Quiz = await _context.Quizzes.FindAsync(question.QuizId);
            return View(question);
        }

        // Link options to the question
        foreach (var o in question.Options)
            o.Question = question;

        // Save
        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(QuizController.Details), "Quiz", new { id = question.QuizId });
    }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var question = await _context.Questions
                .Include(q => q.Quiz)
                .Include(q => q.Options)   
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound();

            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Text,Points,QuizId")] Question question)
        {                                                           // Bind key + editable fields
            if (id != question.Id)
                return NotFound();

            // Validate parent still exists
            if (!await _context.Quizzes.AnyAsync(q => q.QuizId == question.QuizId))
                ModelState.AddModelError("", "Selected quiz does not exist.");

            if (!ModelState.IsValid)
                return View(question);

            try
            {
                _context.Update(question);                          // Mark for UPDATE
                await _context.SaveChangesAsync();                  // Execute UPDATE
            }
            catch (DbUpdateConcurrencyException)                    // Concurrency handling
            {
                if (!await _context.Questions.AnyAsync(e => e.Id == question.Id))
                    return NotFound();                              // Deleted by someone else
                else
                    throw;
            }

            return RedirectToAction(nameof(QuizController.Details), "Quiz", new { id = question.QuizId });
        }

        // GET: /Questions/Delete/
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var question = await _context.Questions
                .Include(q => q.Quiz)        // for breadcrumb/title on the page
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound();

            return View(question);           // Views/Questions/Delete.cshtml
        }

        // POST: /Questions/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Load with children so we can handle FK behavior explicitly if needed
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound();

            var quizId = question.QuizId;

            // If your FK Question->Options is not configured for Cascade Delete,
            // uncomment the next line to delete children first:
            // _context.Options.RemoveRange(question.Options);

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            // Back to that quiz’s details
         return RedirectToAction(nameof(QuizController.Details), "Quiz", new { id = quizId });

        }


        
    }


}