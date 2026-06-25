using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace FacetedGlass;

[VideoEffect(nameof(Texts.EffectName), [VideoEffectCategories.Filtering, VideoEffectCategories.Decoration], [nameof(Texts.TagGlass), nameof(Texts.TagPrism), nameof(Texts.TagRefraction), nameof(Texts.TagFacet)], IsAviUtlSupported = false, ResourceType = typeof(Texts))]
public sealed class FacetedGlassEffect : VideoEffectBase
{
    public override string Label => Texts.EffectName;

    [Display(GroupName = nameof(Texts.BasicGroup), Name = nameof(Texts.AmountName), Description = nameof(Texts.AmountDesc), Order = 0, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Amount { get; } = new(100, 0, 100);

    [Display(GroupName = nameof(Texts.GeometryGroup), Name = nameof(Texts.CellSizeName), Description = nameof(Texts.CellSizeDesc), Order = 10, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "px", 4d, 300d)]
    public Animation CellSize { get; } = new(72, 4, 1000);

    [Display(GroupName = nameof(Texts.GeometryGroup), Name = nameof(Texts.ReliefName), Description = nameof(Texts.ReliefDesc), Order = 11, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 200d)]
    public Animation Relief { get; } = new(55, 0, 200);

    [Display(GroupName = nameof(Texts.GeometryGroup), Name = nameof(Texts.RotationName), Description = nameof(Texts.RotationDesc), Order = 12, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "°", -180d, 180d)]
    public Animation Rotation { get; } = new(0, -180, 180);

    [Display(GroupName = nameof(Texts.GeometryGroup), Name = nameof(Texts.SeedName), Description = nameof(Texts.SeedDesc), Order = 13, ResourceType = typeof(Texts))]
    [Range(0, 2147483647)]
    [DefaultValue(0)]
    [TextBoxSlider("F0", "", 0, 10000)]
    public int Seed { get => seed; set => Set(ref seed, Math.Max(value, 0)); }
    int seed;

    [Display(GroupName = nameof(Texts.OpticsGroup), Name = nameof(Texts.RefractionName), Description = nameof(Texts.RefractionDesc), Order = 20, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "px", 0d, 100d)]
    public Animation Refraction { get; } = new(18, 0, 512);

    [Display(GroupName = nameof(Texts.OpticsGroup), Name = nameof(Texts.RefractiveIndexName), Description = nameof(Texts.RefractiveIndexDesc), Order = 21, ResourceType = typeof(Texts))]
    [AnimationSlider("F2", "", 1d, 2.5d)]
    public Animation RefractiveIndex { get; } = new(1.5, 1, 2.5);

    [Display(GroupName = nameof(Texts.OpticsGroup), Name = nameof(Texts.DispersionName), Description = nameof(Texts.DispersionDesc), Order = 22, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Dispersion { get; } = new(35, 0, 100);

    [Display(GroupName = nameof(Texts.AppearanceGroup), Name = nameof(Texts.ReflectionName), Description = nameof(Texts.ReflectionDesc), Order = 30, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0d, 200d)]
    public Animation Reflection { get; } = new(55, 0, 200);

    [Display(GroupName = nameof(Texts.AppearanceGroup), Name = nameof(Texts.BorderWidthName), Description = nameof(Texts.BorderWidthDesc), Order = 31, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "px", 0d, 10d)]
    public Animation BorderWidth { get; } = new(1, 0, 32);

    [Display(GroupName = nameof(Texts.LightingGroup), Name = nameof(Texts.LightAngleName), Description = nameof(Texts.LightAngleDesc), Order = 40, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "°", -180d, 180d)]
    public Animation LightAngle { get; } = new(-35, -180, 180);

    [Display(GroupName = nameof(Texts.LightingGroup), Name = nameof(Texts.LightElevationName), Description = nameof(Texts.LightElevationDesc), Order = 41, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "°", 1d, 89d)]
    public Animation LightElevation { get; } = new(45, 1, 89);

    public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription) => [];

    public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices) => new FacetedGlassEffectProcessor(devices, this);

    protected override IEnumerable<IAnimatable> GetAnimatables() => [Amount, CellSize, Relief, Rotation, Refraction, RefractiveIndex, Dispersion, Reflection, BorderWidth, LightAngle, LightElevation];
}
