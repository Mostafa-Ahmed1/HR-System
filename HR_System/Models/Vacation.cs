using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;


namespace HR_System.Models
{
    public partial class Vacation
    {
        public int VacId { get; set; }

        [Required(ErrorMessage = "*")]

        public DateTime VacationDate { get; set; }

        [Required(ErrorMessage = "*")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Invalid Vacation Name")]
        public string? VacationName { get; set; }
    }
}
