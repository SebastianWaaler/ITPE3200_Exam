using Microsoft.AspNetCore.Mvc;                               // Brings in MVC attributes and base classes like Controller
using Microsoft.EntityFrameworkCore;                          // Brings in EF Core extension methods like Include(), ToListAsync(), etc.
using QuizApp.Data;                                           // Gives access to QuizContext (your DbContext)
using QuizApp.Models;                                         // Gives access to your Quiz, Question, Option models
using System.Threading.Tasks;                                 // For Task / async support
using System.Linq;

namespace QuizApp.Controllers
{
    public class QuizController : Controller
    {
        private readonly QuizContext _context;

        public QuizController(QuizContext context)              // Constructor where the framework injects the QuizContext
        {
            _context = context;                                 // Save the injected context so we can query/update the DB in actions
        }

        // GET Quizzes
        public async Task<IActionResult> Index()                // Action method that returns a view listing all quizzes
        {
            var quizzes = await _context.Quizzes                // Start a query against the Quizzes DbSet
            .AsNoTracking().                                    // Faster read-only query (EF won’t track returned entities)
            ToListAsync();                                      // Execute SQL and return results as a list (async)

            return View(quizzes);                               // Pass the list to the Index.cshtml view
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null!)
                return NotFound();

            var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            return View(quiz);                                  // Render Details.cshtml with the quiz model
        }

        // GET: /Quizzes/Create
        public IActionResult Create()                         // Shows the empty create form
        {
            return View();                                    // Returns Create.cshtml (strongly-typed to Quiz)
        }

        // POST: /Quizzes/Create/
        [HttpPost]                                            // This action handles form POST requests
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title, Description")] Quiz quiz) // The [Bind] limits which properties are bound from the form
        {
            if (ModelState.IsValid)
            {
                _context.Add(quiz);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(quiz);
        }

        // GET: /Quizzes/Edit/
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
                return NotFound();

            return View("~/Views/Quiz/Edit.cshtml", quiz);
        }

        // POST: /Quizzes/Edit/5
        [HttpPost]                                            // Handles POST for edit
        [ValidateAntiForgeryToken]                            // Protects against CSRF
        public async Task<IActionResult> Edit(int id, [Bind("QuizId,Title,Description")] Quiz quiz) // We bind key + editable fields (not Questions list)
        {
            if (id != quiz.QuizId)                            // Route id must match form’s key value
                return NotFound();

            if (!ModelState.IsValid)                          // If validation failed, redisplay form
                return View(quiz);

            try
            {
                _context.Update(quiz);
                await _context.SaveChangesAsync();
            }

            catch (DbUpdateConcurrencyException)
            {
                if (!QuizExists(quiz.QuizId))
                    return NotFound();
                else
                    throw;

            }
            return RedirectToAction(nameof(Index));

        }

        // GET: /Quiz/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var quiz = await _context.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null) 
                return NotFound();

            return View(quiz);  // Views/Quiz/Delete.cshtml
        }



        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Take(int id)
        {
            var quiz = _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefault(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            return View(quiz);
        }
        [HttpPost]
        public IActionResult Submit(int QuizId)
        {
            // Load quiz including questions and options
            var quiz = _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefault(q => q.QuizId == QuizId);

            if (quiz == null)
                return NotFound();

            int totalPoints = 0;
            int earnedPoints = 0;

            foreach (var question in quiz.Questions)
            {
                totalPoints += question.Points;

                // Name of radio button group: question_QUESTIONID
                string formKey = $"question_{question.Id}";

                // Did the user answer this question?
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

            // Pass results to the view
            var result = new QuizResultViewModel
            {
                QuizTitle = quiz.Title,
                TotalPoints = totalPoints,
                EarnedPoints = earnedPoints
            };

            return View("QuizResult", result);
        }






        private bool QuizExists(int id)                       // Helper used by Edit’s concurrency catch
            => _context.Quizzes.Any(e => e.QuizId == id); 
    }
}

