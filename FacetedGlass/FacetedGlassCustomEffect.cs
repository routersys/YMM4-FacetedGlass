using System.Numerics;
using System.Runtime.InteropServices;
using Vortice;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace FacetedGlass;

internal sealed class FacetedGlassCustomEffect(IGraphicsDevicesAndContext devices) : D2D1CustomShaderEffectBase(Create<EffectImpl>(devices))
{
    public float Amount { get => GetFloatValue((int)EffectImpl.Properties.Amount); set => SetValue((int)EffectImpl.Properties.Amount, value); }
    public float CellSize { get => GetFloatValue((int)EffectImpl.Properties.CellSize); set => SetValue((int)EffectImpl.Properties.CellSize, value); }
    public float Relief { get => GetFloatValue((int)EffectImpl.Properties.Relief); set => SetValue((int)EffectImpl.Properties.Relief, value); }
    public float Rotation { get => GetFloatValue((int)EffectImpl.Properties.Rotation); set => SetValue((int)EffectImpl.Properties.Rotation, value); }
    public float Refraction { get => GetFloatValue((int)EffectImpl.Properties.Refraction); set => SetValue((int)EffectImpl.Properties.Refraction, value); }
    public float RefractiveIndex { get => GetFloatValue((int)EffectImpl.Properties.RefractiveIndex); set => SetValue((int)EffectImpl.Properties.RefractiveIndex, value); }
    public float Dispersion { get => GetFloatValue((int)EffectImpl.Properties.Dispersion); set => SetValue((int)EffectImpl.Properties.Dispersion, value); }
    public float Reflection { get => GetFloatValue((int)EffectImpl.Properties.Reflection); set => SetValue((int)EffectImpl.Properties.Reflection, value); }
    public float BorderWidth { get => GetFloatValue((int)EffectImpl.Properties.BorderWidth); set => SetValue((int)EffectImpl.Properties.BorderWidth, value); }
    public float LightAngle { get => GetFloatValue((int)EffectImpl.Properties.LightAngle); set => SetValue((int)EffectImpl.Properties.LightAngle, value); }
    public float LightElevation { get => GetFloatValue((int)EffectImpl.Properties.LightElevation); set => SetValue((int)EffectImpl.Properties.LightElevation, value); }
    public int Seed { get => GetIntValue((int)EffectImpl.Properties.Seed); set => SetValue((int)EffectImpl.Properties.Seed, value); }

    [CustomEffect(1)]
    sealed class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
    {
        ConstantBuffer constants = new()
        {
            Amount = 1f,
            CellSize = 72f,
            Relief = 0.55f,
            Refraction = 18f,
            RefractiveIndex = 1.5f,
            Dispersion = 0.35f,
            Reflection = 0.55f,
            BorderWidth = 1f,
            LightAngle = -35f,
            LightElevation = 45f
        };

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Amount)]
        public float Amount { get => constants.Amount; set { constants.Amount = Clamp(value, 0f, 1f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.CellSize)]
        public float CellSize { get => constants.CellSize; set { constants.CellSize = Clamp(value, 4f, 1000f, 72f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Relief)]
        public float Relief { get => constants.Relief; set { constants.Relief = Clamp(value, 0f, 2f, 0.55f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Rotation)]
        public float Rotation { get => constants.Rotation; set { constants.Rotation = Clamp(value, -180f, 180f, 0f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Refraction)]
        public float Refraction { get => constants.Refraction; set { constants.Refraction = Clamp(value, 0f, 512f, 18f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.RefractiveIndex)]
        public float RefractiveIndex { get => constants.RefractiveIndex; set { constants.RefractiveIndex = Clamp(value, 1f, 2.5f, 1.5f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Dispersion)]
        public float Dispersion { get => constants.Dispersion; set { constants.Dispersion = Clamp(value, 0f, 1f, 0.35f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Reflection)]
        public float Reflection { get => constants.Reflection; set { constants.Reflection = Clamp(value, 0f, 2f, 0.55f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.BorderWidth)]
        public float BorderWidth { get => constants.BorderWidth; set { constants.BorderWidth = Clamp(value, 0f, 32f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.LightAngle)]
        public float LightAngle { get => constants.LightAngle; set { constants.LightAngle = Clamp(value, -180f, 180f, -35f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.LightElevation)]
        public float LightElevation { get => constants.LightElevation; set { constants.LightElevation = Clamp(value, 1f, 89f, 45f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Int32, (int)Properties.Seed)]
        public int Seed { get => constants.Seed; set { constants.Seed = Math.Max(value, 0); UpdateConstants(); } }

        public EffectImpl() : base(ShaderResourceUri.Get("FacetedGlass"))
        {
        }

        protected override void UpdateConstants()
        {
            drawInformation?.SetPixelShaderConstantBuffer(constants);
        }

        public override void MapInputRectsToOutputRect(RawRect[] inputRects, RawRect[] inputOpaqueSubRects, out RawRect outputRect, out RawRect outputOpaqueSubRect)
        {
            inputRect = inputRects.Length > 0 ? ClampInputRect(inputRects[0]) : default;
            constants.InputBounds = new Vector4(inputRect.Left, inputRect.Top, inputRect.Right, inputRect.Bottom);
            UpdateConstants();
            outputRect = inputRect;
            outputOpaqueSubRect = default;
        }

        public override void MapOutputRectToInputRects(RawRect outputRect, RawRect[] inputRects)
        {
            if (inputRects.Length == 0)
                return;

            var margin = (int)Math.Ceiling(constants.Refraction * 1.25f + 2f);
            inputRects[0] = new RawRect(
                Saturate((long)outputRect.Left - margin),
                Saturate((long)outputRect.Top - margin),
                Saturate((long)outputRect.Right + margin),
                Saturate((long)outputRect.Bottom + margin));
        }

        static float Clamp(float value, float minimum, float maximum, float fallback)
        {
            if (!float.IsFinite(value))
                return fallback;
            return Math.Clamp(value, minimum, maximum);
        }

        static int Saturate(long value) => (int)Math.Clamp(value, int.MinValue, int.MaxValue);

        [StructLayout(LayoutKind.Sequential)]
        struct ConstantBuffer
        {
            public Vector4 InputBounds;
            public float Amount;
            public float CellSize;
            public float Relief;
            public float Rotation;
            public float Refraction;
            public float RefractiveIndex;
            public float Dispersion;
            public float Reflection;
            public float BorderWidth;
            public float LightAngle;
            public float LightElevation;
            public int Seed;
        }

        public enum Properties
        {
            Amount,
            CellSize,
            Relief,
            Rotation,
            Refraction,
            RefractiveIndex,
            Dispersion,
            Reflection,
            BorderWidth,
            LightAngle,
            LightElevation,
            Seed,
        }
    }
}
