using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace School_Library_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipTypeToMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MembershipType",
                table: "Members",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Student");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MembershipType",
                table: "Members");
        }
    }
}
