using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Application.ViewModels;

namespace PersonalToDo_Freelance.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly ICategoryService _categoryService;

        public TaskController(ITaskService taskService, ICategoryService categoryService)
        {
            _taskService = taskService;
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var cats = await _categoryService.GetAllAsync();
            ViewData["Categories"] = cats;
            return View(new TaskCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskCreateViewModel model)
        {
            var cats = await _categoryService.GetAllAsync();
            ViewData["Categories"] = cats;
            if (!ModelState.IsValid) return View(model);
            if (model.StartDate.HasValue && model.DueDate.HasValue && model.StartDate > model.DueDate)
            {
                ModelState.AddModelError(string.Empty, "Start date cannot be after due date.");
                return View(model);
            }
            var (succeeded, error, id) = await _taskService.CreateAsync(model);
            if (!succeeded)
            {
                ModelState.AddModelError(string.Empty, error ?? "");
                return View(model);
            }
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
