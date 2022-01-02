using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HR_System.Models
{
    public partial class User
    {
        public int UserId { get; set; }

        [StringLength(100, MinimumLength = 3, ErrorMessage = "Enter 3  or More Characters !")]
        [Required(ErrorMessage = "* User Name Is Required !")]
        [Display(Name = "User Name")]
        //[RegularExpression(@"^[^\W\d_]+$" , ErrorMessage ="Enter Characters Only")]
        public string Username { get; set; } = null!;
        [Required(ErrorMessage = "* Email Is Required")]
        [EmailAddress(ErrorMessage = "Enter A Valid Email Address !")]

        public string Email { get; set; } = null!;
        [Required(ErrorMessage = "* Password Is Required !")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "Enter 8  or More Characters")]
        public string Password { get; set; } = null!;
        public int? GroupId { get; set; }

        public virtual Group? Group { get; set; }
    }
}
