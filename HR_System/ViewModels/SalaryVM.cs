using HR_System.Models;
namespace HR_System.ViewModels;
public class SalaryVM
{
    public string employeeName { get; set; }
    public string departmentName { get; set; }
    public int fixedSalary { get; set; }
   
    public int attendenceDays {  get; set; }

    public int abscenseDays {  get; set; }


    public decimal BonusHours { get; set; }

    public decimal MinusHours { get; set; }

    public decimal TotalBonus { get; set; }
    public decimal TotalMinus { get; set; }

    public decimal NetSalary { get; set; }








}
