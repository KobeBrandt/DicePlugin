namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using static System.FormattableString;

    internal enum FaceState { Idle, Rolling, Result }

    internal enum Mood { Normal, Gold, Slate }

    // Each die is a real convex solid lit facet by facet, so the stills and the tumble are one drawing.
    // Everything scales with the shorter side and fits the inscribed circle, so the round ring and square keys share one layout.
    internal static class DieFace
    {
        // The hue drifts from dark to light so that shaded facets stay jewel-toned instead of turning grey.
        private sealed record Palette(String Dark, String Mid, String Light, String Flash = null, String Top = null, String Bottom = null);

        private sealed record Solid(Vector3[] Points, Int32[][] Faces, Vector3[] Normals, Matrix4x4 Hero, Single Reach, Palette Palette);

        private const Single Phi = 1.618034f;
        private const Int32 CoinSides = 24;
        private const Single MaxFlash = 0.55f;

        private static readonly Single[] Signs = { -1f, 1f };

        // Screen axes: x right, y down, z toward the viewer. The light sits to the upper left, a little in front.
        private static readonly Vector3 Light = Vector3.Normalize(new Vector3(-0.55f, -0.8f, 0.28f));
        private static readonly Vector3 Glint = Vector3.Normalize(Light + Vector3.UnitZ);

        // Matches no symmetry of any solid, and a full turn lands back on the still view, so the result follows the last frame.
        private static readonly Vector3 TumbleAxis = Vector3.Normalize(new Vector3(0.62f, 0.74f, 0.27f));

        private static readonly Palette GoldTones = new("#C98A12", "#FFC041", "#FFE08A");
        private static readonly Palette SlateTones = new("#262A38", "#323645", "#3F4354", null, "#23252E", "#0D0E12");

        private static readonly ConcurrentDictionary<Int32, Solid> Solids = new();
        private static readonly ConcurrentDictionary<(Int32 Sides, Int32 Width, Int32 Height, Mood Mood, Int32 Turn), Byte[]> Art = new();

        public static void Draw(BitmapBuilder builder, Int32 sides, FaceState state, Int32 value, Single rotation)
        {
            var mood = state != FaceState.Result ? Mood.Normal : value == sides ? Mood.Gold : value == 1 ? Mood.Slate : Mood.Normal;
            DrawArt(builder, sides, state, mood, rotation);
            if (state == FaceState.Rolling)
            {
                return;
            }

            var size = Math.Min(builder.Width, builder.Height);
            var text = state == FaceState.Result ? value.ToString(CultureInfo.InvariantCulture) : $"D{sides}";

            // Labels shrink with their length so they all span about the same width; only 100 needs a condensed face.
            var (font, scale) = state == FaceState.Idle
                ? ("Lexend", text.Length > 3 ? 0.27f : text.Length > 2 ? 0.33f : 0.40f)
                : (text.Length > 2 ? "Oswald" : "Lexend", text.Length > 2 ? 0.48f : 0.50f);
            var fontSize = (Int32)MathF.Round(size * scale);
            var (ink, under) = mood switch
            {
                Mood.Gold => (new BitmapColor(0x2B, 0x1A, 0x00), new BitmapColor(255, 244, 205, 150)),
                Mood.Slate => (new BitmapColor(0xFF, 0x7A, 0x68), new BitmapColor(0, 0, 0, 120)),
                _ => (BitmapColor.White, new BitmapColor(0, 0, 0, 120)),
            };
            var (x, y) = (builder.Width / 2f, builder.Height / 2f);
            FaceText.DrawCentered(builder, text, x, y + Math.Max(1f, size * 0.02f), under, fontSize, font);
            FaceText.DrawCentered(builder, text, x, y, ink, fontSize, font);
        }

        private static void DrawArt(BitmapBuilder builder, Int32 sides, FaceState state, Mood mood, Single rotation)
        {
            // A turn repeats every 2 pi, so quantizing it keeps the cache small whatever the host passes.
            var turn = state == FaceState.Rolling ? (Int32)MathF.Round((rotation % MathF.Tau + MathF.Tau) % MathF.Tau * 100f) : -1;
            builder.DrawImage(Art.GetOrAdd((sides, builder.Width, builder.Height, mood, turn), Rasterize), 0, 0, BitmapRotation.None);
        }

        // The service can ask for one action's image from two threads at once, so the cache holds plain PNG bytes that any thread can read.
        private static Byte[] Rasterize((Int32 Sides, Int32 Width, Int32 Height, Mood Mood, Int32 Turn) key)
        {
            using (var vector = BitmapImage.FromSvg(BuildSvg(key.Sides, key.Width, key.Height, key.Mood, key.Turn)))
            using (var raster = new BitmapBuilder(key.Width, key.Height))
            {
                raster.DrawImage(vector, 0, 0, key.Width, key.Height, BitmapRotation.None);
                return raster.ToArray(BitmapImageFormat.Png);
            }
        }

        private static String BuildSvg(Int32 sides, Int32 width, Int32 height, Mood mood, Int32 turn)
        {
            var size = Math.Min(width, height);
            var (cx, cy) = (width / 2f, height / 2f);
            var solid = Solids.GetOrAdd(sides, Build);
            var pose = turn < 0 ? solid.Hero : solid.Hero * Matrix4x4.CreateFromAxisAngle(TumbleAxis, turn / 100f);
            var points = solid.Points.Select(p => Vector3.Transform(p, pose)).ToArray();
            var fit = size * solid.Reach / points.Max(p => MathF.Sqrt(p.X * p.X + p.Y * p.Y));
            var flat = points.Select(p => new Vector2(cx + p.X * fit, cy + p.Y * fit)).ToArray();
            String Path(IEnumerable<Int32> ring) => String.Join(" ", ring.Select(i => Invariant($"{flat[i].X:0.##},{flat[i].Y:0.##}")));

            var tones = mood == Mood.Gold ? GoldTones : mood == Mood.Slate ? SlateTones : solid.Palette;
            var edges = new Dictionary<(Int32, Int32), (Vector3 Sum, Int32 Count)>();
            var facets = new List<(String Path, String Color)>();
            for (var f = 0; f < solid.Faces.Length; f++)
            {
                var normal = Vector3.TransformNormal(solid.Normals[f], pose);
                if (normal.Z > 0.0005f)
                {
                    var ring = solid.Faces[f];
                    var bias = sides != 2 ? 0f : ring.Length == CoinSides ? -0.26f : 0.12f;
                    facets.Add((Path(ring), Tone(tones, Shade(normal, turn >= 0) + bias)));
                    for (var e = 0; e < ring.Length; e++)
                    {
                        var (a, b) = (ring[e], ring[(e + 1) % ring.Length]);
                        var key = a < b ? (a, b) : (b, a);
                        edges[key] = edges.TryGetValue(key, out var seen) ? (seen.Sum + normal, seen.Count + 1) : (normal, 1);
                    }
                }
            }

            var (top, bottom) = mood == Mood.Slate ? (SlateTones.Top, SlateTones.Bottom) : (solid.Palette.Top, solid.Palette.Bottom);
            var glow = mood == Mood.Gold ? (Color: GoldTones.Mid, Opacity: 0.3f) : (Color: tones.Mid, Opacity: 0.4f);
            var svg = new StringBuilder();
            svg.Append(Invariant($"""
                <svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}"><defs>
                <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{top}"/><stop offset="1" stop-color="{bottom}"/></linearGradient>
                <radialGradient id="glow" cx=".5" cy=".46" r=".5"><stop offset="0" stop-color="{glow.Color}" stop-opacity="{glow.Opacity:0.##}"/><stop offset="1" stop-color="{glow.Color}" stop-opacity="0"/></radialGradient>
                <linearGradient id="sheen" gradientUnits="userSpaceOnUse" x1="{cx - size * 0.3f:0.##}" y1="{cy - size * 0.42f:0.##}" x2="{cx + size * 0.3f:0.##}" y2="{cy + size * 0.42f:0.##}"><stop offset="0" stop-color="#fff" stop-opacity=".1"/><stop offset=".5" stop-color="#fff" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity=".14"/></linearGradient>
                <filter id="soft" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="{size * 0.022f:0.##}"/></filter>
                <radialGradient id="pool"><stop offset="0" stop-color="{tones.Light}" stop-opacity=".4"/><stop offset="1" stop-color="{tones.Light}" stop-opacity="0"/></radialGradient>
                <linearGradient id="wall" x1="0" y1="0" x2=".6" y2="1"><stop offset="0" stop-color="#000" stop-opacity=".5"/><stop offset="1" stop-color="#fff" stop-opacity=".5"/></linearGradient>
                <radialGradient id="halo"><stop offset="0" stop-color="#fff" stop-opacity=".55"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>
                <radialGradient id="ember"><stop offset="0" stop-color="#FF7A68" stop-opacity=".2"/><stop offset="1" stop-color="#FF7A68" stop-opacity="0"/></radialGradient>
                <clipPath id="gem">{String.Concat(facets.Select(x => $"<polygon points=\"{x.Path}\"/>"))}</clipPath></defs>
                <rect width="{width}" height="{height}" fill="url(#bg)"/><rect width="{width}" height="{height}" fill="url(#glow)"/>
                """));

            // The shadow lifts the gem off the dark ground, so its unlit facets keep a readable outline.
            svg.Append(Invariant($"""<g filter="url(#soft)" transform="translate(0 {size * 0.03f:0.##})" opacity="{(mood == Mood.Gold ? 0.4f : 0.75f):0.##}" fill="#000">"""));
            svg.AppendJoin("", facets.Select(x => $"<polygon points=\"{x.Path}\"/>")).Append("</g>");

            // A same-coloured stroke closes the antialiasing seams between neighbouring facets.
            svg.AppendJoin("", facets.Select(x => Invariant($"""<polygon points="{x.Path}" fill="{x.Color}" stroke="{x.Color}" stroke-width="{size * 0.012f:0.##}" stroke-linejoin="round"/>""")));
            svg.AppendJoin("", facets.Select(x => $"<polygon points=\"{x.Path}\" fill=\"url(#sheen)\"/>"));

            // Light entering a real gem pools on the side away from the source, which is what stops it reading as painted plastic.
            if (mood != Mood.Slate)
            {
                svg.Append(Invariant($"""<circle cx="{cx + size * 0.15f:0.##}" cy="{cy + size * 0.20f:0.##}" r="{size * 0.36f:0.##}" fill="url(#pool)" clip-path="url(#gem)"/>"""));
            }

            // Convex edges catch the light. Inner edges shorter than a few pixels would only add noise, so they stay unlit.
            var fine = size >= 64;
            var hair = size * (fine ? 0.010f : 0.014f);
            var strength = (fine ? 1f : 0.7f) * (mood == Mood.Slate ? 0.55f : 1f);
            foreach (var ((a, b), (sum, count)) in edges)
            {
                var lit = Vector3.Dot(Vector3.Normalize(sum), Light);
                var alpha = strength * (count > 1 ? Math.Clamp(0.05f + 0.34f * lit, 0f, 0.38f) : Math.Clamp(0.15f + 0.60f * lit, 0f, 0.85f));
                var longEnough = sides != 2 && Vector2.Distance(flat[a], flat[b]) >= 9f;
                if (alpha > 0.02f && (count == 1 || longEnough))
                {
                    svg.Append(Invariant($"""<line x1="{flat[a].X:0.##}" y1="{flat[a].Y:0.##}" x2="{flat[b].X:0.##}" y2="{flat[b].Y:0.##}" stroke="#fff" stroke-opacity="{alpha:0.##}" stroke-width="{hair:0.##}" stroke-linecap="round"/>"""));
                }
            }

            if (sides == 2)
            {
                // Show the field of whichever coin side faces the viewer: half of it takes more light, and the rim's inner wall is dark toward the light and bright away from it.
                var first = Vector3.TransformNormal(Vector3.UnitZ, pose).Z >= 0 ? 0 : CoinSides * 3;
                var half = Enumerable.Range(0, CoinSides / 2 + 1).Select(i => first + (i + CoinSides * 3 / 32) % CoinSides);
                svg.Append(Invariant($"""
                    <polygon points="{Path(half)}" fill="{tones.Light}" fill-opacity=".16"/>
                    <polygon points="{Path(Enumerable.Range(first, CoinSides))}" fill="none" stroke="url(#wall)" stroke-width="{size * 0.018f:0.##}"/>
                    """));
            }

            if (mood == Mood.Gold)
            {
                Star(svg, cx + size * 0.21f, cy - size * 0.33f, size * 0.085f);
                if (fine)
                {
                    Star(svg, cx - size * 0.33f, cy + size * 0.27f, size * 0.045f);
                }
            }
            else if (mood == Mood.Slate)
            {
                svg.Append(Invariant($"""<circle cx="{cx:0.##}" cy="{cy:0.##}" r="{size * 0.3f:0.##}" fill="url(#ember)"/>"""));
                if (fine)
                {
                    Cracks(svg, cx, cy, size);
                }
            }

            return svg.Append("</svg>").ToString();
        }

        private static void Star(StringBuilder svg, Single x, Single y, Single r)
        {
            var (w, n, e, s) = (x - r * 0.8f, y - r, x + r * 0.8f, y + r);
            svg.Append(Invariant($"""
                <circle cx="{x:0.##}" cy="{y:0.##}" r="{r * 1.1f:0.##}" fill="url(#halo)"/>
                <path d="M{x:0.##} {n:0.##}Q{x:0.##} {y:0.##} {e:0.##} {y:0.##}Q{x:0.##} {y:0.##} {x:0.##} {s:0.##}Q{x:0.##} {y:0.##} {w:0.##} {y:0.##}Q{x:0.##} {y:0.##} {x:0.##} {n:0.##}Z" fill="#fff"/>
                """));
        }

        // Fractures that radiate from an impact hidden behind the numeral, with a thread of ember light inside.
        private static void Cracks(StringBuilder svg, Single cx, Single cy, Int32 size)
        {
            var cracks = new[]
            {
                new[] { (0.02f, -0.02f), (0.11f, -0.09f), (0.09f, -0.17f), (0.20f, -0.24f), (0.21f, -0.33f), (0.31f, -0.44f) },
                new[] { (-0.02f, 0.03f), (-0.07f, 0.14f), (-0.15f, 0.17f), (-0.17f, 0.29f), (-0.28f, 0.38f) },
                new[] { (0.05f, 0.02f), (0.17f, 0.05f), (0.22f, 0.14f), (0.34f, 0.15f), (0.44f, 0.24f) },
            };
            svg.Append("""<g clip-path="url(#gem)" fill="none" stroke-linejoin="round" stroke-linecap="round">""");
            foreach (var crack in cracks)
            {
                var path = String.Join(" ", crack.Select(p => Invariant($"{cx + p.Item1 * size:0.##},{cy + p.Item2 * size:0.##}")));
                svg.Append(Invariant($"""<polyline points="{path}" stroke="#0E1016" stroke-width="{size * 0.028f:0.##}"/><polyline points="{path}" stroke="#FF7A68" stroke-opacity=".85" stroke-width="{size * 0.009f:0.##}"/>"""));
            }

            svg.Append("</g>");
        }

        // While tumbling, the facet that mirrors the light flashes past its lit tone.
        private static Single Shade(Vector3 normal, Boolean tumbling)
        {
            var shade = Math.Clamp((Vector3.Dot(normal, Light) + 0.55f) / 1.55f, 0f, 1f);
            return tumbling ? shade + MaxFlash * MathF.Pow(Math.Max(0f, Vector3.Dot(normal, Glint)), 18f) : shade;
        }

        private static String Tone(Palette tones, Single shade)
        {
            var (from, to, t) = shade > 1f
                ? (tones.Light, tones.Flash, (shade - 1f) / MaxFlash)
                : shade > 0.5f ? (tones.Mid, tones.Light, (shade - 0.5f) * 2f) : (tones.Dark, tones.Mid, Math.Max(0f, shade) * 2f);
            var (a, b) = (Convert.ToInt32(from.Substring(1), 16), Convert.ToInt32(to.Substring(1), 16));
            Int32 Channel(Int32 bit) => (Int32)MathF.Round((a >> bit & 255) * (1f - t) + (b >> bit & 255) * Math.Min(t, 1f));
            return $"#{Channel(16):X2}{Channel(8):X2}{Channel(0):X2}";
        }

        private static Solid Build(Int32 sides)
        {
            var points = sides switch
            {
                2 => Rings((CoinSides, 0.80f, 0.20f, 0f), (CoinSides, 1f, 0.10f, 0f), (CoinSides, 1f, -0.10f, 0f), (CoinSides, 0.80f, -0.20f, 0f)),
                4 => BluntedTetrahedron(0.16f),
                6 => (from x in Signs from y in Signs from z in Signs select new Vector3(x, y, z)).ToArray(),
                8 => new[] { Vector3.UnitX * 0.88f, -Vector3.UnitX * 0.88f, Vector3.UnitY * 1.08f, -Vector3.UnitY * 1.08f, Vector3.UnitZ, -Vector3.UnitZ },
                10 => Trapezohedron(0.125f),
                12 => Rings((5, 0.62f, 0.55f, 0f), (5, 1f, 0f, 0f), (1, 0f, -0.8f, 0f)),
                20 => (from s in Signs from t in Signs from p in new[] { new Vector3(0, s, t * Phi), new Vector3(s, t * Phi, 0), new Vector3(s * Phi, 0, t) } select p).ToArray(),
                _ => Rings((10, 0.64f, 0.42f, 0f), (10, 1f, 0f, 0.5f), (1, 0f, -0.8f, 0f)),
            };

            var length = points.Max(p => p.Length());
            points = points.Select(p => p / length).ToArray();
            var (faces, normals) = Hull(points);
            var table = Array.FindIndex(normals, n => n.Z > 0.5f);

            Solid Pose(Vector3 front, Vector3 up, Single reach, Palette palette) => new Solid(points, faces, normals, Facing(front, up), reach, palette);

            // Round shapes look bigger than pointed ones of the same reach, so they get less of it.
            return sides switch
            {
                2 => Pose(Vector3.UnitZ, Vector3.UnitY, 0.43f, new("#004F3A", "#027E47", "#00B046", "#99E7A4", "#003B1E", "#000E02")),
                4 => Pose(Vector3.One, new Vector3(1, -1, -1), 0.47f, new("#004D63", "#077B7C", "#07A598", "#64EBDB", "#023838", "#000D0D")),
                6 => Pose(new Vector3(1, 1.1f, 1), Vector3.UnitY, 0.46f, new("#113988", "#006E9F", "#009BC9", "#92DDFE", "#00344F", "#000B19")),
                8 => Pose(new Vector3(0.1f, 0.12f, 1f), Vector3.UnitY, 0.465f, new("#3A2A82", "#3755CB", "#2C8EFE", "#B3D4FE", "#1B2B62", "#03071D")),
                10 => Pose(new Vector3(MathF.Cos(MathF.PI / 5) * 0.92f, MathF.Sin(MathF.PI / 5) * 0.92f, 0.40f), Vector3.UnitZ, 0.46f, new("#2C2F84", "#6E43BF", "#AD60FE", "#DCC4FF", "#35225C", "#0B041B")),
                12 => Pose(Vector3.UnitZ, new Vector3(MathF.Cos(MathF.PI / 5), MathF.Sin(MathF.PI / 5), 0), 0.46f, new("#4A237A", "#94309B", "#DB47CA", "#FEB5F1", "#481A4B", "#130214")),
                20 => Pose(normals[table], points[faces[table][0]], 0.46f, new("#5E1A63", "#AA2568", "#F33D82", "#FEBCCC", "#541433", "#17010A")),
                _ => Pose(Vector3.UnitZ, Vector3.UnitY, 0.44f, new("#6E0F40", "#BE1E30", "#F84A33", "#FFC0B0", "#5E1016", "#1B0102")),
            };
        }

        private static Vector3[] Rings(params (Int32 Count, Single Radius, Single Z, Single Twist)[] rings) =>
            (from ring in rings
             from i in Enumerable.Range(0, ring.Count)
             let angle = MathF.Tau * (i + ring.Twist + 0.5f) / ring.Count
             select new Vector3(ring.Radius * MathF.Cos(angle), ring.Radius * MathF.Sin(angle), ring.Z)).ToArray();

        // The three corners that make the triangle's outline are blunted, so the body can grow inside the round slot.
        private static Vector3[] BluntedTetrahedron(Single cut)
        {
            var corners = new[] { new Vector3(1, 1, 1), new Vector3(1, -1, -1), new Vector3(-1, 1, -1), new Vector3(-1, -1, 1) };
            var blunted = from c in corners.Skip(1) from o in corners where o != c select Vector3.Lerp(c, o, cut);
            return blunted.Prepend(corners[0]).ToArray();
        }

        // A real d10: two apexes over a zigzag belt, with the apex height that keeps each kite flat.
        private static Vector3[] Trapezohedron(Single belt)
        {
            var c = MathF.Cos(MathF.PI / 5);
            var apex = belt * (1 + c) / (1 - c);
            var zigzag = Enumerable.Range(0, 10).Select(i => new Vector3(MathF.Cos(MathF.PI * i / 5), MathF.Sin(MathF.PI * i / 5), i % 2 == 0 ? belt : -belt));
            return zigzag.Append(Vector3.UnitZ * apex).Append(-Vector3.UnitZ * apex).ToArray();
        }

        // Finds the faces of a convex solid centered on the origin. Brute force is fine: few points, cached per die.
        private static (Int32[][] Faces, Vector3[] Normals) Hull(Vector3[] points)
        {
            const Single Tolerance = 0.002f;
            var faces = new List<Int32[]>();
            var normals = new List<Vector3>();
            for (var i = 0; i < points.Length; i++)
            {
                for (var j = i + 1; j < points.Length; j++)
                {
                    for (var k = j + 1; k < points.Length; k++)
                    {
                        var cross = Vector3.Cross(points[j] - points[i], points[k] - points[i]);
                        if (cross.LengthSquared() < 1e-6f)
                        {
                            continue;
                        }

                        var normal = Vector3.Normalize(cross);
                        var distance = Vector3.Dot(normal, points[i]);
                        (normal, distance) = distance < 0 ? (-normal, -distance) : (normal, distance);
                        if (points.Any(p => Vector3.Dot(normal, p) > distance + Tolerance) || normals.Any(n => (n - normal).LengthSquared() < 1e-4f))
                        {
                            continue;
                        }

                        var center = normal * distance;
                        var axis = Vector3.Normalize(points[i] - center);
                        var side = Vector3.Cross(normal, axis);
                        faces.Add(Enumerable.Range(0, points.Length)
                            .Where(p => MathF.Abs(Vector3.Dot(normal, points[p]) - distance) < Tolerance)
                            .OrderBy(p => MathF.Atan2(Vector3.Dot(points[p] - center, side), Vector3.Dot(points[p] - center, axis)))
                            .ToArray());
                        normals.Add(normal);
                    }
                }
            }

            return (faces.ToArray(), normals.ToArray());
        }

        // Rotation that turns a solid so `front` faces the viewer and `up` points to the top of the screen, where y grows downward.
        private static Matrix4x4 Facing(Vector3 front, Vector3 up)
        {
            var z = Vector3.Normalize(front);
            var x = Vector3.Normalize(Vector3.Cross(up, z));
            var y = Vector3.Cross(z, x);
            return new Matrix4x4(x.X, -y.X, z.X, 0, x.Y, -y.Y, z.Y, 0, x.Z, -y.Z, z.Z, 0, 0, 0, 0, 1);
        }
    }
}
