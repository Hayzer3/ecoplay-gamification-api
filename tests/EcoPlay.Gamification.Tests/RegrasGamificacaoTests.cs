using EcoPlay.Gamification.Api.Domain;

namespace EcoPlay.Gamification.Tests;

public sealed class RegrasGamificacaoTests
{
    [Theory]
    [InlineData(0, "Semente")]
    [InlineData(199, "Semente")]
    [InlineData(200, "Broto")]
    [InlineData(600, "Muda")]
    [InlineData(1500, "Árvore")]
    [InlineData(4000, "Floresta")]
    public void Nivel_usa_limites_planejados(int xp, string esperado)
        => Assert.Equal(esperado, RegrasGamificacao.Nivel(xp));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.5, 20)]
    [InlineData(2.1, 21)]
    [InlineData(30, 200)]
    public void XpEntrega_aplica_dez_por_kg_com_minimo_e_teto(decimal kg, int esperado)
        => Assert.Equal(esperado, RegrasGamificacao.XpEntrega(kg));
}
