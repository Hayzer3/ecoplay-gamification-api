using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcoPlay.Gamification.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gamificacao");

            migrationBuilder.CreateTable(
                name: "lancamento_xp",
                schema: "gamificacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    PesoKg = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamento_xp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "medalha",
                schema: "gamificacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Regra = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medalha", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "missao",
                schema: "gamificacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Meta = table.Column<int>(type: "int", nullable: false),
                    Xp = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_missao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "perfil",
                schema: "gamificacao",
                columns: table => new
                {
                    UsuarioId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfil", x => x.UsuarioId);
                });

            migrationBuilder.CreateTable(
                name: "pergunta_quiz",
                schema: "gamificacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enunciado = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Alternativas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndiceCorreto = table.Column<int>(type: "int", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pergunta_quiz", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "requisicao_idempotente",
                schema: "gamificacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Operacao = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    HashRequisicao = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RespostaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requisicao_idempotente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "medalha_usuario",
                schema: "gamificacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MedalhaId = table.Column<int>(type: "int", nullable: false),
                    ConquistadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medalha_usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_medalha_usuario_medalha_MedalhaId",
                        column: x => x.MedalhaId,
                        principalSchema: "gamificacao",
                        principalTable: "medalha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "missao_usuario",
                schema: "gamificacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MissaoId = table.Column<int>(type: "int", nullable: false),
                    Progresso = table.Column<int>(type: "int", nullable: false),
                    ConcluidaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_missao_usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_missao_usuario_missao_MissaoId",
                        column: x => x.MissaoId,
                        principalSchema: "gamificacao",
                        principalTable: "missao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "gamificacao",
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

            migrationBuilder.InsertData(
                schema: "gamificacao",
                table: "missao",
                columns: new[] { "Id", "Descricao", "Meta", "Tipo", "Titulo", "Xp" },
                values: new object[,]
                {
                    { 1, "Conclua sua primeira entrega em uma cooperativa.", 1, "ENTREGA", "Primeira entrega", 30 },
                    { 2, "Entregue 2 kg de vidro corretamente.", 2, "ENTREGA", "Separação de vidro", 40 },
                    { 3, "Revise como preparar materiais recusados por contaminação.", 1, "REFORCO", "Lavou, separou", 20 }
                });

            migrationBuilder.InsertData(
                schema: "gamificacao",
                table: "pergunta_quiz",
                columns: new[] { "Id", "Alternativas", "Ativa", "Categoria", "Enunciado", "IndiceCorreto" },
                values: new object[,]
                {
                    { 1, "[\"Papel e papel\\u00E3o\",\"Restos org\\u00E2nicos\",\"Rejeitos sanit\\u00E1rios\"]", true, "PAPEL", "Qual material deve estar limpo e seco antes da reciclagem?", 0 },
                    { 2, "[\"No lixo comum\",\"Em ponto de coleta espec\\u00EDfico\",\"Na pia\"]", true, "ESPECIAL", "Onde pilhas usadas devem ser descartadas?", 1 },
                    { 3, "[\"Inteira e sem restos\",\"Quebrada em casa\",\"Com alimento dentro\"]", true, "VIDRO", "Uma embalagem de vidro deve ser descartada como?", 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_xp_UsuarioId_CriadoEm",
                schema: "gamificacao",
                table: "lancamento_xp",
                columns: new[] { "UsuarioId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_medalha_usuario_MedalhaId",
                schema: "gamificacao",
                table: "medalha_usuario",
                column: "MedalhaId");

            migrationBuilder.CreateIndex(
                name: "IX_medalha_usuario_UsuarioId_MedalhaId",
                schema: "gamificacao",
                table: "medalha_usuario",
                columns: new[] { "UsuarioId", "MedalhaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_missao_usuario_MissaoId",
                schema: "gamificacao",
                table: "missao_usuario",
                column: "MissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_missao_usuario_UsuarioId_MissaoId",
                schema: "gamificacao",
                table: "missao_usuario",
                columns: new[] { "UsuarioId", "MissaoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_requisicao_idempotente_Chave",
                schema: "gamificacao",
                table: "requisicao_idempotente",
                column: "Chave",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lancamento_xp",
                schema: "gamificacao");

            migrationBuilder.DropTable(
                name: "medalha_usuario",
                schema: "gamificacao");

            migrationBuilder.DropTable(
                name: "missao_usuario",
                schema: "gamificacao");

            migrationBuilder.DropTable(
                name: "perfil",
                schema: "gamificacao");

            migrationBuilder.DropTable(
                name: "pergunta_quiz",
                schema: "gamificacao");

            migrationBuilder.DropTable(
                name: "requisicao_idempotente",
                schema: "gamificacao");

            migrationBuilder.DropTable(
                name: "medalha",
                schema: "gamificacao");

            migrationBuilder.DropTable(
                name: "missao",
                schema: "gamificacao");
        }
    }
}
