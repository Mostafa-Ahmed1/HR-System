using Microsoft.EntityFrameworkCore;

namespace HR_System.Models
{
    public class HrSysContext : DbContext
    {
        public HrSysContext()
        {
        }

        public HrSysContext(DbContextOptions<HrSysContext> option) : base(option)
        {
        }
        public virtual DbSet<Admin> Admins { get; set; } = null!;
        public virtual DbSet<AttDep> Att_dep { get; set; } = null!;
        public virtual DbSet<Crud> CRUDs { get; set; } = null!;
        public virtual DbSet<Department> Departments { get; set; } = null!;
        public virtual DbSet<Employee> Employees { get; set; } = null!;
        public virtual DbSet<Group> Groups { get; set; } = null!;
        public virtual DbSet<Page> Pages { get; set; } = null!;
        public virtual DbSet<Setting> Settings { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<Vacation> Vacations { get; set; } = null!;
        public object Employee { get; internal set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("Admin");

                entity.Property(e => e.AdminId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("admin_id");

                entity.Property(e => e.AdminName)
                    .HasMaxLength(50)
                    .HasColumnName("admin_name");

                entity.Property(e => e.AdminPass)
                    .HasMaxLength(50)
                    .HasColumnName("admin_pass");
            });

            modelBuilder.Entity<AttDep>(entity =>
            {
                entity.HasKey(e => e.AttId);

                entity.ToTable("Att_dep");

                entity.Property(e => e.AttId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("att_id");

                entity.Property(e => e.Attendance).HasColumnName("attendance");

                entity.Property(e => e.Date)
                    .HasColumnType("date")
                    .HasColumnName("date");

                entity.Property(e => e.Departure).HasColumnName("departure");

                entity.Property(e => e.EmpId).HasColumnName("emp_id");

                entity.HasOne(d => d.Emp)
                    .WithMany(p => p.AttDeps)
                    .HasForeignKey(d => d.EmpId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Att_dep_Employee");


                entity.Property(m => m.workedHours)
                .HasComputedColumnSql("DatePart(HOUR ,[departure] ) + DatePart(MINUTE ,[departure])/60.0 -  DatePart(HOUR ,[attendance] ) + DatePart(MINUTE ,[attendance] )/60.0");
            });

            modelBuilder.Entity<Crud>(entity =>
            {
                entity.ToTable("CRUD");

                entity.Property(e => e.CrudId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("crud_id");

                entity.Property(e => e.Add).HasColumnName("add");

                entity.Property(e => e.Delete).HasColumnName("delete");

                entity.Property(e => e.GroupId).HasColumnName("group_id");

                entity.Property(e => e.PageId).HasColumnName("page_id");

                entity.Property(e => e.Read).HasColumnName("read");

                entity.Property(e => e.Update).HasColumnName("update");

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.Cruds)
                    .HasForeignKey(d => d.GroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CRUD_Group");

                entity.HasOne(d => d.Page)
                    .WithMany(p => p.Cruds)
                    .HasForeignKey(d => d.PageId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CRUD_Page");
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.DeptId);

                entity.ToTable("Department");

                entity.Property(e => e.DeptId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("dept_id");

                entity.Property(e => e.DeptName)
                    .HasMaxLength(50)
                    .HasColumnName("dept_name");
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmpId);

                entity.ToTable("Employee");

                entity.Property(e => e.EmpId).ValueGeneratedOnAdd().HasColumnName("emp_id");

                entity.Property(e => e.Address)
                    .HasMaxLength(150)
                    .HasColumnName("address");

                entity.Property(e => e.AttTime).HasColumnName("att_time");

                entity.Property(e => e.Birthdate)
                    .HasColumnType("date")
                    .HasColumnName("birthdate");

                entity.Property(e => e.DepartureTime).HasColumnName("departure_time");

                entity.Property(e => e.DeptId).HasColumnName("dept_id");

                entity.Property(e => e.EmpName)
                    .HasMaxLength(150)
                    .HasColumnName("emp_name");

                entity.Property(e => e.FixedSalary).HasColumnName("fixed_salary");

                entity.Property(e => e.Gender)
                    .HasMaxLength(10)
                    .HasColumnName("gender");

                entity.Property(e => e.Hiredate)
                    .HasColumnType("date")
                    .HasColumnName("hiredate");

                entity.Property(e => e.NationalId)
                    .HasMaxLength(14)
                    .HasColumnName("national_id");

                entity.Property(e => e.Nationality)
                    .HasMaxLength(50)
                    .HasColumnName("nationality");

                entity.Property(e => e.Phone).HasColumnName("phone");

                entity.HasOne(d => d.Dept)
                    .WithMany(p => p.Employees)
                    .HasForeignKey(d => d.DeptId)
                    .HasConstraintName("FK_Employee_Department");
            });

            modelBuilder.Entity<Group>(entity =>
            {
                entity.ToTable("Group");

                entity.Property(e => e.GroupId).ValueGeneratedOnAdd().HasColumnName("group_id");

                entity.Property(e => e.GroupName)
                    .HasMaxLength(50)
                    .HasColumnName("group_name");
            });

            modelBuilder.Entity<Page>(entity =>
            {
                entity.ToTable("Page");

                entity.Property(e => e.PageId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("page_id");

                entity.Property(e => e.PageName)
                    .HasMaxLength(50)
                    .HasColumnName("page_name");
            });

            modelBuilder.Entity<Setting>(entity =>
            {
                entity.Property(e => e.SettingId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("setting_id");

                entity.Property(e => e.Dayoff1)
                    .HasMaxLength(50)
                    .HasColumnName("dayoff_1");

                entity.Property(e => e.Dayoff2)
                    .HasMaxLength(50)
                    .HasColumnName("dayoff_2");

                entity.Property(e => e.MinusPerhour).HasColumnName("minus_perhour");

                entity.Property(e => e.PlusPerhour).HasColumnName("plus_perhour");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.Property(e => e.UserId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("user_id");

                entity.Property(e => e.Email)
                    .HasMaxLength(150)
                    .HasColumnName("email");

               

                entity.Property(e => e.GroupId).HasColumnName("group_id");

                entity.Property(e => e.Password)
                    .HasMaxLength(50)
                    .HasColumnName("password");

                entity.Property(e => e.Username)
                    .HasMaxLength(50)
                    .HasColumnName("username");

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.GroupId)
                    .HasConstraintName("FK_User_Group");
            });

            modelBuilder.Entity<Vacation>(entity =>
            {
                entity.HasKey(e => e.VacId);

                entity.ToTable("Vacation");

                entity.Property(e => e.VacId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("vac_id");

                entity.Property(e => e.VacationDate)
                    .HasColumnType("date")
                    .HasColumnName("vacation_date");

                entity.Property(e => e.VacationName)
                    .HasMaxLength(150)
                    .HasColumnName("vacation_name");
            });
        }
    }
}
