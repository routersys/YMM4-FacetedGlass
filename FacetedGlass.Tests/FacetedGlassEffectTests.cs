namespace FacetedGlass.Tests;

public sealed class FacetedGlassEffectTests
{
    static double ValueAt(YukkuriMovieMaker.Commons.Animation animation) => animation.GetValue(0, 1, 30);

    [Fact]
    public void DefaultParameterValuesMatchSpecification()
    {
        var effect = new FacetedGlassEffect();

        Assert.Equal(100d, ValueAt(effect.Amount), 6);
        Assert.Equal(72d, ValueAt(effect.CellSize), 6);
        Assert.Equal(65d, ValueAt(effect.Irregularity), 6);
        Assert.Equal(55d, ValueAt(effect.Relief), 6);
        Assert.Equal(0d, ValueAt(effect.Rotation), 6);
        Assert.Equal(0d, ValueAt(effect.Evolution), 6);
        Assert.Equal(18d, ValueAt(effect.Refraction), 6);
        Assert.Equal(1.5d, ValueAt(effect.RefractiveIndex), 6);
        Assert.Equal(35d, ValueAt(effect.Dispersion), 6);
        Assert.Equal(55d, ValueAt(effect.Reflection), 6);
        Assert.Equal(40d, ValueAt(effect.Glint), 6);
        Assert.Equal(1d, ValueAt(effect.BorderWidth), 6);
        Assert.Equal(-35d, ValueAt(effect.LightAngle), 6);
        Assert.Equal(45d, ValueAt(effect.LightElevation), 6);
    }

    [Fact]
    public void Seed_DefaultsToZero()
    {
        var effect = new FacetedGlassEffect();

        Assert.Equal(0, effect.Seed);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(-2147483648, 0)]
    [InlineData(0, 0)]
    [InlineData(1234, 1234)]
    public void Seed_ClampsNegativeInputToZero(int input, int expected)
    {
        var effect = new FacetedGlassEffect { Seed = input };

        Assert.Equal(expected, effect.Seed);
    }

    [Fact]
    public void CreateExoVideoFilters_ReturnsEmpty()
    {
        var effect = new FacetedGlassEffect();

        Assert.Empty(effect.CreateExoVideoFilters(0, null!));
    }
}
