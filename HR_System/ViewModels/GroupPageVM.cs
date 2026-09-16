using HR_System.Models;
namespace HR_System.ViewModels;
public class GroupPageVM
{
    public List<PagesVM> PagesVM { get; set; } = null!;
    public Group group { get; set; } = null!;
}
