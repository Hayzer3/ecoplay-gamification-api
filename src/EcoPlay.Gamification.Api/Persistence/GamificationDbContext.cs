using System.Text.Json;
using EcoPlay.Gamification.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EcoPlay.Gamification.Api.Persistence;

public sealed class GamificationDbContext(DbContextOptions<GamificationDbContext> options) : DbContext(options)
{
    public DbSet<LancamentoXp> LancamentosXp => Set<LancamentoXp>();
    public DbSet<RequisicaoIdempotente> RequisicoesIdempotentes => Set<RequisicaoIdempotente>();
    public DbSet<PerguntaQuiz> PerguntasQuiz => Set<PerguntaQuiz>();
    public DbSet<Missao> Missoes => Set<Missao>();
    public DbSet<MissaoUsuario> MissoesUsuario => Set<MissaoUsuario>();
    public DbSet<Medalha> Medalhas => Set<Medalha>();
    public DbSet<MedalhaUsuario> MedalhasUsuario => Set<MedalhaUsuario>();
    public DbSet<PerfilGamificacao> Perfis => Set<PerfilGamificacao>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<LancamentoXp>(e =>
        {
            e.ToTable("lancamento_xp"); e.HasKey(x => x.Id);
            e.Property(x => x.UsuarioId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Origem).HasMaxLength(30).IsRequired();
            e.Property(x => x.Referencia).HasMaxLength(150);
            e.Property(x => x.PesoKg).HasPrecision(8, 2);
            e.HasIndex(x => new { x.UsuarioId, x.CriadoEm });
        });
        model.Entity<RequisicaoIdempotente>(e =>
        {
            e.ToTable("requisicao_idempotente"); e.HasKey(x => x.Id);
            e.Property(x => x.Chave).HasMaxLength(150).IsRequired();
            e.Property(x => x.Operacao).HasMaxLength(60).IsRequired();
            e.Property(x => x.HashRequisicao).HasMaxLength(64).IsRequired();
            e.Property(x => x.RespostaJson).HasColumnType("CLOB").IsRequired();
            e.HasIndex(x => x.Chave).IsUnique();
        });
        model.Entity<PerguntaQuiz>(e =>
        {
            e.ToTable("pergunta_quiz"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Enunciado).HasMaxLength(500).IsRequired();
            var alternatives = e.Property(x => x.Alternativas).HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>()).HasColumnType("CLOB");
            alternatives.Metadata.SetValueComparer(new ValueComparer<string[]>(
                (left, right) => left != null && right != null && left.SequenceEqual(right),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value.ToArray()));
            e.Property(x => x.Categoria).HasMaxLength(40).IsRequired();
            
        });
        model.Entity<Missao>(e =>
        {
            e.ToTable("missao"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Titulo).HasMaxLength(120).IsRequired(); e.Property(x => x.Descricao).HasMaxLength(500).IsRequired(); e.Property(x => x.Tipo).HasMaxLength(30).IsRequired();
            
        });
        model.Entity<MissaoUsuario>(e =>
        {
            e.ToTable("missao_usuario"); e.HasKey(x => x.Id);
            e.Property(x => x.UsuarioId).HasMaxLength(100).IsRequired(); e.HasIndex(x => new { x.UsuarioId, x.MissaoId }).IsUnique();
            e.HasOne<Missao>().WithMany().HasForeignKey(x => x.MissaoId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Medalha>(e =>
        {
            e.ToTable("medalha"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedNever(); e.Property(x => x.Nome).HasMaxLength(100).IsRequired(); e.Property(x => x.Regra).HasMaxLength(300).IsRequired();
            e.HasData(
                new Medalha { Id = 1, Nome = "Primeira Entrega", Regra = "Concluir a primeira entrega." },
                new Medalha { Id = 2, Nome = "10 kg Entregues", Regra = "Acumular 10 kg em entregas confirmadas." },
                new Medalha { Id = 3, Nome = "Separação Perfeita", Regra = "Concluir cinco entregas seguidas sem recusa." },
                new Medalha { Id = 4, Nome = "Caçador de Pilhas", Regra = "Entregar o primeiro resíduo especial." },
                new Medalha { Id = 5, Nome = "Mestre do Quiz", Regra = "Acertar dez perguntas seguidas." });
        });
        model.Entity<MedalhaUsuario>(e =>
        {
            e.ToTable("medalha_usuario"); e.HasKey(x => x.Id); e.Property(x => x.UsuarioId).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.UsuarioId, x.MedalhaId }).IsUnique();
            e.HasOne<Medalha>().WithMany().HasForeignKey(x => x.MedalhaId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<PerfilGamificacao>(e =>
        {
            e.ToTable("perfil"); e.HasKey(x => x.UsuarioId); e.Property(x => x.UsuarioId).HasMaxLength(100); e.Property(x => x.Bairro).HasMaxLength(100);
        });
    }
}

