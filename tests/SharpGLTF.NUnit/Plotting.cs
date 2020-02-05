using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF
{
    public static class Plotting
    {
        public static Point2Series ToPointSeries(this IEnumerable<Single> points) { return Point2Series.Create(points); }

        public static Point2Series ToPointSeries(this IEnumerable<Double> points) { return Point2Series.Create(points); }

        public static Point2Series ToPointSeries(this IEnumerable<Vector2> points) { return Point2Series.Create(points); }

        public static Point2Series ToLineSeries(this IEnumerable<Vector2> points) { return Point2Series.Create(points, LineType.Continuous); }

        public enum LineType
        {
            Square = 0,
            Dot = 1,
            Cross = 2,
            Star = 3,
            Circle = 4,
            X = 5,            
            Square2 = 6,
            Triangle = 7,
            CircleWithCross = 8,
            CircleWithDot = 9,

            CHAR_X = 88,
            CHAR_Y = 89,
            CHAR_Z = 90,
            CHAR_W = 87,

            Continuous = 65536                
        }

        public struct Point2
        {
            public Point2(Double x, Double y)
            {
                this.X = x; this.Y = y;
            }

            public Point2(Vector2 v)
            {
                this.X = v.X; this.Y = v.Y;
            }

            public Double X;
            public Double Y;
        }

        public class Point2Series
        {
            #region lifecycle

            public static Point2Series Create(IEnumerable<float> series, LineType lt = LineType.Continuous)
            {
                var points = series
                    .Select((y, x) => (x, y))
                    .Where(item => item.y.IsFinite())
                    .Select(item => new Vector2(item.x, item.y));

                return Create(points, lt);
            }

            public static Point2Series Create(IEnumerable<double> series, LineType lt = LineType.Continuous)
            {
                var points = series
                    .Select((y, x) => (x, (float)y))
                    .Where(item => item.Item2.IsFinite())
                    .Select(item => new Vector2(item.x, item.Item2));

                return Create(points, lt);
            }

            public static Point2Series Create(IEnumerable<Vector2> points, LineType lt = LineType.Dot)
            {
                points = points.Where(item => item.X.IsFinite() && item.Y.IsFinite());

                var ps = new Point2Series();
                ps._Points.AddRange(points.Select(item => new Point2(item)));

                ps.LineType = lt;

                return ps;
            }

            #endregion

            #region data

            private readonly List<Point2> _Points = new List<Point2>();            

            #endregion

            #region properties

            public LineType LineType { get; set; }

            #endregion

            #region API

            public Point2Series WithLineType(LineType t) { LineType = t; return this; }

            public void DrawToFile(string filePath)
            {
                DrawToFile(filePath, this);
            }

            public static (Point2, Point2) GetBounds(params Point2Series[] series)
            {
                var xmin = series.SelectMany(item => item._Points).Min(item => item.X);
                var xmax = series.SelectMany(item => item._Points).Max(item => item.X);
                if (xmin == xmax) { xmin -= 1; xmax += 1; }

                var ymin = series.SelectMany(item => item._Points).Min(item => item.Y);
                var ymax = series.SelectMany(item => item._Points).Max(item => item.Y);
                if (ymin == ymax) { ymin -= 1; ymax += 1; }

                return (new Point2(xmin, ymin), new Point2(xmax, ymax));
            }

            public static void DrawToFile(string filePath, params Point2Series[] series)
            {
                // arguments check
                if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

                var bounds = GetBounds(series);

                try
                {
                    using (var pl = new PLplot.PLStream())
                    {
                        pl.sdev("pngcairo");
                        pl.sfnam(filePath);
                        pl.spal0("cmap0_alternate.pal");

                        pl.init();

                        pl.env(bounds.Item1.X, bounds.Item2.X, bounds.Item1.Y, bounds.Item2.Y, PLplot.AxesScale.Independent, PLplot.AxisBox.BoxTicksLabelsAxes);

                        for (int i = 0; i < series.Length; ++i)
                        {
                            var ps = series[i];
                            var s = ps._Points;

                            var seriesX = new double[s.Count];
                            var seriesY = new double[s.Count];

                            for (int j = 0; j < s.Count; ++j)
                            {
                                seriesX[j] = s[j].X;
                                seriesY[j] = s[j].Y;
                            }

                            pl.col0(i + 2);

                            if (ps.LineType == LineType.Continuous) pl.line(seriesX, seriesY);
                            else pl.poin(seriesX, seriesY, (char)ps.LineType);
                        }

                        pl.eop(); // write to disk
                    }
                }
                catch
                {
                    NUnit.Framework.TestContext.WriteLine("PLPlot not supported.");
                }
            }

            #endregion
        }

        public class Point3Series
        {
            #region lifecycle            

            public static Point3Series Create(IEnumerable<Vector3> points)
            {
                points = points.Where(item => item.X.IsFinite() && item.Y.IsFinite() && item.Z.IsFinite() );

                var ps = new Point3Series();
                ps._Points.AddRange(points);
                return ps;
            }

            #endregion

            #region data

            private readonly List<Vector3> _Points = new List<Vector3>();
            private char _PointGlyph = '+';
            private bool _Lines = false;

            #endregion

            #region API

            public void DrawToFile(string filePath)
            {
                DrawToFile(filePath, this);
            }

            public static (Vector3 Min,  Vector3 Max) GetBounds(params Point3Series[] series)
            {
                var xmin = series.SelectMany(item => item._Points).Min(item => item.X);
                var xmax = series.SelectMany(item => item._Points).Max(item => item.X);
                if (xmin == xmax) { xmin -= 1; xmax += 1; }

                var ymin = series.SelectMany(item => item._Points).Min(item => item.Y);
                var ymax = series.SelectMany(item => item._Points).Max(item => item.Y);
                if (ymin == ymax) { ymin -= 1; ymax += 1; }

                var zmin = series.SelectMany(item => item._Points).Min(item => item.Z);
                var zmax = series.SelectMany(item => item._Points).Max(item => item.Z);
                if (zmin == zmax) { zmin -= 1; zmax += 1; }

                return (new Vector3(xmin, ymin,zmin), new Vector3(xmax, ymax, zmax));
            }

            public static void DrawToFile(string filePath, params Point3Series[] series)
            {
                // arguments check
                if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

                var plen = series
                    .Select(item => item._Points.Count)
                    .Max();

                if (plen < 1) throw new ArgumentOutOfRangeException($"The series only has {plen} values", nameof(series));

                if (series.Any(item => item._Lines))
                {
                    plen = series
                        .Where(item => item._Lines)
                        .Select(item => item._Points.Count)
                        .Max();

                    if (plen < 2) throw new ArgumentOutOfRangeException($"The series only has {plen} values", nameof(series));
                }

                var bounds = GetBounds(series);

                using (var pl = new PLplot.PLStream())
                {
                    pl.sdev("pngcairo");
                    pl.sfnam(filePath);
                    pl.spal0("cmap0_alternate.pal");

                    pl.init();

                    pl.env(bounds.Min.X, bounds.Max.X, bounds.Min.Y, bounds.Max.Y, PLplot.AxesScale.Independent, PLplot.AxisBox.BoxTicksLabelsAxes);

                    for (int i = 0; i < series.Length; ++i)
                    {
                        var ps = series[i];
                        var s = ps._Points;

                        var seriesX = new double[s.Count];
                        var seriesY = new double[s.Count];
                        var seriesZ = new double[s.Count];

                        for (int j = 0; j < s.Count; ++i)
                        {
                            seriesX[j] = s[j].X;
                            seriesY[j] = s[j].Y;
                            seriesZ[j] = s[j].Z;
                        }

                        pl.col0(i + 2);

                        if (ps._Lines) pl.line3(seriesX, seriesY, seriesZ);
                        else pl.poin3(seriesX, seriesY, seriesZ, ps._PointGlyph);
                    }

                    pl.eop(); // write to disk
                }
            }

            #endregion
        }
    }


    public static class PlottingNUnit
    {
        public static void AttachToCurrentTest(this Plotting.Point2Series points, string fileName)
        {
            fileName = NUnit.Framework.TestContext.CurrentContext.GetAttachmentPath(fileName);

            points.DrawToFile(fileName);

            if (System.IO.File.Exists(fileName)) NUnit.Framework.TestContext.AddTestAttachment(fileName);
        }

        public static void AttachToCurrentTest(this IEnumerable<Plotting.Point2Series> series, string fileName)
        {
            fileName = NUnit.Framework.TestContext.CurrentContext.GetAttachmentPath(fileName);

            Plotting.Point2Series.DrawToFile(fileName, series.ToArray());

            if (System.IO.File.Exists(fileName)) NUnit.Framework.TestContext.AddTestAttachment(fileName);
        }
    }
}
