using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EcoPlay.Gamification.Api.Domain;
using EcoPlay.Gamification.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcoPlay.Gamification.Api.Services;

public sealed class IdempotencyConflictException(string message) : Exception(message);

public sealed class IdempotencyService(GamificationDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T> ExecuteAsync<T>(string key, string operation, object request, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 150)
            throw new ArgumentException("Idempotency-Key é obrigatório e deve ter até 150 caracteres.");

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, JsonOptions))));
        var previous = await db.RequisicoesIdempotentes.AsNoTracking().SingleOrDefaultAsync(x => x.Chave == key, cancellationToken);
        if (previous is not null) return Replay<T>(previous, operation, hash);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await work();
            db.RequisicoesIdempotentes.Add(new RequisicaoIdempotente
            {
                Chave = key, Operacao = operation, HashRequisicao = hash,
                RespostaJson = JsonSerializer.Serialize(result, JsonOptions)
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            var winner = await db.RequisicoesIdempotentes.AsNoTracking().SingleOrDefaultAsync(x => x.Chave == key, cancellationToken);
            if (winner is null) throw;
            return Replay<T>(winner, operation, hash);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private static T Replay<T>(RequisicaoIdempotente previous, string operation, string hash)
    {
        if (previous.Operacao != operation || previous.HashRequisicao != hash)
            throw new IdempotencyConflictException("Esta chave Idempotency-Key já foi usada com outra operação ou conteúdo.");
        return JsonSerializer.Deserialize<T>(previous.RespostaJson, JsonOptions)
            ?? throw new InvalidOperationException("Não foi possível recuperar a resposta idempotente armazenada.");
    }
}
