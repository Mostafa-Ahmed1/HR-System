using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Migrations
{
    public partial class add1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admin",
                columns: table => new
                {
                    admin_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    admin_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    admin_pass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admin", x => x.admin_id);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    dept_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    dept_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.dept_id);
                });

            migrationBuilder.CreateTable(
                name: "Group",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    group_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Group", x => x.group_id);
                });

            migrationBuilder.CreateTable(
                name: "Page",
                columns: table => new
                {
                    page_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    page_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Page", x => x.page_id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    setting_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    plus_perhour = table.Column<float>(type: "real", nullable: false),
                    minus_perhour = table.Column<float>(type: "real", nullable: false),
                    dayoff_1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    dayoff_2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.setting_id);
                });

            migrationBuilder.CreateTable(
                name: "Vacation",
                columns: table => new
                {
                    vac_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vacation_date = table.Column<DateTime>(type: "date", nullable: false),
                    vacation_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacation", x => x.vac_id);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    emp_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    emp_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    address = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    nationality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    birthdate = table.Column<DateTime>(type: "date", nullable: false),
                    national_id = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    hiredate = table.Column<DateTime>(type: "date", nullable: false),
                    fixed_salary = table.Column<int>(type: "int", nullable: false),
                    att_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    departure_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    dept_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.emp_id);
                    table.ForeignKey(
                        name: "FK_Employee_Department",
                        column: x => x.dept_id,
                        principalTable: "Department",
                        principalColumn: "dept_id");
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    password = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    group_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_User_Group",
                        column: x => x.group_id,
                        principalTable: "Group",
                        principalColumn: "group_id");
                });

            migrationBuilder.CreateTable(
                name: "CRUD",
                columns: table => new
                {
                    crud_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    add = table.Column<bool>(type: "bit", nullable: false),
                    update = table.Column<bool>(type: "bit", nullable: false),
                    delete = table.Column<bool>(type: "bit", nullable: false),
                    read = table.Column<bool>(type: "bit", nullable: false),
                    page_id = table.Column<int>(type: "int", nullable: false),
                    group_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CRUD", x => x.crud_id);
                    table.ForeignKey(
                        name: "FK_CRUD_Group",
                        column: x => x.group_id,
                        principalTable: "Group",
                        principalColumn: "group_id");
                    table.ForeignKey(
                        name: "FK_CRUD_Page",
                        column: x => x.page_id,
                        principalTable: "Page",
                        principalColumn: "page_id");
                });

            migrationBuilder.CreateTable(
                name: "Att_dep",
                columns: table => new
                {
                    att_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    emp_id = table.Column<int>(type: "int", nullable: false),
                    date = table.Column<DateTime>(type: "date", nullable: false),
                    attendance = table.Column<TimeSpan>(type: "time", nullable: false),
                    departure = table.Column<TimeSpan>(type: "time", nullable: false),
                    workedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false, computedColumnSql: "DatePart(HOUR ,[departure] ) + DatePart(MINUTE ,[departure])/60.0 -  DatePart(HOUR ,[attendance] ) + DatePart(MINUTE ,[attendance] )/60.0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Att_dep", x => x.att_id);
                    table.ForeignKey(
                        name: "FK_Att_dep_Employee",
                        column: x => x.emp_id,
                        principalTable: "Employee",
                        principalColumn: "emp_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Att_dep_emp_id",
                table: "Att_dep",
                column: "emp_id");

            migrationBuilder.CreateIndex(
                name: "IX_CRUD_group_id",
                table: "CRUD",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_CRUD_page_id",
                table: "CRUD",
                column: "page_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_dept_id",
                table: "Employee",
                column: "dept_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_group_id",
                table: "User",
                column: "group_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admin");

            migrationBuilder.DropTable(
                name: "Att_dep");

            migrationBuilder.DropTable(
                name: "CRUD");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Vacation");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "Page");

            migrationBuilder.DropTable(
                name: "Group");

            migrationBuilder.DropTable(
                name: "Department");
        }
    }
}
