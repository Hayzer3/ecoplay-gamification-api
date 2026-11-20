using EcoPlay.Gamification.Api.Domain;
using EcoPlay.Gamification.Api.Persistence;
using EcoPlay.Gamification.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EcoPlay.Gamification.Tests;

public sealed class MissaoProgressoTests
{
    [Fact]
    public async Task Credito_de_entrega_avanca_missao_e_replay_nao_duplica_recompensa()
    {
        await using var store = await TestStore.CreateAsync(
            new Missao { Id = 1, Titulo = "Primeira entrega", Descricao = "Conclua uma entrega.", Tipo = "ENTREGA", Meta = 1, Xp = 30 });
        var request = new CreditoXpRequest("cidadao-1", "ENTREGA", "entrega-1", 1m);

        var first = await store.Service.CreditarAsync("entrega-1", request, CancellationToken.None);
        var replay = await store.Service.CreditarAsync("entrega-1", request, CancellationToken.None);

        Assert.Equal(first, replay);
        Assert.Equal(50, first.XpTotal);
        var assignment = await store.Db.MissoesUsuario.SingleAsync();
        Assert.Equal(1, assignment.Progresso);
        Assert.NotNull(assignment.ConcluidaEm);
        Assert.Equal(1, await store.Db.LancamentosXp.CountAsync(x => x.Origem == "MISSAO"));
    }

    [Fact]
    public async Task Creditos_de_identificacao_concluem_missao_e_concedem_xp_uma_vez()
    {
        await using var store = await TestStore.CreateAsync(
            new Missao { Id = 5, Titulo = "Olhar atento", Descricao = "Confirme identificações.", Tipo = "IDENTIFICACAO", Meta = 3, Xp = 10 });

        await store.Service.CreditarAsync("identificacao-1", new("cidadao-1", "IDENTIFICACAO", "identificacao-1"), CancellationToken.None);
        await store.Service.CreditarAsync("identificacao-2", new("cidadao-1", "IDENTIFICACAO", "identificacao-2"), CancellationToken.None);
        var third = await store.Service.CreditarAsync("identificacao-3", new("cidadao-1", "IDENTIFICACAO", "identificacao-3"), CancellationToken.None);

        Assert.Equal(16, third.XpTotal);
        var assignment = await store.Db.MissoesUsuario.SingleAsync();
        Assert.Equal(3, assignment.Progresso);
        Assert.NotNull(assignment.ConcluidaEm);
        Assert.Equal(1, await store.Db.LancamentosXp.CountAsync(x => x.Origem == "MISSAO"));
    }

    [Fact]
    public async Task Quiz_avanca_missao_somente_com_resposta_correta_e_premia_conclusao()
    {
        await using var store = await TestStore.CreateAsync(
            new Missao { Id = 4, Titulo = "Quiz consciente", Descricao = "Acerte cinco respostas.", Tipo = "QUIZ", Meta = 5, Xp = 25 },
            new PerguntaQuiz { Id = 10, Enunciado = "Pergunta de teste?", Alternativas = ["Correta", "Incorreta"], IndiceCorreto = 0, Categoria = "GERAL" });

        var incorreta = await store.Service.ResponderQuizAsync("cidadao-1", new(10, 1), CancellationToken.None);
        Assert.False(incorreta.Correta);
        Assert.Empty(await store.Db.MissoesUsuario.ToListAsync());

        RespostaQuizResult? quintaResposta = null;
        for (var i = 0; i < 5; i++)
            quintaResposta = await store.Service.ResponderQuizAsync("cidadao-1", new(10, 0), CancellationToken.None);

        Assert.NotNull(quintaResposta);
        Assert.Equal(30, quintaResposta.XpConcedido);
        Assert.Equal(25, quintaResposta.XpQuizHoje);
        Assert.Contains("Missão concluída", quintaResposta.Mensagem);
        var assignment = await store.Db.MissoesUsuario.SingleAsync();
        Assert.Equal(5, assignment.Progresso);
        Assert.NotNull(assignment.ConcluidaEm);
        Assert.Equal(1, await store.Db.LancamentosXp.CountAsync(x => x.Origem == "MISSAO"));
    }

    private sealed class TestStore : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public GamificationDbContext Db { get; }
        public GamificationService Service { get; }

        private TestStore(SqliteConnection connection, GamificationDbContext db)
        {
            this.connection = connection;
            Db = db;
            Service = new GamificationService(db, new IdempotencyService(db), NullLogger<GamificationService>.Instance);
        }

        public static async Task<TestStore> CreateAsync(params object[] seed)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<GamificationDbContext>().UseSqlite(connection).Options;
            var db = new GamificationDbContext(options);
            await db.Database.EnsureCreatedAsync();
            foreach (var item in seed)
            {
                if (item is Missao mission) db.Missoes.Add(mission);
                else if (item is PerguntaQuiz question) db.PerguntasQuiz.Add(question);
            }
            await db.SaveChangesAsync();
            return new TestStore(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
