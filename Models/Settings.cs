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
