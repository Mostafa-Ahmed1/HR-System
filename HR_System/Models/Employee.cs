using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HR_System.Models
{
    public partial class Employee
    {
        public Employee()
        {
            AttDeps = new HashSet<AttDep>();
        }
        [Required(ErrorMessage ="*")]
        public int EmpId { get; set; }


        [Required(ErrorMessage = "*")]
        [StringLength(100,MinimumLength =3,ErrorMessage ="Name must be between 3 and 100 characters")]
        public string EmpName { get; set; } = null!;


        [Required(ErrorMessage = "*")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Address must be between 3 and 150 characters")]
        public string Address { get; set; } = null!;

        [StringLength(11)]
        [Required(ErrorMessage = "*")]
        [RegularExpression("^01[0-2]{1}[0-9]{8}", ErrorMessage ="Please Insert a valid Phone Number")]
        //[Phone]
        public string Phone { get; set; }


        public string Gender { get; set; } = null!;


        [Required(ErrorMessage = "*")]
        [RegularExpression("^[a-zA-Z ]*$",ErrorMessage ="Invalid Nationality")]
        public string Nationality { get; set; } = null!;


        [Required(ErrorMessage = "*")]
        [Remote("birthdatecheck", "employees",ErrorMessage ="Employee age must be greatear than 20")]
        public DateTime Birthdate { get; set; }


        [Required(ErrorMessage ="*")]
        [RegularExpression("^[0-9]{14}$",ErrorMessage ="Invalid.. must be 14 degit number")]
        public string NationalId { get; set; } = null!;


        [Required(ErrorMessage = "*")]
        [Remote("hiredatecheck","Employees",ErrorMessage ="Please Insert a Valid Hire Date")]
        public DateTime Hiredate { get; set; }


        [Required(ErrorMessage = "*")]
        public int FixedSalary { get; set; }

        //[Remote("DeptTimeCheck","employees")]
        [Required(ErrorMessage = "*")]
        public TimeSpan AttTime { get; set; }


        [Required(ErrorMessage = "*")]
        //[Remote("DeptTimeCheck", "employees", ErrorMessage = "Departure time Must be after attendance time!")]
        public TimeSpan DepartureTime { get; set; }
        public int? DeptId { get; set; }

        public virtual Department? Dept { get; set; }
        public virtual ICollection<AttDep> AttDeps { get; set; }
    }
}
