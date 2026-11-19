using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcoPlay.Gamification.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOracle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lancamento_xp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UsuarioId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Origem = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    Referencia = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: true),
                    Quantidade = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    PesoKg = table.Column<decimal>(type: "DECIMAL(8,2)", precision: 8, scale: 2, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamento_xp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "medalha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Nome = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Regra = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medalha", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "missao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Titulo = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Tipo = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    Meta = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Xp = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_missao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "perfil",
                columns: table => new
                {
                    UsuarioId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Bairro = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfil", x => x.UsuarioId);
                });

            migrationBuilder.CreateTable(
                name: "pergunta_quiz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Enunciado = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Alternativas = table.Column<string>(type: "CLOB", nullable: false),
                    IndiceCorreto = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Categoria = table.Column<string>(type: "NVARCHAR2(40)", maxLength: 40, nullable: false),
                    Ativa = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pergunta_quiz", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "requisicao_idempotente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Chave = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    Operacao = table.Column<string>(type: "NVARCHAR2(60)", maxLength: 60, nullable: false),
                    HashRequisicao = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    RespostaJson = table.Column<string>(type: "CLOB", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requisicao_idempotente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "medalha_usuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UsuarioId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    MedalhaId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ConquistadaEm = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medalha_usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_medalha_usuario_medalha_MedalhaId",
                        column: x => x.MedalhaId,
                        principalTable: "medalha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "missao_usuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UsuarioId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    MissaoId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Progresso = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ConcluidaEm = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_missao_usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_missao_usuario_missao_MissaoId",
                        column: x => x.MissaoId,
                        principalTable: "missao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "medalha",
                columns: new[] { "Id", "Nome", "Regra" },
                values: new object[,]
                {
                    { 1, "Primeira Entrega", "Concluir a primeira entrega." },
                    { 2, "10 kg Entregues", "Acumular 10 kg em entregas confirmadas." },
                    { 3, "Separação Perfeita", "Concluir cinco entregas seguidas sem recusa." },
                    { 4, "Caçador de Pilhas", "Entregar o primeiro resíduo especial." },
                    { 5, "Mestre do Quiz", "Acertar dez perguntas seguidas." }
                });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_xp_UsuarioId_CriadoEm",
                table: "lancamento_xp",
                columns: new[] { "UsuarioId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_medalha_usuario_MedalhaId",
                table: "medalha_usuario",
                column: "MedalhaId");

            migrationBuilder.CreateIndex(
                name: "IX_medalha_usuario_UsuarioId_MedalhaId",
                table: "medalha_usuario",
                columns: new[] { "UsuarioId", "MedalhaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_missao_usuario_MissaoId",
                table: "missao_usuario",
                column: "MissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_missao_usuario_UsuarioId_MissaoId",
                table: "missao_usuario",
                columns: new[] { "UsuarioId", "MissaoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_requisicao_idempotente_Chave",
                table: "requisicao_idempotente",
                column: "Chave",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lancamento_xp");

            migrationBuilder.DropTable(
                name: "medalha_usuario");

            migrationBuilder.DropTable(
                name: "missao_usuario");

            migrationBuilder.DropTable(
                name: "perfil");

            migrationBuilder.DropTable(
                name: "pergunta_quiz");

            migrationBuilder.DropTable(
                name: "requisicao_idempotente");

            migrationBuilder.DropTable(
                name: "medalha");

            migrationBuilder.DropTable(
                name: "missao");
        }
    }
}
