using HR_System.Models;
namespace HR_System.ViewModels;
public class PagesVM
{
        public Page page { get; set; }
        public bool ADD { get; set; }
        public bool Update { get; set; }
       public bool Delete { get; set; }
       public bool Read { get; set; }

}
