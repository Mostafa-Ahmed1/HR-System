
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HR_System.Models
{
    public partial class Group
    {
        public Group()
        {
            Cruds = new HashSet<Crud>();
            Users = new HashSet<User>();
        }

        public int GroupId { get; set; }

        [Unique]
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "*Group Name Is Required")]
        public string GroupName { get; set; }

        public virtual ICollection<Crud> Cruds { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}
