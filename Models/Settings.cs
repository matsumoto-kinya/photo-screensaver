using System.Text.Json.Serialization;

namespace MyPhotoScreensaver.Models;

public class Settings
{
    public string ImageFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    public int DisplaySeconds { get; set; } = 5;
    public TransitionType Transition { get; set; } = TransitionType.Fade;
    public double TransitionSpeed { get; set; } = 0.8; // seconds
    public FitMode FitMode { get; set; } = FitMode.Letterbox;
    public bool Shuffle { get; set; } = true;

    /// <summary>Transition が Panel のときのセル入れ替え方。それ以外では無視される。</summary>
    public PanelMode PanelMode { get; set; } = PanelMode.Conveyor;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransitionType
{
    Fade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    ZoomIn,
    Dissolve,
    Panel,
    Random
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FitMode
{
    Letterbox,
    Crop
}

/// <summary>
/// Panel モードでのセルの入れ替え方。
/// Conveyor 以外はセルを移動させず、その場で画像だけを差し替える
/// （画像のアスペクト比が変わるぶん、その行の再ジャスティファイは走る）。
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PanelMode
{
    /// <summary>行の端 1 枚がコンベア式に入れ替わる（従来の動作）。</summary>
    Conveyor,
    /// <summary>ランダムな 1 セルをその場でクロスフェード。</summary>
    SingleCell,
    /// <summary>ランダムな複数セルを、縮小→等倍のズームで差し替え。</summary>
    CellZoom,
    /// <summary>ランダムな複数セルを、横に潰して開くフリップで差し替え。</summary>
    CellFlip,
    /// <summary>対角線上のセルを順にクロスフェード。</summary>
    DiagonalWave,
    /// <summary>同じ列のセルを上から順にクロスフェード。</summary>
    ColumnWave
}
