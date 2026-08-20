using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PersonalToDo_Freelance.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly Application.Interfaces.ITaskService _taskService;

        public DashboardController(Application.Interfaces.ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _taskService.GetUserTasksAsync();
            return View(items);
        }
    }
}
