using System.Windows;
using System.Windows.Media.Animation;

namespace SoleSurvivor.App;

/// <summary>Small awaitable wrappers over WPF animations.</summary>
internal static class Anim
{
    /// <summary>Multiplier on every duration. 1 = normal, lower = faster.</summary>
    public static double Speed { get; set; } = 1.0;

    private static Duration D(double ms) => new(TimeSpan.FromMilliseconds(Math.Max(1, ms * Speed)));

    public static readonly IEasingFunction EaseOut = Freeze(new CubicEase { EasingMode = EasingMode.EaseOut });
    public static readonly IEasingFunction EaseInOut = Freeze(new SineEase { EasingMode = EasingMode.EaseInOut });
    public static readonly IEasingFunction Back = Freeze(new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 });

    private static IEasingFunction Freeze(EasingFunctionBase ease)
    {
        ease.Freeze();
        return ease;
    }

    /// <summary>Animate a double property and complete when done. The final value is held.</summary>
    public static Task To(IAnimatable target, DependencyProperty property, double to, double ms, IEasingFunction? ease = null, double? from = null)
    {
        var tcs = new TaskCompletionSource();
        var animation = new DoubleAnimation { To = to, Duration = D(ms), EasingFunction = ease };
        if (from.HasValue) animation.From = from.Value;
        animation.Completed += (_, _) => tcs.TrySetResult();
        target.BeginAnimation(property, animation);
        return tcs.Task;
    }

    /// <summary>Repeat forever between two values until <see cref="Stop"/> is called.</summary>
    public static void Loop(IAnimatable target, DependencyProperty property, double from, double to, double ms, bool autoReverse = true)
    {
        target.BeginAnimation(property, new DoubleAnimation(from, to, D(ms))
        {
            AutoReverse = autoReverse,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = EaseInOut
        });
    }

    /// <summary>Stop any animation on the property and set its value directly.</summary>
    public static void Set(IAnimatable target, DependencyProperty property, double value)
    {
        target.BeginAnimation(property, null);
        ((DependencyObject)target).SetValue(property, value);
    }

    public static Task Wait(double ms) => Task.Delay(TimeSpan.FromMilliseconds(Math.Max(1, ms * Speed)));
}
