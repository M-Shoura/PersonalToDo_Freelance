using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PersonalToDo_Freelance.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly Application.Interfaces.ITaskService _taskService;
        private readonly Application.Interfaces.ICategoryService _categoryService;

        public DashboardController(Application.Interfaces.ITaskService taskService, Application.Interfaces.ICategoryService categoryService)
        {
            _taskService = taskService;
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index([FromQuery] Application.ViewModels.TaskQueryParameters? q)
        {
            ViewData["Query"] = q ?? new Application.ViewModels.TaskQueryParameters();
            ViewData["Categories"] = await _categoryService.GetAllAsync();
            var model = await _taskService.GetDashboardAsync(DateTime.UtcNow.Date);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> All([FromQuery] Application.ViewModels.TaskQueryParameters? q)
        {
            ViewData["Query"] = q ?? new Application.ViewModels.TaskQueryParameters();
            ViewData["Categories"] = await _categoryService.GetAllAsync();
            var model = await _taskService.GetUserTasksAsync(q);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Statistics(DateTime? start, DateTime? end)
        {
            var s = start ?? DateTime.UtcNow.Date.AddDays(-30);
            var e = end ?? DateTime.UtcNow.Date;
            var vm = await _taskService.GetStatisticsAsync(s, e);
            return View(vm);
        }
    }
}
