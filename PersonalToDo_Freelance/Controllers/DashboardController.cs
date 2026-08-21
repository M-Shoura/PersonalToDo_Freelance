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
    }
}
