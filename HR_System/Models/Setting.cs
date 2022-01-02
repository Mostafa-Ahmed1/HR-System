using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_System.Models
{
    public partial class Setting
    {
        public int SettingId { get; set; }

        [Required(ErrorMessage ="*")]
        [RegularExpression("^[0-9]*$",ErrorMessage ="this field take numbers ")]
        public float? PlusPerhour { get; set; }

        [Required(ErrorMessage = "*")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "this field take numbers ")]

        public float? MinusPerhour { get; set; }

        [Required(ErrorMessage = "*")]
        public string? Dayoff1 { get; set; }

        [Required(ErrorMessage = "*")]

        public string? Dayoff2 { get; set; }
    }
}
