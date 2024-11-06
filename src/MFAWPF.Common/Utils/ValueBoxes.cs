using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace MFAWPF.Common.Utils;

internal static class ValueBoxes
{
    internal static object TrueBox = true;

    internal static object FalseBox = false;

    internal static object VerticalBox = Orientation.Vertical;

    internal static object HorizontalBox = Orientation.Horizontal;

    internal static object VisibleBox = Avalonia.Controls.Primitives.LayoutTransformControl.IsVisibleProperty.Default;

    internal static object CollapsedBox = Avalonia.Controls.Primitives.LayoutTransformControl.IsVisibleProperty.Default;

    internal static object HiddenBox = Avalonia.Controls.Primitives.LayoutTransformControl.IsVisibleProperty.Default;

    internal static object Double01Box = .1;

    internal static object Double0Box = .0;

    internal static object Double1Box = 1.0;

    internal static object Double10Box = 10.0;

    internal static object Double20Box = 20.0;

    internal static object Double100Box = 100.0;

    internal static object Double200Box = 200.0;

    internal static object Double300Box = 300.0;

    internal static object DoubleNeg1Box = -1.0;

    internal static object Int0Box = 0;

    internal static object Int1Box = 1;

    internal static object Int2Box = 2;

    internal static object Int5Box = 5;

    internal static object Int99Box = 99;

    internal static object BooleanBox(bool value) => value ? TrueBox : FalseBox;

    internal static object OrientationBox(Orientation value) =>
        value == Orientation.Horizontal ? HorizontalBox : VerticalBox;

    internal static object VisibilityBox(bool isVisible) => isVisible ? VisibleBox : CollapsedBox;
}