namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Concurrent;

    // BitmapBuilder.DrawText centers its line box, not the visible glyphs, which leaves digits well above center.
    // This measures what the text really draws and moves it, so the visible ink lands on the wanted point.
    internal static class FaceText
    {
        private static readonly ConcurrentDictionary<(String Text, String Font, Int32 Size), (Single X, Single Y)> InkCenters = new();

        // Wide enough that the longest label never wraps.
        private const Int32 BoxEms = 4;

        public static void DrawCentered(BitmapBuilder builder, String text, Single centerX, Single centerY, BitmapColor color, Int32 fontSize, String fontName)
        {
            var ink = InkCenters.GetOrAdd((text, fontName, fontSize), key => MeasureInkCenter(key.Text, key.Font, key.Size));
            var box = fontSize * BoxEms;
            builder.DrawText(text, (Int32)MathF.Round(centerX - ink.X), (Int32)MathF.Round(centerY - ink.Y), box, box, color, fontSize, -1, -1, fontName);
        }

        // Returns where the visible ink's center falls when the text is drawn into a box at (0, 0).
        private static (Single X, Single Y) MeasureInkCenter(String text, String fontName, Int32 fontSize)
        {
            var box = fontSize * BoxEms;
            using (var builder = new BitmapBuilder(box, box))
            {
                builder.Clear(BitmapColor.Black);
                builder.DrawText(text, 0, 0, box, box, BitmapColor.White, fontSize, -1, -1, fontName);

                var pixels = builder.ToArray(BitmapImageFormat.Rgb888);
                Int32 left = Int32.MaxValue, top = Int32.MaxValue, right = -1, bottom = -1;
                for (var y = 0; y < box; y++)
                {
                    for (var x = 0; x < box; x++)
                    {
                        if (pixels[(y * box + x) * 3] >= 128)
                        {
                            left = Math.Min(left, x);
                            top = Math.Min(top, y);
                            right = Math.Max(right, x);
                            bottom = Math.Max(bottom, y);
                        }
                    }
                }

                return right < 0 ? (box / 2f, box / 2f) : ((left + right + 1) / 2f, (top + bottom + 1) / 2f);
            }
        }
    }
}
