using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfCoreLab.Migrations.BonusDb
{
    /// <inheritdoc />
    public partial class BonusChallange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonusCustomers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusCustomers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonusInvoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusInvoices", x => x.Id);
                    table.CheckConstraint("CK_BonusInvoice_Amount", "Amount >= 0");
                    table.ForeignKey(
                        name: "FK_BonusInvoices_BonusCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "BonusCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BonusTelephoneNumbers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusTelephoneNumbers", x => x.Id);
                    table.CheckConstraint("CK_BonusTelephoneNumber_Type", "Type IN ('Mobile', 'Work', 'DirectDial')");
                    table.ForeignKey(
                        name: "FK_BonusTelephoneNumbers_BonusCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "BonusCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonusCustomers_CreatedDate",
                table: "BonusCustomers",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_BonusCustomers_Email",
                table: "BonusCustomers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonusCustomers_IsDeleted",
                table: "BonusCustomers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BonusInvoices_CustomerId",
                table: "BonusInvoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusInvoices_InvoiceDate",
                table: "BonusInvoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_BonusInvoices_InvoiceNumber",
                table: "BonusInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonusInvoices_IsDeleted",
                table: "BonusInvoices",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BonusTelephoneNumbers_CustomerId",
                table: "BonusTelephoneNumbers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusTelephoneNumbers_IsDeleted",
                table: "BonusTelephoneNumbers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BonusTelephoneNumbers_Type",
                table: "BonusTelephoneNumbers",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonusInvoices");

            migrationBuilder.DropTable(
                name: "BonusTelephoneNumbers");

            migrationBuilder.DropTable(
                name: "BonusCustomers");
        }
    }
}
