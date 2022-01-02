using Microsoft.AspNetCore.Mvc;
using HR_System.Models;
using HR_System.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_System.Controllers;
public class SalaryController : Controller
{
    HrSysContext db;
    public SalaryController(HrSysContext db)
    {
            this.db = db;

     }
    public IActionResult Index()
    {
        var admin_id = HttpContext.Session.GetString("adminId");
        var user_id = HttpContext.Session.GetString("userId");

        if (admin_id != null)
        {
            ViewBag.PagesRules = null;
        }
        else if (user_id != null)
        {
            var b = HttpContext.Session.GetString("groupId");
            if (b != null)
            {
                List<Crud> Rules = db.CRUDs.Where(n => n.GroupId == int.Parse(b)).ToList();
                ViewBag.PagesRules = Rules;

            }
        }
        #region select lists
        List<int> months = new List<int>();
        for (int i = 1; i <= 12; i++)
        {
            months.Add(i);
        }
        ViewBag.months = new SelectList(months);


        List<int> years = new List<int>();
        for (int i = 2008; i <= DateTime.Now.Year; i++)
        {
            years.Add(i);
        }
        ViewBag.years = new SelectList(years);
        #endregion
        return View();

    }
    public IActionResult SalaryTable(int selectedyear,int selectedmonth, string name)
    {
        int month = selectedmonth == 0? 1 : selectedmonth;
        int year = selectedyear == 0?2008 : selectedyear;
        
        List<SalaryVM> salary = new List<SalaryVM>();
        List<Employee> employees = db.Employees.ToList();
        List<AttDep> attendence = db.Att_dep.ToList();

        // get days in month 
        var daysInMonth = DateTime.DaysInMonth(year,month);


        #region Get days of dayoff_oneCount & dayoff_twoCount in  a month 
        int dayoff_oneCount = 0;
        int dayoff_twoCount = 0;
        int m_Month =month;
        int m_Year = year;

        DateTime dt = new DateTime(m_Year, m_Month, 1);
        var dayone = db.Settings.Select(n=>n.Dayoff1).FirstOrDefault();
        var daytwo = db.Settings.Select(n=>n.Dayoff2).FirstOrDefault();


        while (dt.Month == m_Month)
        {
            if (dt.DayOfWeek.ToString() == dayone)
            {
                dayoff_oneCount++;
            }  
            if (dt.DayOfWeek.ToString() == daytwo)
            {
                dayoff_twoCount++;
            }
            dt = dt.AddDays(1);
        }
        #endregion

        // Get official yearly vacations from vacation table
        int yearlyVacs = db.Vacations.Where(n => n.VacationDate.Month == month  && n.VacationDate.Year == year).Count();


        // Get Plus and Minus Work Hours
        decimal plusPerHour = (decimal)db.Settings.Select(r => r.PlusPerhour).FirstOrDefault() ;
        decimal minusPerHour = (decimal)db.Settings.Select(r => r.MinusPerhour).FirstOrDefault();

        foreach (var emp in employees)
        {
            // Employee Fixed Working Hours Per Day
            decimal  workingHoursEmployee = (decimal) (emp.DepartureTime.TotalHours - emp.AttTime.TotalHours);


            // get total bonus hours
            var BonusHours = db.Entry(emp).Collection(n => n.AttDeps)
                .Query()
                .Where(n => n.Date.Year == year && n.Date.Month == month && n.workedHours > workingHoursEmployee)
                .Select(n => n.workedHours).ToList()

                .Sum( n => n - workingHoursEmployee);

            // get Minus bonus hours
            var MinusHours = db.Entry(emp).Collection(n => n.AttDeps)
             .Query()

             .Where(n => n.Date.Year == year && n.Date.Month == month && n.workedHours < workingHoursEmployee)
             .Select(n => n.workedHours).ToList()
             .Sum( n =>  workingHoursEmployee - n );

            // Hourly Rate
            decimal HourlyRate = emp.FixedSalary * 12 / 52 / (5 * workingHoursEmployee);
            

            // Days To Attend
            int DaysToAttend = daysInMonth - dayoff_oneCount - dayoff_twoCount - yearlyVacs;
            
            // Daily Rate
            decimal DailyRate = emp.FixedSalary/daysInMonth;
            
            //Attendance Days
            int AttendanceDays = db.Entry(emp).Collection(n => n.AttDeps).Query().Where(n => n.Date.Month == month && n.Date.Year == year).Count();
            
            // Abscence Days 
            int AbscenceDays = DaysToAttend - AttendanceDays;

            // Total Bounus
            decimal TotalBounus = BonusHours * plusPerHour * HourlyRate;

            //Total Minus
            decimal TotalMinus = MinusHours * minusPerHour * HourlyRate;

            //NetSalary
            decimal NetSalary = emp.FixedSalary + TotalBounus - TotalMinus - AbscenceDays * DailyRate;
            //decimal NetSalary = AttendanceDays * DailyRate + TotalBounus - TotalMinus;
            //decimal DisplayedSal = NetSalary > emp.FixedSalary? emp.FixedSalary : NetSalary;

            salary.Add(new SalaryVM()
            {
                fixedSalary = emp.FixedSalary,
                employeeName = emp.EmpName,

                departmentName = emp.Dept.DeptName ,
                attendenceDays = AttendanceDays,
                abscenseDays = AbscenceDays,
                BonusHours = Math.Floor(BonusHours),
                MinusHours = Math.Floor(MinusHours),
                TotalBonus = Math.Floor(TotalBounus),
                TotalMinus = Math.Floor(TotalMinus),
                NetSalary = Math.Floor(NetSalary)
            });
        }
        if (name != null)
        {
            var filteredSalary = salary.Where(n => n.employeeName.Contains(name)).ToList();
            return PartialView(filteredSalary);
        }
        return PartialView(salary);

    }
    public IActionResult invoice(String empName, String departmentName,int fixedSalary,int attendenceDays,int abscenseDays, double BonusHours, double MinusHours, double TotalBonus, double TotalMinus, double NetSalary)
    { 
        ViewBag.empName= empName;
        ViewBag.fixedSalary = fixedSalary;
        ViewBag.departmentName = departmentName;
        ViewBag.attendenceDays = attendenceDays;
        ViewBag.abscenseDays = abscenseDays;
        ViewBag.BonusHours = BonusHours;
        ViewBag.MinusHours =MinusHours;
        ViewBag.TotalBonus = TotalBonus; 
        ViewBag.TotalMinus = TotalMinus;
        ViewBag.NetSalary = NetSalary; 
        return View();
    }
}
