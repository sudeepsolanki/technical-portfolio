using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "CustomerName", "OrderDate", "Status", "TotalAmount" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "John Doe", new DateTime(2026, 8, 29, 9, 21, 16, 574, DateTimeKind.Utc).AddTicks(8160), 3, 159.98m },
                    { new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"), "Jane Smith", new DateTime(2026, 9, 1, 9, 21, 16, 574, DateTimeKind.Utc).AddTicks(8470), 1, 249.99m },
                    { new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"), "Bob Johnson", new DateTime(2026, 9, 3, 9, 21, 16, 574, DateTimeKind.Utc).AddTicks(8470), 0, 89.50m }
                });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "Id", "OrderId", "ProductName", "Quantity", "UnitPrice" },
                values: new object[,]
                {
                    { new Guid("a7b8c9d0-e1f2-3456-abcd-567890123456"), new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"), "Mouse Pad", 2, 50.00m },
                    { new Guid("b8c9d0e1-f2a3-4567-bcde-678901234567"), new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"), "Monitor Stand", 1, 89.50m },
                    { new Guid("d4e5f6a7-b8c9-0123-defa-234567890123"), new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "Wireless Mouse", 2, 29.99m },
                    { new Guid("e5f6a7b8-c9d0-1234-efab-345678901234"), new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "USB Cable", 1, 99.99m },
                    { new Guid("f6a7b8c9-d0e1-2345-fabc-456789012345"), new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"), "Mechanical Keyboard", 1, 149.99m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
