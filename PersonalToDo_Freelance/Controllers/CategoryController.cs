using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Application.ViewModels;

namespace PersonalToDo_Freelance.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _service.GetAllAsync();
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoryCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var (succeeded, error) = await _service.CreateAsync(model);
            if (!succeeded)
            {
                ModelState.AddModelError(string.Empty, error ?? "");
                return View(model);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var vm = await _service.GetForEditAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryEditViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var (succeeded, error) = await _service.UpdateAsync(model);
            if (!succeeded)
            {
                ModelState.AddModelError(string.Empty, error ?? "");
                return View(model);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var (succeeded, error) = await _service.DeleteAsync(id);
            if (!succeeded)
            {
                TempData["Error"] = error;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
