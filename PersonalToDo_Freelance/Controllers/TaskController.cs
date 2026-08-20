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
        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var vm = await _taskService.GetDetailsAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var vm = await _taskService.GetForEditAsync(id);
            if (vm == null) return NotFound();
            var cats = await _categoryService.GetAllAsync();
            ViewData["Categories"] = cats;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaskEditViewModel model)
        {
            var cats = await _categoryService.GetAllAsync();
            ViewData["Categories"] = cats;
            if (!ModelState.IsValid) return View(model);
            if (model.StartDate.HasValue && model.DueDate.HasValue && model.StartDate > model.DueDate)
            {
                ModelState.AddModelError(string.Empty, "Start date cannot be after due date.");
                return View(model);
            }
            var (succeeded, error) = await _taskService.UpdateAsync(model);
            if (!succeeded)
            {
                ModelState.AddModelError(string.Empty, error ?? "");
                return View(model);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var (succeeded, error) = await _taskService.DeleteAsync(id);
            if (!succeeded) TempData["Error"] = error;
            return RedirectToAction("Index", "Dashboard");
        }
        [HttpGet]
        public async Task<IActionResult> Reschedule(long id)
        {
            var vm = await _taskService.GetForEditAsync(id);
            if (vm == null) return NotFound();
            var model = new Application.ViewModels.TaskRescheduleViewModel { Id = id, NewDueDate = DateTime.UtcNow.Date.AddDays(1) };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(Application.ViewModels.TaskRescheduleViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var (succeeded, error) = await _taskService.RescheduleAsync(model.Id, model.NewDueDate);
            if (!succeeded)
            {
                ModelState.AddModelError(string.Empty, error ?? "");
                return View(model);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> BulkReschedule()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkReschedule(Application.ViewModels.TaskRescheduleViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var count = await _taskService.BulkRescheduleOverdueAsync(model.NewDueDate);
            TempData["Message"] = $"Rescheduled {count} overdue tasks.";
            return RedirectToAction("Index", "Dashboard");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(long id, PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus status)
        {
            var (succeeded, error) = await _taskService.ChangeStatusAsync(id, status);
            if (!succeeded) TempData["Error"] = error;
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
