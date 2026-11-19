namespace EcoPlay.Gamification.Api.Domain;

public sealed record CreditoXpRequest(string UsuarioId, string Origem, string Referencia, decimal? PesoKg = null, bool ResiduoEspecial = false);
public sealed record ReforcoRequest(string UsuarioId, int MissaoId, string Motivo);
public sealed record RespostaQuizRequest(int PerguntaId, int IndiceAlternativa);
public sealed record RespostaQuizResult(bool Correta, int XpConcedido, int XpQuizHoje, int LimiteDiario, string Mensagem);
public sealed record CreditoXpResult(string UsuarioId, int XpCreditado, int XpTotal, string Nivel);
public sealed record ProgressoResult(string UsuarioId, int XpTotal, string Nivel, decimal KgEntregues, IReadOnlyList<string> Medalhas, string? Bairro);
public sealed record PerguntaQuizResult(int Id, string Enunciado, IReadOnlyList<string> Alternativas, string Categoria);
public sealed record MissaoResult(int Id, string Titulo, string Descricao, string Tipo, int Meta, int Xp, int Progresso, bool Concluida);
public sealed record RankingItem(int Posicao, string UsuarioId, int XpTotal, string Nivel);

public static class RegrasGamificacao
{
    public static string Nivel(int xp) => xp switch
    {
        >= 4000 => "Floresta",
        >= 1500 => "Árvore",
        >= 600 => "Muda",
        >= 200 => "Broto",
        _ => "Semente"
    };

    public static int XpEntrega(decimal pesoKg)
    {
        if (pesoKg <= 0) return 0;
        return Math.Clamp((int)Math.Floor(pesoKg * 10m), 20, 200);
    }
}
