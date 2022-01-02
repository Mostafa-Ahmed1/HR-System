using System;
using System.Collections.Generic;

namespace HR_System.Models
{
    public partial class Crud
    {
        public int CrudId { get; set; }
        public bool Add { get; set; }
        public bool Update { get; set; }
        public bool Delete { get; set; }
        public bool Read { get; set; }
        public int PageId { get; set; }
        public int GroupId { get; set; }

        public virtual Group Group { get; set; } = null!;
        public virtual Page Page { get; set; } = null!;
    }
}
