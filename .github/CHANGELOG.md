# v1.0.0 - 多面体ガラス for YMM4

YukkuriMovieMaker4 向けの多面体ガラスエフェクトプラグインの初回リリースです。
Direct2D カスタムピクセルシェーダーで映像を三角形の面に分割し、面ごとの屈折・色分散・反射・照明を合成する映像エフェクトプラグインです。
共有頂点の高さから各面の法線を求め、波長ごとに異なる屈折率で色分散を生成し、
Fresnel 反射・鏡面ハイライト・面境界の反射線を方向性のある光源で合成します。
8 言語対応 UI を備えます。

---

## 新機能

### 1. パラメーター正規化（ParameterNormalizer）

`internal static class ParameterNormalizer` は数値パラメーターのクランプと非有限値のフォールバックを提供します。

| メソッド | 説明 |
|---|---|
| `Finite(double, float, float, float)` | 値が有限なら [minimum, maximum] にクランプして float へ変換。非有限値（NaN・±∞）は fallback を返す |
| `Percent(double, float, float, float)` | `value / 100` を求め、`Finite` で [minimum, maximum] にクランプ |

`FacetedGlassEffectProcessor` は `using static` により両メソッドを参照します。

---

### 2. ピクセルシェーダー（FacetedGlass.hlsl）

ピクセルシェーダー `main`（`ps_5_0`）はフレームごとに、シーン座標を三角形格子へ写像し、所属する三角形の面法線から屈折・色分散・反射・照明を計算します。

#### 三角形格子と面法線

シーン座標を中心からの相対座標へ回転（`rotation`）し、格子座標 `latticeU`・`latticeV` を求めて所属する三角形の 3 頂点を決定します。各頂点の高さは `seed` を含む `Hash32` ハッシュで決定論的に生成し、高さスケールは `cellSize × relief × 0.35` です。3 頂点の外積で面法線を求め、`rotation` で平面成分を回転します。

#### 色分散

`CauchyIndex` で波長別の屈折率を計算し、赤・緑・青を異なる波長でサンプリングして色分散を生成します。

| 定数 | 値 | 用途 |
|---|---|---|
| `WavelengthF` | 486.1 nm | コーシー係数の基準（F 線） |
| `WavelengthD` | 587.6 nm | 基準屈折率の波長（D 線） |
| `WavelengthC` | 656.3 nm | コーシー係数の基準（C 線） |

`deltaFC = 0.04 × dispersion`、`b = deltaFC / (1/F² − 1/C²)`、`a = refractiveIndex − b/D²` とし、波長 λ における屈折率を `a + b/λ²` で求めます。

| チャンネル | サンプリング波長 |
|---|---|
| R | 610 nm |
| G | 550 nm |
| B | 460 nm |

#### 反射と照明

`lightAngle`（方位）と `lightElevation`（仰角）から光源ベクトルを構成します。

| 項目 | 式 |
|---|---|
| 拡散 | `0.78 + 0.22 × saturate(dot(normal, light))` |
| 鏡面 | `pow(saturate(dot(reflect(-light, normal), view)), 48)` |
| Fresnel | `Schlick(indexG, dot(normal, view))`（`f0 = ((n−1)/(n+1))²`） |
| 面境界 | `1 − smoothstep(borderWidth, borderWidth + antialias, edgeDistance)`（`borderWidth ≤ 0` のとき 0） |

`edgeDistance` は三角形の各辺までの距離の最小値、`antialias = max(fwidth(edgeDistance), 0.5)` です。

#### 合成

`reflectance = reflection × (fresnel + specular × 0.75 + border × 0.35)`、`glass = refracted × diffuse × (1 − saturate(fresnel × reflection × 0.35)) + reflectance` を求め、最終出力を `lerp(source, faceted, amount)` で合成します。`amount ≤ 0` または `source.a ≤ 0` のときはソースをそのまま返します。

---

### 3. カスタムシェーダーエフェクト（FacetedGlassCustomEffect）

`internal sealed class FacetedGlassCustomEffect(IGraphicsDevicesAndContext) : D2D1CustomShaderEffectBase` は `[CustomEffect(1)]` の 1 入力エフェクトとして宣言されます（入力 0: ソース画像）。

公開プロパティは `GetFloatValue`・`GetIntValue`・`SetValue` を介して `EffectImpl` へ転送します。

| プロパティ | 型 | 範囲 |
|---|---|---|
| `Amount` | `float` | 0〜1 |
| `CellSize` | `float` | 4〜1000 |
| `Relief` | `float` | 0〜2 |
| `Rotation` | `float` | -180〜180 |
| `Refraction` | `float` | 0〜512 |
| `RefractiveIndex` | `float` | 1〜2.5 |
| `Dispersion` | `float` | 0〜1 |
| `Reflection` | `float` | 0〜2 |
| `BorderWidth` | `float` | 0〜32 |
| `LightAngle` | `float` | -180〜180 |
| `LightElevation` | `float` | 1〜89 |
| `Seed` | `int` | 0 以上 |

#### EffectImpl（内部 sealed クラス）

各プロパティの setter は `Clamp(value, minimum, maximum, fallback)`（非有限値は fallback）で値を制限し、`UpdateConstants` で定数バッファーを更新します。`Seed` は `Math.Max(value, 0)` で 0 以上に制限します。

`ConstantBuffer` 構造体（`LayoutKind.Sequential`）のレイアウトは以下のとおりです。

| フィールド | 型 | 説明 |
|---|---|---|
| `InputBounds` | `Vector4` | 入力矩形（Left, Top, Right, Bottom） |
| `Amount` | `float` | 合成強度 |
| `CellSize` | `float` | 面サイズ |
| `Relief` | `float` | 凹凸 |
| `Rotation` | `float` | 分割角度 |
| `Refraction` | `float` | 屈折 |
| `RefractiveIndex` | `float` | 屈折率 |
| `Dispersion` | `float` | 分散 |
| `Reflection` | `float` | 反射 |
| `BorderWidth` | `float` | 面境界 |
| `LightAngle` | `float` | 光源角度 |
| `LightElevation` | `float` | 光源高度 |
| `Seed` | `int` | シード |

`MapInputRectsToOutputRect` は入力 0 の矩形を `ClampInputRect` でクランプして出力矩形に設定し、`InputBounds` を定数バッファーに書き込みます。`MapOutputRectToInputRects` は `ceil(Refraction × 1.25 + 2)` のマージンだけ入力矩形を拡張します。

シェーダーリソース: `pack://application:,,,/FacetedGlass;component/Shaders/FacetedGlass.cso`（ps_5_0、`ShaderResourceUri.Get` が生成）

---

### 4. エフェクト定義（FacetedGlassEffect）

`public sealed class FacetedGlassEffect : VideoEffectBase` を継承します。

`[VideoEffect]` 属性は以下のパラメーターで宣言されます。

- 表示名：`Texts.EffectName`（ローカライズキー）
- カテゴリー：`VideoEffectCategories.Filtering`・`VideoEffectCategories.Decoration`
- 検索タグ：`TagGlass`・`TagPrism`・`TagRefraction`・`TagFacet`（「ガラス」・「プリズム」・「屈折」・「多面体」）
- `IsAviUtlSupported = false` により AviUtl 向け EXO 出力は非対応
- `ResourceType = typeof(Texts)` でローカライズリソースを指定

`Label` プロパティは `Texts.EffectName` を返します。

公開プロパティは以下のとおりです（内部範囲は `Animation` の最小値・最大値）。

**基本グループ**

| プロパティ | 型 | デフォルト | 内部範囲 |
|---|---|---|---|
| `Amount` | `Animation` | 100 | 0〜100 |

**面構造グループ**

| プロパティ | 型 | デフォルト | 内部範囲 |
|---|---|---|---|
| `CellSize` | `Animation` | 72 | 4〜1000 |
| `Relief` | `Animation` | 55 | 0〜200 |
| `Rotation` | `Animation` | 0 | -180〜180 |
| `Seed` | `int` | 0 | 0〜2147483647 |

**光学グループ**

| プロパティ | 型 | デフォルト | 内部範囲 |
|---|---|---|---|
| `Refraction` | `Animation` | 18 | 0〜512 |
| `RefractiveIndex` | `Animation` | 1.5 | 1〜2.5 |
| `Dispersion` | `Animation` | 35 | 0〜100 |

**外観グループ**

| プロパティ | 型 | デフォルト | 内部範囲 |
|---|---|---|---|
| `Reflection` | `Animation` | 55 | 0〜200 |
| `BorderWidth` | `Animation` | 1 | 0〜32 |

**照明グループ**

| プロパティ | 型 | デフォルト | 内部範囲 |
|---|---|---|---|
| `LightAngle` | `Animation` | -35 | -180〜180 |
| `LightElevation` | `Animation` | 45 | 1〜89 |

`GetAnimatables` は `Amount`・`CellSize`・`Relief`・`Rotation`・`Refraction`・`RefractiveIndex`・`Dispersion`・`Reflection`・`BorderWidth`・`LightAngle`・`LightElevation` を yield します（`Seed` はアニメーション対象外）。

`CreateExoVideoFilters` は空のシーケンスを返します（EXO 非対応）。`CreateVideoEffect` は `FacetedGlassEffectProcessor` を生成します。

---

### 5. エフェクトプロセッサー（FacetedGlassEffectProcessor）

`internal sealed class FacetedGlassEffectProcessor : VideoEffectProcessorBase` を継承します。

#### Update メソッド

`IsPassThroughEffect || effect is null` の場合は `effectDescription.DrawDescription` をそのまま返します。

各フレームで `ParameterNormalizer` を用いて以下の値を計算します。

| パラメーター | 変換 |
|---|---|
| `Amount` | `value / 100` を [0, 1] にクランプ（非有限値は 1） |
| `CellSize` | 有限値を [4, 1000] にクランプ（非有限値は 72） |
| `Relief` | `value / 100` を [0, 2] にクランプ（非有限値は 0.55） |
| `Rotation` | 有限値を [-180, 180] にクランプ（非有限値は 0） |
| `Refraction` | 有限値を [0, 512] にクランプ（非有限値は 18） |
| `RefractiveIndex` | 有限値を [1, 2.5] にクランプ（非有限値は 1.5） |
| `Dispersion` | `value / 100` を [0, 1] にクランプ（非有限値は 0.35） |
| `Reflection` | `value / 100` を [0, 2] にクランプ（非有限値は 0.55） |
| `BorderWidth` | 有限値を [0, 32] にクランプ（非有限値は 1） |
| `LightAngle` | 有限値を [-180, 180] にクランプ（非有限値は -35） |
| `LightElevation` | 有限値を [1, 89] にクランプ（非有限値は 45） |
| `Seed` | `Math.Max(item.Seed, 0)` |

計算した値は `Parameters`（readonly record struct）にまとめ、`isFirst` または前フレームと値が異なる場合のみ各プロパティを `effect` へ転送します。

#### CreateEffect / setInput / ClearEffectChain

`CreateEffect` は `FacetedGlassCustomEffect` を生成し、`IsEnabled` が false の場合は破棄して `null` を返します。

`setInput` は `effect?.SetInput(0, input, true)` を呼び出します。

`ClearEffectChain` は `effect?.SetInput(0, null, true)` を呼び出し、`isFirst = true` にリセットします。

---

### 6. ローカライズ（Texts）

`Texts` クラスは `[AutoGenLocalizer]` 属性を持つ `partial` クラスとして宣言されます。
`YukkuriMovieMaker.Generator` のソースジェネレーターが `Texts.csv` を処理し、各ロケールのリソースファイルを自動生成します。

対応言語：日本語（`ja-jp`）・英語（`en-us`）・中国語簡体字（`zh-cn`）・中国語繁体字（`zh-tw`）・韓国語（`ko-kr`）・スペイン語（`es-es`）・アラビア語（`ar-sa`）・インドネシア語（`id-id`）

ローカライズキーの一覧は以下のとおりです。

| キー | ja-jp |
|---|---|
| `EffectName` | 多面体ガラス |
| `BasicGroup` | 基本 |
| `GeometryGroup` | 面構造 |
| `OpticsGroup` | 光学 |
| `AppearanceGroup` | 外観 |
| `LightingGroup` | 照明 |
| `AmountName` | 強さ |
| `AmountDesc` | 元映像と多面体ガラスの合成量です。 |
| `CellSizeName` | 面サイズ |
| `CellSizeDesc` | 三角形の一辺の長さです。 |
| `ReliefName` | 凹凸 |
| `ReliefDesc` | 共有頂点の高さ差による面の傾きです。 |
| `RotationName` | 分割角度 |
| `RotationDesc` | 三角形格子全体の角度です。 |
| `SeedName` | シード |
| `SeedDesc` | 面の高さ配置を決定する固定値です。 |
| `RefractionName` | 屈折 |
| `RefractionDesc` | 面法線による映像のずれ量です。 |
| `RefractiveIndexName` | 屈折率 |
| `RefractiveIndexDesc` | 基準波長におけるガラスの屈折率です。 |
| `DispersionName` | 分散 |
| `DispersionDesc` | 波長ごとの屈折率差による色分離です。 |
| `ReflectionName` | 反射 |
| `ReflectionDesc` | Fresnel反射と鏡面ハイライトの強さです。 |
| `BorderWidthName` | 面境界 |
| `BorderWidthDesc` | 三角形の境界に現れる反射線の幅です。 |
| `LightAngleName` | 光源角度 |
| `LightAngleDesc` | 平面上の光源方向です。 |
| `LightElevationName` | 光源高度 |
| `LightElevationDesc` | 面に対する光源の高さです。 |
| `TagGlass` | ガラス |
| `TagPrism` | プリズム |
| `TagRefraction` | 屈折 |
| `TagFacet` | 多面体 |