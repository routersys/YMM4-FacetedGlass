namespace FacetedGlass.Tests;

public sealed class ParameterNormalizerTests
{
    [Fact]
    public void Finite_PassesThroughValueWithinRange()
    {
        Assert.Equal(0.5f, ParameterNormalizer.Finite(0.5d, 0f, 1f, 0.25f));
    }

    [Fact]
    public void Finite_ClampsBelowMinimum()
    {
        Assert.Equal(0f, ParameterNormalizer.Finite(-3d, 0f, 1f, 0.25f));
    }

    [Fact]
    public void Finite_ClampsAboveMaximum()
    {
        Assert.Equal(1f, ParameterNormalizer.Finite(4d, 0f, 1f, 0.25f));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Finite_ReturnsFallbackForNonFiniteInput(double value)
    {
        Assert.Equal(0.25f, ParameterNormalizer.Finite(value, 0f, 1f, 0.25f));
    }

    [Fact]
    public void Percent_DividesInputByOneHundred()
    {
        Assert.Equal(0.55f, ParameterNormalizer.Percent(55d, 0f, 2f, 0.5f), 6);
    }

    [Fact]
    public void Percent_ClampsAfterConversion()
    {
        Assert.Equal(2f, ParameterNormalizer.Percent(250d, 0f, 2f, 0.5f));
    }

    [Fact]
    public void Percent_ReturnsFallbackForNonFiniteInput()
    {
        Assert.Equal(0.5f, ParameterNormalizer.Percent(double.NaN, 0f, 2f, 0.5f));
    }
}
