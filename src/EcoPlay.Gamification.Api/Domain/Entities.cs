namespace EcoPlay.Gamification.Api.Domain;

public sealed class LancamentoXp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UsuarioId { get; set; }
    public required string Origem { get; set; }
    public string? Referencia { get; set; }
    public int Quantidade { get; set; }
    public decimal? PesoKg { get; set; }
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RequisicaoIdempotente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Chave { get; set; }
    public required string Operacao { get; set; }
    public required string HashRequisicao { get; set; }
    public required string RespostaJson { get; set; }
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PerguntaQuiz
{
    public int Id { get; set; }
    public required string Enunciado { get; set; }
    public required string[] Alternativas { get; set; }
    public int IndiceCorreto { get; set; }
    public required string Categoria { get; set; }
    public bool Ativa { get; set; } = true;
}

public sealed class Missao
{
    public int Id { get; set; }
    public required string Titulo { get; set; }
    public required string Descricao { get; set; }
    public required string Tipo { get; set; }
    public int Meta { get; set; }
    public int Xp { get; set; }
}

public sealed class MissaoUsuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UsuarioId { get; set; }
    public int MissaoId { get; set; }
    public int Progresso { get; set; }
    public DateTimeOffset? ConcluidaEm { get; set; }
}

public sealed class Medalha
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public required string Regra { get; set; }
}

public sealed class MedalhaUsuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UsuarioId { get; set; }
    public int MedalhaId { get; set; }
    public DateTimeOffset ConquistadaEm { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PerfilGamificacao
{
    public required string UsuarioId { get; set; }
    public string? Bairro { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; } = DateTimeOffset.UtcNow;
}
