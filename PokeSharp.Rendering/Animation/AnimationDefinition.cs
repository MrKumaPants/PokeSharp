using Microsoft.Xna.Framework;

namespace PokeSharp.Rendering.Animation;

/// <summary>
/// Defines a single animation sequence with frames, durations, and playback settings.
/// </summary>
public class AnimationDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for this animation.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the array of source rectangles for each frame.
    /// Each rectangle defines the sprite sheet region for that frame.
    /// </summary>
    public Rectangle[] Frames { get; set; } = Array.Empty<Rectangle>();

    /// <summary>
    /// Gets or sets the duration of each frame in seconds.
    /// </summary>
    public float FrameDuration { get; set; } = 0.15f; // Default: 6.67 FPS

    /// <summary>
    /// Gets or sets whether the animation loops continuously.
    /// </summary>
    public bool Loop { get; set; } = true;

    /// <summary>
    /// Gets the total number of frames in this animation.
    /// </summary>
    public int FrameCount => Frames.Length;

    /// <summary>
    /// Gets the total duration of the animation in seconds.
    /// </summary>
    public float TotalDuration => FrameCount * FrameDuration;

    /// <summary>
    /// Initializes a new instance of the AnimationDefinition class.
    /// </summary>
    public AnimationDefinition()
    {
    }

    /// <summary>
    /// Initializes a new instance of the AnimationDefinition class with specified parameters.
    /// </summary>
    /// <param name="name">The animation name.</param>
    /// <param name="frames">The frame source rectangles.</param>
    /// <param name="frameDuration">Duration of each frame in seconds.</param>
    /// <param name="loop">Whether the animation loops.</param>
    public AnimationDefinition(string name, Rectangle[] frames, float frameDuration = 0.15f, bool loop = true)
    {
        Name = name;
        Frames = frames;
        FrameDuration = frameDuration;
        Loop = loop;
    }

    /// <summary>
    /// Gets the source rectangle for a specific frame index.
    /// </summary>
    /// <param name="frameIndex">The frame index (0-based).</param>
    /// <returns>The source rectangle for the frame.</returns>
    public Rectangle GetFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= FrameCount)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex),
                $"Frame index {frameIndex} is out of range. Valid range: 0-{FrameCount - 1}");
        }

        return Frames[frameIndex];
    }

    /// <summary>
    /// Creates a simple single-frame animation (for idle poses).
    /// </summary>
    /// <param name="name">The animation name.</param>
    /// <param name="frame">The single frame rectangle.</param>
    /// <returns>A new AnimationDefinition with one frame.</returns>
    public static AnimationDefinition CreateSingleFrame(string name, Rectangle frame)
    {
        return new AnimationDefinition(name, new[] { frame }, frameDuration: 1.0f, loop: true);
    }

    /// <summary>
    /// Creates an animation from a sprite sheet grid layout.
    /// </summary>
    /// <param name="name">The animation name.</param>
    /// <param name="startX">Starting X position on sprite sheet.</param>
    /// <param name="startY">Starting Y position on sprite sheet.</param>
    /// <param name="frameWidth">Width of each frame in pixels.</param>
    /// <param name="frameHeight">Height of each frame in pixels.</param>
    /// <param name="frameCount">Number of frames in the sequence.</param>
    /// <param name="frameDuration">Duration of each frame in seconds.</param>
    /// <param name="loop">Whether the animation loops.</param>
    /// <returns>A new AnimationDefinition with calculated frame rectangles.</returns>
    public static AnimationDefinition CreateFromGrid(
        string name,
        int startX,
        int startY,
        int frameWidth,
        int frameHeight,
        int frameCount,
        float frameDuration = 0.15f,
        bool loop = true)
    {
        var frames = new Rectangle[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            frames[i] = new Rectangle(
                startX + (i * frameWidth),
                startY,
                frameWidth,
                frameHeight
            );
        }

        return new AnimationDefinition(name, frames, frameDuration, loop);
    }
}
