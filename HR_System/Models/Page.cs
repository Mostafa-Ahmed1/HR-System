using System;
using System.Collections.Generic;

namespace HR_System.Models
{
    public partial class Page
    {
        public Page()
        {
            Cruds = new HashSet<Crud>();
        }

        public int PageId { get; set; }
        public string? PageName { get; set; }

        public virtual ICollection<Crud> Cruds { get; set; }
    }
}
