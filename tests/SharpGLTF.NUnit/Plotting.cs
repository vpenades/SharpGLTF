using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using TRACES = Plotly.Box<Plotly.Types.ITracesProperty>;

namespace SharpGLTF
{
    public static class Plotting
    {
        public static Point2Series ToPointSeries(this IEnumerable<Single> points, string name = null) { return Point2Series.Create(points).WithName(name); }

        public static Point2Series ToPointSeries(this IEnumerable<Double> points, string name = null) { return Point2Series.Create(points).WithName(name); }

        public static Point2Series ToPointSeries(this IEnumerable<Vector2> points, string name = null) { return Point2Series.Create(points).WithName(name); }

        public static Point2Series ToLineSeries(this IEnumerable<Vector2> points, string name = null) { return Point2Series.Create(points, LineType.Continuous).WithName(name); }

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

            public Point2Series WithName(string name) { Name = name; return this; }

            #endregion

            #region data

            private readonly List<Point2> _Points = new List<Point2>();            

            #endregion

            #region properties

            public string Name { get; set; }

            public LineType LineType { get; set; }

            #endregion

            #region API

            public Point2Series WithLineType(LineType t) { LineType = t; return this; }            

            public void DrawToFile(string filePath)
            {
                DrawToFile(filePath, this);
            }

            private TRACES GetTrace()
            {
                var x = Plotly.Scatter.x(_Points.Select(item => (float)item.X));
                var y = Plotly.Scatter.y(_Points.Select(item => (float)item.Y));

                var mode = LineType == LineType.Continuous ? Plotly.Scatter.Mode.lines() : Plotly.Scatter.Mode.markers();

                var name = Plotly.Scatter.name(this.Name);

                return Plotly.Traces.scatter(x, y, mode, name);
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
                var traces = series
                    .Select(item => item.GetTrace())
                    .ToArray();

                var plot = Plotly.Plot.traces(traces);

                var chart = new Plotly.Plot(plot);

                var html = chart.Render().ToString();

                System.IO.File.WriteAllText(filePath, html);
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

            private Plotly.Box<Plotly.Types.ITracesProperty> GetTrace()
            {
                var x = Plotly.Scatter3d.x(_Points.Select(item => (float)item.X));
                var y = Plotly.Scatter3d.y(_Points.Select(item => (float)item.Y));
                var z = Plotly.Scatter3d.z(_Points.Select(item => (float)item.Z));
                return Plotly.Traces.scatter3d(x, y, z);
            }

            public void DrawToFile(string filePath)
            {
                DrawToFile(filePath, this);
            }

            public static void DrawToFile(string filePath, params Point3Series[] series)
            {
                var traces = series
                    .Select(item => item.GetTrace())
                    .ToArray();

                var plot = Plotly.Plot.traces(traces);

                var chart = new Plotly.Plot(plot);

                var html = chart.Render().ToString();

                System.IO.File.WriteAllText(filePath, html);
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
