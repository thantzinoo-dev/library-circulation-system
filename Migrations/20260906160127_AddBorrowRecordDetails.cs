using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace School_Library_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowRecordDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "BorrowRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "BorrowRecords",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ReturnCondition",
                table: "BorrowRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_BorrowRecords_Quantity",
                table: "BorrowRecords",
                sql: "[Quantity] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BorrowRecords_Quantity",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "ReturnCondition",
                table: "BorrowRecords");
        }
    }
}
