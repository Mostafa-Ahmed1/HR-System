namespace HR_System.Models
{
    public class GroupRelation
    {
        public Group group { get; set; } = null!;
        public List<PageCrud> pageCruds { get; set; }
    }
}
