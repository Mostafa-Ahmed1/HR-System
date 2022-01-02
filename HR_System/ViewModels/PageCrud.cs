namespace HR_System.Models
{
    public class PageCrud
    {
        public Page page { get; set; }
        public bool ADD { get; set; }
        public bool Update { get; set; }
        public bool Delete { get; set; }
        public bool Read { get; set; }

        public int Count=0;
    }
}
