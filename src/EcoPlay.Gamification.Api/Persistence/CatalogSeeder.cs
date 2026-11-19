using EcoPlay.Gamification.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoPlay.Gamification.Api.Persistence;

public static class CatalogSeeder
{
    private static readonly PerguntaQuiz[] Questions =
    [
        new() { Id = 1, Enunciado = "Qual material deve estar limpo e seco antes da reciclagem?", Alternativas = ["Papel e papelão", "Restos orgânicos", "Rejeitos sanitários"], IndiceCorreto = 0, Categoria = "PAPEL" },
        new() { Id = 2, Enunciado = "Onde pilhas usadas devem ser descartadas?", Alternativas = ["No lixo comum", "Em ponto de coleta específico", "Na pia"], IndiceCorreto = 1, Categoria = "ESPECIAL" },
        new() { Id = 3, Enunciado = "Uma embalagem de vidro deve ser descartada como?", Alternativas = ["Inteira e sem restos", "Quebrada em casa", "Com alimento dentro"], IndiceCorreto = 0, Categoria = "VIDRO" },
        new() { Id = 4, Enunciado = "Uma lata de bebida vazia pertence a qual categoria?", Alternativas = ["Metal", "Orgânico", "Rejeito"], IndiceCorreto = 0, Categoria = "METAL" },
        new() { Id = 5, Enunciado = "O que fazer com uma embalagem plástica reciclável antes da entrega?", Alternativas = ["Retirar o excesso de resíduos", "Misturar com óleo", "Queimar"], IndiceCorreto = 0, Categoria = "PLASTICO" },
        new() { Id = 6, Enunciado = "Cascas de frutas podem ser destinadas a qual fluxo quando há coleta adequada?", Alternativas = ["Compostagem de orgânicos", "Papel", "Vidro"], IndiceCorreto = 0, Categoria = "ORGANICO" },
        new() { Id = 7, Enunciado = "Qual é a forma adequada de descartar óleo de cozinha usado?", Alternativas = ["Guardar em recipiente e levar a ponto de coleta", "Jogar na pia", "Despejar no solo"], IndiceCorreto = 0, Categoria = "ESPECIAL" },
        new() { Id = 8, Enunciado = "Onde devem ser entregues medicamentos vencidos?", Alternativas = ["Em ponto de coleta que os aceite", "No reciclável seco", "No ralo"], IndiceCorreto = 0, Categoria = "ESPECIAL" },
        new() { Id = 9, Enunciado = "O que fazer com um aparelho eletrônico sem uso?", Alternativas = ["Levar a um ponto de coleta de eletrônicos", "Colocar no lixo orgânico", "Desmontar e descartar na rua"], IndiceCorreto = 0, Categoria = "ESPECIAL" },
        new() { Id = 10, Enunciado = "Uma caixa de pizza com gordura deve ser separada como?", Alternativas = ["A parte limpa pode ir ao papel; a parte engordurada segue a orientação local", "Toda a caixa vai no vidro", "Toda a caixa vai no metal"], IndiceCorreto = 0, Categoria = "PAPEL" },
        new() { Id = 11, Enunciado = "Por que separar recicláveis secos de restos de comida?", Alternativas = ["Para reduzir contaminação e facilitar a triagem", "Para aumentar o peso do saco", "Para misturar categorias"], IndiceCorreto = 0, Categoria = "GERAL" },
        new() { Id = 12, Enunciado = "Uma garrafa de vidro que ainda contém bebida deve ser entregue como?", Alternativas = ["Esvaziada, sem quebrar, conforme a orientação do ponto", "Com líquido dentro", "Triturada em casa"], IndiceCorreto = 0, Categoria = "VIDRO" },
        new() { Id = 13, Enunciado = "Lâmpadas devem ser descartadas em qual local?", Alternativas = ["Ponto específico que aceite lâmpadas", "Coleta de papel", "Pia da cozinha"], IndiceCorreto = 0, Categoria = "ESPECIAL" },
        new() { Id = 14, Enunciado = "Uma folha de papel limpa e seca deve ir para qual categoria?", Alternativas = ["Papel", "Orgânico", "Óleo"], IndiceCorreto = 0, Categoria = "PAPEL" },
        new() { Id = 15, Enunciado = "Qual atitude ajuda a cooperativa durante a triagem?", Alternativas = ["Manter materiais separados e sem restos de comida", "Misturar vidro quebrado com papel", "Encharcar o papel"], IndiceCorreto = 0, Categoria = "GERAL" },
        new() { Id = 16, Enunciado = "Uma embalagem metálica vazia deve ser encaminhada como?", Alternativas = ["Metal, se o ponto aceitar", "Orgânico", "Resíduo hospitalar"], IndiceCorreto = 0, Categoria = "METAL" },
        new() { Id = 17, Enunciado = "O que fazer quando não sabe se o ponto aceita um material?", Alternativas = ["Consultar a lista de materiais aceitos", "Deixar no local sem perguntar", "Misturar com qualquer saco"], IndiceCorreto = 0, Categoria = "GERAL" },
        new() { Id = 18, Enunciado = "Papelão molhado ou com alimento pode prejudicar a reciclagem?", Alternativas = ["Sim, pode contaminar o material", "Não, sempre melhora a qualidade", "Só se for metal"], IndiceCorreto = 0, Categoria = "PAPEL" },
        new() { Id = 19, Enunciado = "Qual é o destino mais seguro para uma bateria usada?", Alternativas = ["Ponto de coleta de baterias", "Lixo comum", "Compostagem"], IndiceCorreto = 0, Categoria = "ESPECIAL" },
        new() { Id = 20, Enunciado = "Se houver dúvida na identificação de um resíduo, qual deve ser o próximo passo?", Alternativas = ["Confirmar a categoria ou buscar pelo nome antes de descartar", "Chutar e enviar ao reciclável", "Descartar em qualquer ponto"], IndiceCorreto = 0, Categoria = "GERAL" }
    ];

    private static readonly Missao[] Missions =
    [
        new() { Id = 1, Titulo = "Primeira entrega", Descricao = "Conclua sua primeira entrega em uma cooperativa.", Tipo = "ENTREGA", Meta = 1, Xp = 30 },
        new() { Id = 2, Titulo = "Separação de vidro", Descricao = "Entregue 2 kg de vidro corretamente.", Tipo = "ENTREGA", Meta = 2, Xp = 40 },
        new() { Id = 3, Titulo = "Lavou, separou", Descricao = "Revise como preparar materiais recusados por contaminação.", Tipo = "REFORCO", Meta = 1, Xp = 20 },
        new() { Id = 4, Titulo = "Quiz consciente", Descricao = "Responda corretamente a cinco perguntas do quiz.", Tipo = "QUIZ", Meta = 5, Xp = 25 },
        new() { Id = 5, Titulo = "Olhar atento", Descricao = "Confirme três identificações de resíduos.", Tipo = "IDENTIFICACAO", Meta = 3, Xp = 10 },
        new() { Id = 6, Titulo = "Entrega limpa", Descricao = "Conclua uma entrega com os materiais separados e aceitos.", Tipo = "ENTREGA", Meta = 1, Xp = 35 }
    ];

    public static async Task SeedAsync(GamificationDbContext db, CancellationToken ct = default)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            var questionIds = await db.PerguntasQuiz.Select(x => x.Id).ToListAsync(ct);
            var questionsToAdd = Questions.Where(x => !questionIds.Contains(x.Id)).ToArray();
            if (questionsToAdd.Length > 0)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT [gamificacao].[pergunta_quiz] ON", ct);
                try
                {
                    db.PerguntasQuiz.AddRange(questionsToAdd);
                    await db.SaveChangesAsync(ct);
                }
                finally
                {
                    await db.Database.ExecuteSqlRawAsync(
                        "SET IDENTITY_INSERT [gamificacao].[pergunta_quiz] OFF", ct);
                }
            }

            var missionIds = await db.Missoes.Select(x => x.Id).ToListAsync(ct);
            var missionsToAdd = Missions.Where(x => !missionIds.Contains(x.Id)).ToArray();
            if (missionsToAdd.Length > 0)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SET IDENTITY_INSERT [gamificacao].[missao] ON", ct);
                try
                {
                    db.Missoes.AddRange(missionsToAdd);
                    await db.SaveChangesAsync(ct);
                }
                finally
                {
                    await db.Database.ExecuteSqlRawAsync(
                        "SET IDENTITY_INSERT [gamificacao].[missao] OFF", ct);
                }
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
