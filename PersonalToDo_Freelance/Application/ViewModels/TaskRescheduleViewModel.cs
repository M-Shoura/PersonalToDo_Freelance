using System;
using System.ComponentModel.DataAnnotations;

namespace PersonalToDo_Freelance.Application.ViewModels
{
    public class TaskRescheduleViewModel
    {
        public long Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime NewDueDate { get; set; }
    }
}
