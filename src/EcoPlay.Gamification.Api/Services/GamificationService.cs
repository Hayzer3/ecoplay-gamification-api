using EcoPlay.Gamification.Api.Domain;
using EcoPlay.Gamification.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcoPlay.Gamification.Api.Services;

public sealed class GamificationService(GamificationDbContext db, IdempotencyService idempotency, ILogger<GamificationService> logger)
{
    private static readonly TimeZoneInfo SaoPaulo = FindSaoPaulo();

    public async Task<CreditoXpResult> CreditarAsync(string key, CreditoXpRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.UsuarioId) || request.UsuarioId.Length > 100)
            throw new ArgumentException("UsuarioId inválido.");
        if (request.Origem is not ("ENTREGA" or "IDENTIFICACAO"))
            throw new ArgumentException("Origem deve ser ENTREGA ou IDENTIFICACAO.");
        if (string.IsNullOrWhiteSpace(request.Referencia)) throw new ArgumentException("Referencia é obrigatória.");

        var result = await idempotency.ExecuteAsync(key, "CREDITO_XP", request, async () =>
        {
            var agora = DateTimeOffset.UtcNow;
            var creditado = request.Origem switch
            {
                "ENTREGA" => RegrasGamificacao.XpEntrega(request.PesoKg ?? 0),
                "IDENTIFICACAO" => await CreditoIdentificacaoDoDia(request.UsuarioId, agora, ct),
                _ => 0
            };
            if (request.Origem == "ENTREGA" && (request.PesoKg is null or <= 0 or > 200))
                throw new ArgumentException("A entrega deve ter peso maior que zero e até 200 kg.");

            db.LancamentosXp.Add(new LancamentoXp
            {
                UsuarioId = request.UsuarioId, Origem = request.Origem,
                Referencia = request.Referencia, Quantidade = creditado, PesoKg = request.PesoKg, CriadoEm = agora
            });
            var xpMissoes = await AvancarMissoesAsync(request.UsuarioId, request.Origem, 1, agora, ct);
            var total = await db.LancamentosXp.Where(x => x.UsuarioId == request.UsuarioId).SumAsync(x => (int?)x.Quantidade, ct) ?? 0;
            total += creditado + xpMissoes;
            return new CreditoXpResult(request.UsuarioId, creditado, total, RegrasGamificacao.Nivel(total));
        }, ct);
        if (request.Origem == "ENTREGA")
        {
            try
            {
                await ConcederMedalhasEntrega(request, DateTimeOffset.UtcNow, ct);
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                db.ChangeTracker.Clear();
                logger.LogWarning(ex, "Medalha concorrente não foi gravada; o crédito idempotente de XP continua válido para {UsuarioId}.", request.UsuarioId);
            }
        }
        return result;
    }

    public async Task<RespostaQuizResult> ResponderQuizAsync(string userId, RespostaQuizRequest request, CancellationToken ct)
    {
        var question = await db.PerguntasQuiz.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.PerguntaId && x.Ativa, ct)
            ?? throw new KeyNotFoundException("Pergunta não encontrada.");
        if (request.IndiceAlternativa < 0 || request.IndiceAlternativa >= question.Alternativas.Length)
            throw new ArgumentException("Alternativa inválida.");

        var agora = DateTimeOffset.UtcNow;
        var (inicio, fim) = HojeEmSaoPaulo(agora);
        var xpHoje = await db.LancamentosXp.Where(x => x.UsuarioId == userId && x.Origem == "QUIZ" && x.CriadoEm >= inicio && x.CriadoEm < fim).SumAsync(x => (int?)x.Quantidade, ct) ?? 0;
        var correta = question.IndiceCorreto == request.IndiceAlternativa;
        var xp = correta ? Math.Min(5, Math.Max(0, 50 - xpHoje)) : 0;
        var xpMissao = correta
            ? await AvancarMissoesAsync(userId, "QUIZ", 1, agora, ct)
            : 0;
        if (xp > 0 || correta)
        {
            if (xp > 0)
                db.LancamentosXp.Add(new LancamentoXp { UsuarioId = userId, Origem = "QUIZ", Referencia = $"pergunta-{question.Id}-{Guid.NewGuid():N}", Quantidade = xp, CriadoEm = agora });
            await db.SaveChangesAsync(ct);
        }
        var totalDia = xpHoje + xp;
        var mensagem = !correta
            ? "Resposta incorreta. Revise a orientação e tente novamente."
            : xp == 0
                ? "Resposta correta; limite diário de XP do quiz atingido."
                : "Resposta correta!";
        if (xpMissao > 0) mensagem += $" Missão concluída: +{xpMissao} XP.";
        return new RespostaQuizResult(correta, xp + xpMissao, totalDia, 50, mensagem);
    }

    public async Task<ProgressoResult> ProgressoAsync(string userId, string? bairro, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(bairro))
        {
            await AtualizarBairro(userId, bairro, DateTimeOffset.UtcNow, ct);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                var existingProfile = await db.Perfis.SingleOrDefaultAsync(x => x.UsuarioId == userId, ct);
                if (existingProfile is null) throw;
                existingProfile.Bairro = bairro;
                existingProfile.AtualizadoEm = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }
        var total = await db.LancamentosXp.Where(x => x.UsuarioId == userId).SumAsync(x => (int?)x.Quantidade, ct) ?? 0;
        var kg = await db.LancamentosXp.Where(x => x.UsuarioId == userId && x.Origem == "ENTREGA").SumAsync(x => (decimal?)x.PesoKg, ct) ?? 0m;
        var medals = await (from earned in db.MedalhasUsuario where earned.UsuarioId == userId join medal in db.Medalhas on earned.MedalhaId equals medal.Id select medal.Nome).ToListAsync(ct);
        var profile = await db.Perfis.AsNoTracking().SingleOrDefaultAsync(x => x.UsuarioId == userId, ct);
        return new ProgressoResult(userId, total, RegrasGamificacao.Nivel(total), kg, medals, profile?.Bairro);
    }

    public async Task<IReadOnlyList<MissaoResult>> MissoesAsync(string userId, CancellationToken ct)
    {
        var all = await db.Missoes.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);
        var progress = await db.MissoesUsuario.AsNoTracking().Where(x => x.UsuarioId == userId).ToDictionaryAsync(x => x.MissaoId, ct);
        return all.Select(m => progress.TryGetValue(m.Id, out var p)
            ? new MissaoResult(m.Id, m.Titulo, m.Descricao, m.Tipo, m.Meta, m.Xp, p.Progresso, p.ConcluidaEm is not null)
            : new MissaoResult(m.Id, m.Titulo, m.Descricao, m.Tipo, m.Meta, m.Xp, 0, false)).ToArray();
    }

    public async Task<IReadOnlyList<PerguntaQuizResult>> QuizHojeAsync(CancellationToken ct)
        => await db.PerguntasQuiz.AsNoTracking().Where(x => x.Ativa).OrderBy(x => x.Id)
            .Select(x => new PerguntaQuizResult(x.Id, x.Enunciado, x.Alternativas, x.Categoria)).ToListAsync(ct);

    public async Task<IReadOnlyList<RankingItem>> RankingAsync(string? bairro, CancellationToken ct)
    {
        var query = from profile in db.Perfis.AsNoTracking()
                    join xp in db.LancamentosXp.AsNoTracking().GroupBy(x => x.UsuarioId).Select(g => new { UsuarioId = g.Key, Total = g.Sum(x => x.Quantidade) })
                        on profile.UsuarioId equals xp.UsuarioId
                    select new { profile.Bairro, xp.UsuarioId, xp.Total };
        if (!string.IsNullOrWhiteSpace(bairro)) query = query.Where(x => x.Bairro == bairro);
        var rows = await query.OrderByDescending(x => x.Total).ThenBy(x => x.UsuarioId).Take(50).ToListAsync(ct);
        return rows.Select((x, i) => new RankingItem(i + 1, x.UsuarioId, x.Total, RegrasGamificacao.Nivel(x.Total))).ToArray();
    }

    public async Task<IReadOnlyList<MissaoResult>> CriarReforcoAsync(string key, ReforcoRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.UsuarioId) || string.IsNullOrWhiteSpace(request.Motivo)) throw new ArgumentException("UsuarioId e motivo são obrigatórios.");
        return await idempotency.ExecuteAsync(key, "MISSAO_REFORCO", request, async () =>
        {
            var mission = await db.Missoes.SingleOrDefaultAsync(x => x.Id == request.MissaoId, ct) ?? throw new KeyNotFoundException("Missão de reforço não encontrada.");
            var assignment = await db.MissoesUsuario.SingleOrDefaultAsync(x => x.UsuarioId == request.UsuarioId && x.MissaoId == mission.Id, ct);
            if (assignment is null) db.MissoesUsuario.Add(new MissaoUsuario { UsuarioId = request.UsuarioId, MissaoId = mission.Id, Progresso = 0 });
            var result = new MissaoResult(mission.Id, mission.Titulo, $"{mission.Descricao} Motivo da recusa: {request.Motivo}", mission.Tipo, mission.Meta, mission.Xp, assignment?.Progresso ?? 0, assignment?.ConcluidaEm is not null);
            return new[] { result };
        }, ct);
    }

    private async Task<int> CreditoIdentificacaoDoDia(string userId, DateTimeOffset now, CancellationToken ct)
    {
        var (start, end) = HojeEmSaoPaulo(now);
        var total = await db.LancamentosXp.Where(x => x.UsuarioId == userId && x.Origem == "IDENTIFICACAO" && x.CriadoEm >= start && x.CriadoEm < end).SumAsync(x => (int?)x.Quantidade, ct) ?? 0;
        return Math.Min(2, Math.Max(0, 20 - total));
    }

    private async Task AtualizarBairro(string userId, string bairro, DateTimeOffset now, CancellationToken ct)
    {
        var profile = await db.Perfis.SingleOrDefaultAsync(x => x.UsuarioId == userId, ct);
        if (profile is null) db.Perfis.Add(new PerfilGamificacao { UsuarioId = userId, Bairro = bairro, AtualizadoEm = now });
        else { profile.Bairro = bairro; profile.AtualizadoEm = now; }
    }

    private async Task<int> AvancarMissoesAsync(string userId, string tipo, int incremento, DateTimeOffset now, CancellationToken ct)
    {
        var missions = await db.Missoes.AsNoTracking()
            .Where(x => x.Tipo == tipo)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        if (missions.Count == 0) return 0;

        var missionIds = missions.Select(x => x.Id).ToArray();
        var assignments = await db.MissoesUsuario
            .Where(x => x.UsuarioId == userId && missionIds.Contains(x.MissaoId))
            .ToDictionaryAsync(x => x.MissaoId, ct);

        var xpAwarded = 0;
        foreach (var mission in missions)
        {
            if (assignments.TryGetValue(mission.Id, out var assignment) && assignment.ConcluidaEm is not null)
                continue;

            if (assignment is null)
            {
                assignment = new MissaoUsuario { UsuarioId = userId, MissaoId = mission.Id, Progresso = 0 };
                db.MissoesUsuario.Add(assignment);
            }

            assignment.Progresso = Math.Min(mission.Meta, assignment.Progresso + incremento);
            if (assignment.Progresso < mission.Meta) continue;

            assignment.ConcluidaEm = now;
            xpAwarded += mission.Xp;
            db.LancamentosXp.Add(new LancamentoXp
            {
                UsuarioId = userId,
                Origem = "MISSAO",
                Referencia = $"missao-{mission.Id}",
                Quantidade = mission.Xp,
                CriadoEm = now
            });
        }

        return xpAwarded;
    }

    private async Task ConcederMedalhasEntrega(CreditoXpRequest request, DateTimeOffset now, CancellationToken ct)
    {
        var previousDeliveries = db.LancamentosXp.Where(x => x.UsuarioId == request.UsuarioId && x.Origem == "ENTREGA");
        var previousCount = await previousDeliveries.CountAsync(x => x.Referencia != request.Referencia, ct);
        var totalKg = await previousDeliveries.SumAsync(x => (decimal?)x.PesoKg, ct) ?? 0m;
        var medalIds = new List<int>();
        if (previousCount == 0) medalIds.Add(1);
        if (totalKg >= 10) medalIds.Add(2);
        if (request.ResiduoEspecial) medalIds.Add(4);
        foreach (var id in medalIds)
        {
            if (!await db.MedalhasUsuario.AnyAsync(x => x.UsuarioId == request.UsuarioId && x.MedalhaId == id, ct))
                db.MedalhasUsuario.Add(new MedalhaUsuario { UsuarioId = request.UsuarioId, MedalhaId = id, ConquistadaEm = now });
        }
    }

    private static (DateTimeOffset Start, DateTimeOffset End) HojeEmSaoPaulo(DateTimeOffset now)
    {
        var local = TimeZoneInfo.ConvertTime(now, SaoPaulo);
        var startLocal = new DateTimeOffset(local.Year, local.Month, local.Day, 0, 0, 0, SaoPaulo.GetUtcOffset(local));
        return (startLocal.ToUniversalTime(), startLocal.AddDays(1).ToUniversalTime());
    }

    private static TimeZoneInfo FindSaoPaulo()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
    }
}
