using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalPlatform.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentId1",
                table: "TrainingRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentId1",
                table: "TrainingAttempts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId1",
                table: "Students",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentId1",
                table: "QuizAttempts",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "PostAttachments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "StudentId1",
                table: "ExamResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentId1",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId1",
                table: "Assistants",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_StudentId1",
                table: "TrainingRecords",
                column: "StudentId1");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAttempts_StudentId1",
                table: "TrainingAttempts",
                column: "StudentId1");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ApplicationUserId1",
                table: "Students",
                column: "ApplicationUserId1",
                unique: true,
                filter: "[ApplicationUserId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_StudentId1",
                table: "QuizAttempts",
                column: "StudentId1");

            migrationBuilder.CreateIndex(
                name: "IX_ExamResults_StudentId1",
                table: "ExamResults",
                column: "StudentId1");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId1",
                table: "AttendanceRecords",
                column: "StudentId1");

            migrationBuilder.CreateIndex(
                name: "IX_Assistants_ApplicationUserId1",
                table: "Assistants",
                column: "ApplicationUserId1",
                unique: true,
                filter: "[ApplicationUserId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Assistants_AspNetUsers_ApplicationUserId1",
                table: "Assistants",
                column: "ApplicationUserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Students_StudentId1",
                table: "AttendanceRecords",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamResults_Students_StudentId1",
                table: "ExamResults",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempts_Students_StudentId1",
                table: "QuizAttempts",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_AspNetUsers_ApplicationUserId1",
                table: "Students",
                column: "ApplicationUserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAttempts_Students_StudentId1",
                table: "TrainingAttempts",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRecords_Students_StudentId1",
                table: "TrainingRecords",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assistants_AspNetUsers_ApplicationUserId1",
                table: "Assistants");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Students_StudentId1",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamResults_Students_StudentId1",
                table: "ExamResults");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempts_Students_StudentId1",
                table: "QuizAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_AspNetUsers_ApplicationUserId1",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingAttempts_Students_StudentId1",
                table: "TrainingAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRecords_Students_StudentId1",
                table: "TrainingRecords");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRecords_StudentId1",
                table: "TrainingRecords");

            migrationBuilder.DropIndex(
                name: "IX_TrainingAttempts_StudentId1",
                table: "TrainingAttempts");

            migrationBuilder.DropIndex(
                name: "IX_Students_ApplicationUserId1",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_StudentId1",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ExamResults_StudentId1",
                table: "ExamResults");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentId1",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_Assistants_ApplicationUserId1",
                table: "Assistants");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "TrainingRecords");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "TrainingAttempts");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId1",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "ExamResults");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId1",
                table: "Assistants");

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "PostAttachments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
