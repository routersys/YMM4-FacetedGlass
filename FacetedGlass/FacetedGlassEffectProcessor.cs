using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Player.Video.Effects;
using static FacetedGlass.ParameterNormalizer;

namespace FacetedGlass;

internal sealed class FacetedGlassEffectProcessor(IGraphicsDevicesAndContext devices, FacetedGlassEffect item) : VideoEffectProcessorBase(devices)
{
    readonly FacetedGlassEffect item = item;
    FacetedGlassCustomEffect? effect;
    Parameters parameters;
    bool isFirst = true;

    public override DrawDescription Update(EffectDescription effectDescription)
    {
        if (IsPassThroughEffect || effect is null)
            return effectDescription.DrawDescription;

        var frame = effectDescription.ItemPosition.Frame;
        var length = effectDescription.ItemDuration.Frame;
        var fps = effectDescription.FPS;
        var next = new Parameters(
            Percent(item.Amount.GetValue(frame, length, fps), 0f, 1f, 1f),
            Finite(item.CellSize.GetValue(frame, length, fps), 4f, 1000f, 72f),
            Percent(item.Relief.GetValue(frame, length, fps), 0f, 2f, 0.55f),
            Finite(item.Rotation.GetValue(frame, length, fps), -180f, 180f, 0f),
            Finite(item.Refraction.GetValue(frame, length, fps), 0f, 512f, 18f),
            Finite(item.RefractiveIndex.GetValue(frame, length, fps), 1f, 2.5f, 1.5f),
            Percent(item.Dispersion.GetValue(frame, length, fps), 0f, 1f, 0.35f),
            Percent(item.Reflection.GetValue(frame, length, fps), 0f, 2f, 0.55f),
            Finite(item.BorderWidth.GetValue(frame, length, fps), 0f, 32f, 1f),
            Finite(item.LightAngle.GetValue(frame, length, fps), -180f, 180f, -35f),
            Finite(item.LightElevation.GetValue(frame, length, fps), 1f, 89f, 45f),
            Math.Max(item.Seed, 0));

        if (isFirst || parameters != next)
        {
            effect.Amount = next.Amount;
            effect.CellSize = next.CellSize;
            effect.Relief = next.Relief;
            effect.Rotation = next.Rotation;
            effect.Refraction = next.Refraction;
            effect.RefractiveIndex = next.RefractiveIndex;
            effect.Dispersion = next.Dispersion;
            effect.Reflection = next.Reflection;
            effect.BorderWidth = next.BorderWidth;
            effect.LightAngle = next.LightAngle;
            effect.LightElevation = next.LightElevation;
            effect.Seed = next.Seed;
            parameters = next;
            isFirst = false;
        }

        return effectDescription.DrawDescription;
    }

    protected override ID2D1Image? CreateEffect(IGraphicsDevicesAndContext devices)
    {
        effect = new FacetedGlassCustomEffect(devices);
        if (!effect.IsEnabled)
        {
            effect.Dispose();
            effect = null;
            return null;
        }

        disposer.Collect(effect);
        var output = effect.Output;
        disposer.Collect(output);
        return output;
    }

    protected override void setInput(ID2D1Image? input)
    {
        effect?.SetInput(0, input, true);
    }

    protected override void ClearEffectChain()
    {
        effect?.SetInput(0, null, true);
        isFirst = true;
    }

    readonly record struct Parameters(
        float Amount,
        float CellSize,
        float Relief,
        float Rotation,
        float Refraction,
        float RefractiveIndex,
        float Dispersion,
        float Reflection,
        float BorderWidth,
        float LightAngle,
        float LightElevation,
        int Seed);
}
