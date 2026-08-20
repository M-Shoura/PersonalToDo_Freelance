using System.ComponentModel.DataAnnotations;

namespace PersonalToDo_Freelance.Application.ViewModels
{
    public class CategoryListItemViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class CategoryCreateViewModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(4000)]
        public string? Description { get; set; }
    }

    public class CategoryEditViewModel
    {
        public long Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(4000)]
        public string? Description { get; set; }

        public bool IsDeleted { get; set; }
    }
}
