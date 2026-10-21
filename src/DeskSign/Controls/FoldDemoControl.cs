using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using DeskSign.Core;

namespace DeskSign.Controls;

public sealed class FoldDemoControl : UserControl
{
    public static readonly DependencyProperty NameTextProperty = Register(nameof(NameText), "");
    public static readonly DependencyProperty FontNameProperty = Register(nameof(FontName), "宋体");
    public static readonly DependencyProperty FontSizePtProperty = Register(nameof(FontSizePt), SignLayout.DefaultFontSizePt);
    public static readonly DependencyProperty SpacingCmProperty = Register(nameof(SpacingCm), SignLayout.DefaultSpacingCm);
    public static readonly DependencyProperty GuideOffsetCmProperty = Register(nameof(GuideOffsetCm), SignLayout.GuideGapCm);
    public static readonly DependencyProperty IsPlaceholderProperty = Register(nameof(IsPlaceholder), false);
    public static readonly DependencyProperty FoldAmountProperty = DependencyProperty.Register(
        nameof(FoldAmount),
        typeof(double),
        typeof(FoldDemoControl),
        new FrameworkPropertyMetadata(0.0, (d, _) => ((FoldDemoControl)d).UpdateScene()));

    private static readonly Color PaperColor = Color.FromRgb(0xFB, 0xDA, 0xEF);
    private const double PageWidth = 2.1;
    private const double FaceLength = 1.485;
    private const double HolderTilt = 28 * Math.PI / 180;
    private const double Ground = 0.03;

    private readonly Viewport3D _viewport = new();
    private readonly PerspectiveCamera _camera = new() { FieldOfView = 30, NearPlaneDistance = 0.05, FarPlaneDistance = 40 };
    private readonly GeometryModel3D _front = new();
    private readonly GeometryModel3D _frontFlap = new();
    private readonly GeometryModel3D _back = new();
    private readonly GeometryModel3D _backFlap = new();
    private double _guideFraction = 0.68;
    private double _seamFraction = 0.66;
    private double HolderHalf => PageWidth / 2 * 1.02;
    private double HolderFace => FaceLength * _seamFraction * 1.02;
    private readonly GeometryModel3D _glassFront = new();
    private readonly GeometryModel3D _glassBack = new();
    private readonly Model3DGroup _holder = new();
    private readonly Material _blankPaper;
    private Material? _frontOpen;
    private Material? _frontSeated;
    private Material? _backOpen;
    private Material? _backSeated;
    private Button? _replay;
    private readonly TextBlock _caption = new()
    {
        TextWrapping = TextWrapping.Wrap,
        TextAlignment = TextAlignment.Center,
        Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x34)),
        FontSize = 13,
        Margin = new Thickness(16, 0, 16, 8)
    };
    private double _yaw = 0.72;
    private Point _dragStart;
    private bool _dragging;

    public FoldDemoControl()
    {
        _blankPaper = PinkPaper(Freeze(new SolidColorBrush(PaperColor)));
        _front.BackMaterial = _blankPaper;
        _frontFlap.Material = _blankPaper;
        _frontFlap.BackMaterial = _blankPaper;
        _backFlap.Material = _blankPaper;
        _backFlap.BackMaterial = _blankPaper;
        var glass = Glass(18);
        _glassFront.Material = glass;
        _glassFront.BackMaterial = glass;
        _glassBack.Material = glass;
        _glassBack.BackMaterial = glass;
        BuildHolder();

        var tableBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xD5, 0xCB, 0xBC)));
        var table = new GeometryModel3D
        {
            Geometry = Quad(
                new Point3D(-3.2, -0.012, -2.4),
                new Point3D(3.2, -0.012, -2.4),
                new Point3D(3.2, -0.012, 2.4),
                new Point3D(-3.2, -0.012, 2.4),
                new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(0, 1)),
            Material = new DiffuseMaterial(tableBrush)
        };

        var world = new Model3DGroup();
        world.Children.Add(new AmbientLight(Color.FromRgb(0xA8, 0xA3, 0x9C)));
        world.Children.Add(new DirectionalLight(Color.FromRgb(0x5C, 0x58, 0x52), new Vector3D(-0.35, -0.8, -0.55)));
        world.Children.Add(new DirectionalLight(Color.FromRgb(0x3A, 0x36, 0x30), new Vector3D(0.45, -0.35, 0.7)));
        world.Children.Add(table);
        world.Children.Add(_front);
        world.Children.Add(_back);
        world.Children.Add(_frontFlap);
        world.Children.Add(_backFlap);
        world.Children.Add(_glassBack);
        world.Children.Add(_glassFront);
        world.Children.Add(_holder);

        _viewport.Camera = _camera;
        _viewport.Children.Add(new ModelVisual3D { Content = world });
        Cursor = Cursors.Hand;
        PreviewMouseLeftButtonDown += Viewport_MouseDown;
        PreviewMouseMove += Viewport_MouseMove;
        PreviewMouseLeftButtonUp += Viewport_MouseUp;

        _replay = new Button
        {
            Content = "再看一次",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 14),
            Padding = new Thickness(14, 6, 14, 6),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xD7, 0xCA)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x34)),
            Cursor = Cursors.Hand
        };
        _replay.Click += (_, _) => Play();

        var overlay = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = true };
        overlay.Children.Add(_caption);
        overlay.Children.Add(_replay);

        var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xE3, 0xD9, 0xC8)) };
        root.Children.Add(_viewport);
        root.Children.Add(overlay);
        Content = root;

        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
            {
                RebuildTexture();
                Play();
            }
        };
    }

    public string NameText
    {
        get => (string)GetValue(NameTextProperty);
        set => SetValue(NameTextProperty, value);
    }

    public string FontName
    {
        get => (string)GetValue(FontNameProperty);
        set => SetValue(FontNameProperty, value);
    }

    public double FontSizePt
    {
        get => (double)GetValue(FontSizePtProperty);
        set => SetValue(FontSizePtProperty, value);
    }

    public double SpacingCm
    {
        get => (double)GetValue(SpacingCmProperty);
        set => SetValue(SpacingCmProperty, value);
    }

    public double GuideOffsetCm
    {
        get => (double)GetValue(GuideOffsetCmProperty);
        set => SetValue(GuideOffsetCmProperty, value);
    }

    public bool IsPlaceholder
    {
        get => (bool)GetValue(IsPlaceholderProperty);
        set => SetValue(IsPlaceholderProperty, value);
    }

    public double FoldAmount
    {
        get => (double)GetValue(FoldAmountProperty);
        set => SetValue(FoldAmountProperty, value);
    }

    public void Play()
    {
        BeginAnimation(FoldAmountProperty, null);
        FoldAmount = 0;
        var animation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(3.4))
        {
            BeginTime = TimeSpan.FromMilliseconds(280),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        BeginAnimation(FoldAmountProperty, animation);
    }

    private void RebuildTexture()
    {
        const int width = 720;
        var height = (int)Math.Round(width * SignLayout.PageHeightPt / SignLayout.PageWidthPt);
        var visual = new DrawingVisual();
        var layout = SignLayout.Create(FontSizePt, SpacingCm, GuideOffsetCm);
        using (var context = visual.RenderOpen())
        {
            context.PushTransform(new ScaleTransform(width / SignLayout.PageWidthPt, height / SignLayout.PageHeightPt));
            var paper = Freeze(new SolidColorBrush(PaperColor));
            PageArt.Draw(context, layout, NameText, FontName, showFoldLine: true, IsPlaceholder, 1, 1.4, emphasizeFold: true, pageBrush: paper);
            var below = layout.BottomGuideYPt + 2;
            context.DrawRectangle(paper, null, new Rect(0, below, SignLayout.PageWidthPt, Math.Max(0, SignLayout.PageHeightPt - below)));
            context.DrawRectangle(paper, null, new Rect(0, 0, SignLayout.PageWidthPt, Math.Max(0, layout.TopGuideYPt - 2)));
            var guidePen = new Pen(Freeze(new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80))), 1.4)
            {
                DashStyle = DashStyles.Dash,
                StartLineCap = PenLineCap.Flat,
                EndLineCap = PenLineCap.Flat
            };
            guidePen.Freeze();
            context.DrawLine(guidePen, new Point(layout.GuideLeftPt, layout.TopGuideYPt), new Point(layout.GuideLeftPt + layout.GuideWidthPt, layout.TopGuideYPt));
            context.DrawLine(guidePen, new Point(layout.GuideLeftPt, layout.BottomGuideYPt), new Point(layout.GuideLeftPt + layout.GuideWidthPt, layout.BottomGuideYPt));
            context.Pop();
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        var mid = SignLayout.PageHeightPt / 2;
        _guideFraction = Math.Clamp((layout.BottomGuideYPt - mid) / mid, 0.4, 0.94);
        _seamFraction = Math.Clamp((layout.BottomGuideYPt - 14 - mid) / mid, 0.35, _guideFraction - 0.01);
        int Row(double pt) => (int)Math.Clamp(Math.Round(pt / SignLayout.PageHeightPt * height), 0, height);
        var midRow = Row(SignLayout.PageHeightPt / 2);
        var openBottom = Math.Clamp(Row(layout.BottomGuideYPt + 2), midRow + 1, height);
        var seatedBottom = Math.Clamp(Row(layout.BottomGuideYPt - 14), midRow + 1, openBottom - 1);
        var openTop = Math.Clamp(Row(layout.TopGuideYPt - 2), 0, midRow - 1);
        var seatedTop = Math.Clamp(Row(layout.TopGuideYPt + 14), openTop + 1, midRow - 1);
        _frontOpen = Paper(Slice(bitmap, 0, midRow, width, openBottom - midRow));
        _frontSeated = Paper(Slice(bitmap, 0, midRow, width, seatedBottom - midRow));
        _backOpen = Paper(Slice(bitmap, 0, openTop, width, midRow - openTop));
        _backSeated = Paper(Slice(bitmap, 0, seatedTop, width, midRow - seatedTop));
        _frontFlap.Material = _blankPaper;
        _backFlap.Material = _blankPaper;
        BuildHolder();
        UpdateScene();
    }

    private static CroppedBitmap Slice(BitmapSource bitmap, int x, int y, int w, int h)
    {
        w = Math.Max(1, Math.Min(w, bitmap.PixelWidth - x));
        h = Math.Max(1, Math.Min(h, bitmap.PixelHeight - y));
        return new CroppedBitmap(bitmap, new Int32Rect(x, y, w, h));
    }

    private static Material Paper(BitmapSource source)
    {
        var brush = new ImageBrush(source) { Stretch = Stretch.Fill };
        brush.Freeze();
        return PinkPaper(brush);
    }

    private static Material PinkPaper(Brush brush)
    {
        var material = new DiffuseMaterial(brush);
        material.Freeze();
        return material;
    }

    private void UpdateScene()
    {
        var fold = Math.Clamp(FoldAmount, 0, 1);
        var tentT = Math.Clamp(fold / 0.32, 0, 1);
        var flapT = Smooth(Math.Clamp((fold - 0.32) / 0.34, 0, 1));
        var insertT = Smooth(Math.Clamp((fold - 0.66) / 0.34, 0, 1));
        var tilt = ((1 - tentT) * 78 + tentT * 28) * Math.PI / 180;
        var scale = (1 - insertT) + insertT * 0.995;
        var guide = Math.Clamp(_guideFraction, 0.4, 0.94);
        var seam = Math.Clamp(_seamFraction, 0.35, guide - 0.01);
        var showFlap = flapT < 0.97;
        var face = showFlap ? guide : seam;
        var mainLen = FaceLength * scale * face;
        var flapLen = FaceLength * scale * (1 - guide);
        var mainRise = mainLen * Math.Cos(tilt);
        var mainReach = mainLen * Math.Sin(tilt);
        var flapDrop = flapLen * Math.Cos(tilt);
        var flapReach = flapLen * Math.Sin(tilt);
        var half = PageWidth / 2 * scale;
        var gap = (1 - insertT) * (HolderFace * Math.Sin(HolderTilt) + mainReach + 0.3);
        var hingeL = new Point3D(-half, Ground + mainRise, gap);
        var hingeR = new Point3D(half, Ground + mainRise, gap);
        var frontGuideL = new Point3D(-half, Ground, gap + mainReach);
        var frontGuideR = new Point3D(half, Ground, gap + mainReach);
        var backGuideL = new Point3D(-half, Ground, gap - mainReach);
        var backGuideR = new Point3D(half, Ground, gap - mainReach);
        var frontBaseL = new Point3D(-half, Ground - flapDrop, gap + mainReach + flapReach);
        var frontBaseR = new Point3D(half, Ground - flapDrop, gap + mainReach + flapReach);
        var backBaseL = new Point3D(-half, Ground - flapDrop, gap - mainReach - flapReach);
        var backBaseR = new Point3D(half, Ground - flapDrop, gap - mainReach - flapReach);
        var flapAngle = flapT * 176 * Math.PI / 180;
        var frontTipL = RotateX(frontBaseL, frontGuideL.Y, frontGuideL.Z, flapAngle);
        var frontTipR = RotateX(frontBaseR, frontGuideR.Y, frontGuideR.Z, flapAngle);
        var backTipL = RotateX(backBaseL, backGuideL.Y, backGuideL.Z, -flapAngle);
        var backTipR = RotateX(backBaseR, backGuideR.Y, backGuideR.Z, -flapAngle);

        var full = new[] { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(0, 1) };
        _front.Geometry = Quad(hingeL, hingeR, frontGuideR, frontGuideL, full[0], full[1], full[2], full[3]);
        _frontFlap.Geometry = showFlap
            ? Quad(frontGuideL, frontGuideR, frontTipR, frontTipL, full[0], full[1], full[2], full[3])
            : Quad(frontGuideL, frontGuideR, frontGuideR, frontGuideL, full[0], full[0], full[0], full[0]);
        _back.Geometry = Quad(hingeR, hingeL, backGuideL, backGuideR, new Point(1, 1), new Point(0, 1), new Point(0, 0), new Point(1, 0));
        _backFlap.Geometry = showFlap
            ? Quad(backGuideR, backGuideL, backTipL, backTipR, full[0], full[1], full[2], full[3])
            : Quad(backGuideR, backGuideL, backGuideL, backGuideR, full[0], full[0], full[0], full[0]);
        if (showFlap)
        {
            if (_frontOpen != null)
                _front.Material = _frontOpen;
            if (_backOpen != null)
            {
                _back.Material = _backOpen;
                _back.BackMaterial = _backOpen;
            }
        }
        else
        {
            if (_frontSeated != null)
                _front.Material = _frontSeated;
            if (_backSeated != null)
            {
                _back.Material = _backSeated;
                _back.BackMaterial = _backSeated;
            }
        }

        var holderRise = HolderFace * Math.Cos(HolderTilt);
        var pitch = (1 - fold) * 0.58 + fold * 0.28;
        var distance = (1 - fold) * 9.4 + fold * 5.35;
        var look = new Point3D(0, Ground + holderRise * (0.34 + 0.12 * (1 - insertT)), gap * 0.25);
        var eye = new Point3D(
            look.X + Math.Sin(_yaw) * Math.Cos(pitch) * distance,
            look.Y + Math.Sin(pitch) * distance,
            look.Z + Math.Cos(_yaw) * Math.Cos(pitch) * distance);
        _camera.Position = eye;
        _camera.LookDirection = look - eye;
        _camera.UpDirection = new Vector3D(0, 1, 0);

        _caption.Text = insertT > 0.92
            ? "放好了。正面和背面都能看到姓名，按住拖动可转到另一面。"
            : insertT > 0.02
                ? "沿灰色虚线折好后，放进透明夹子。"
                : flapT > 0.02
                    ? "A4 比夹子大，沿灰色虚线把多余的纸折到后面。"
                    : "姓名外侧的灰色虚线，就是要折起来的位置。";
    }

    private static Point3D Lerp(Point3D a, Point3D b, double t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);

    private static Point3D RotateX(Point3D point, double pivotY, double pivotZ, double radians)
    {
        var y = point.Y - pivotY;
        var z = point.Z - pivotZ;
        var c = Math.Cos(radians);
        var s = Math.Sin(radians);
        return new Point3D(point.X, pivotY + y * c - z * s, pivotZ + y * s + z * c);
    }

    private void BuildHolder()
    {
        var rise = HolderFace * Math.Cos(HolderTilt);
        var reach = HolderFace * Math.Sin(HolderTilt);
        var hingeL = new Point3D(-HolderHalf, Ground + rise, 0);
        var hingeR = new Point3D(HolderHalf, Ground + rise, 0);
        var frontL = new Point3D(-HolderHalf, Ground, reach);
        var frontR = new Point3D(HolderHalf, Ground, reach);
        var backL = new Point3D(-HolderHalf, Ground, -reach);
        var backR = new Point3D(HolderHalf, Ground, -reach);
        var frontN = new Vector3D(0, Math.Sin(HolderTilt), Math.Cos(HolderTilt));
        var backN = new Vector3D(0, Math.Sin(HolderTilt), -Math.Cos(HolderTilt));
        const double lift = 0.02;

        _glassFront.Geometry = Quad(hingeL + frontN * lift, hingeR + frontN * lift, frontR + frontN * lift, frontL + frontN * lift, new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(0, 1));
        _glassBack.Geometry = Quad(hingeR + backN * lift, hingeL + backN * lift, backL + backN * lift, backR + backN * lift, new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(0, 1));

        var shell = new MeshGeometry3D();
        const double bar = 0.034;
        AddBeam(shell, hingeL, hingeR, bar * 1.15);
        AddBeam(shell, hingeL, frontL, bar);
        AddBeam(shell, hingeR, frontR, bar);
        AddBeam(shell, frontL, frontR, bar * 0.85);
        AddBeam(shell, hingeL, backL, bar);
        AddBeam(shell, hingeR, backR, bar);
        AddBeam(shell, backL, backR, bar * 0.85);
        AddBeam(shell, frontL, backL, bar * 0.72);
        AddBeam(shell, frontR, backR, bar * 0.72);
        var frame = Glass(168);
        _holder.Children.Clear();
        _holder.Children.Add(new GeometryModel3D { Geometry = shell, Material = frame, BackMaterial = frame });

        var side = Glass(28);
        _holder.Children.Add(new GeometryModel3D
        {
            Geometry = Triangle(hingeL, frontL, backL),
            Material = side,
            BackMaterial = side
        });
        _holder.Children.Add(new GeometryModel3D
        {
            Geometry = Triangle(hingeR, backR, frontR),
            Material = side,
            BackMaterial = side
        });
    }

    private static Material Glass(byte alpha)
    {
        var tint = Freeze(new SolidColorBrush(Color.FromArgb(alpha, 214, 226, 230)));
        var shine = Freeze(new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)));
        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(tint));
        group.Children.Add(new SpecularMaterial(shine, alpha > 100 ? 40 : 16));
        group.Freeze();
        return group;
    }

    private static double Smooth(double t) => t * t * (3 - 2 * t);

    private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsOverReplay(e.OriginalSource as DependencyObject))
            return;
        _dragging = true;
        _dragStart = e.GetPosition(this);
        CaptureMouse();
    }

    private bool IsOverReplay(DependencyObject? source)
    {
        while (source != null)
        {
            if (ReferenceEquals(source, _replay))
                return true;
            source = source is Visual ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }
        return false;
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
            return;
        var point = e.GetPosition(this);
        _yaw += (point.X - _dragStart.X) * 0.012;
        _dragStart = point;
        UpdateScene();
        e.Handled = true;
    }

    private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
            return;
        _dragging = false;
        ReleaseMouseCapture();
    }

    private static MeshGeometry3D Quad(Point3D a, Point3D b, Point3D c, Point3D d, Point ta, Point tb, Point tc, Point td)
    {
        return new MeshGeometry3D
        {
            Positions = new Point3DCollection { a, b, c, d },
            TextureCoordinates = new PointCollection { ta, tb, tc, td },
            TriangleIndices = new Int32Collection { 0, 3, 2, 0, 2, 1 }
        };
    }

    private static MeshGeometry3D Triangle(Point3D a, Point3D b, Point3D c)
    {
        return new MeshGeometry3D
        {
            Positions = new Point3DCollection { a, b, c },
            TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 1 }
        };
    }

    private static void AddBeam(MeshGeometry3D mesh, Point3D a, Point3D b, double radius)
    {
        var dir = b - a;
        if (dir.Length < 1e-6)
            return;
        dir.Normalize();
        var helper = Math.Abs(dir.Y) < 0.85 ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0);
        var side = Vector3D.CrossProduct(dir, helper);
        side.Normalize();
        var up = Vector3D.CrossProduct(side, dir);
        up.Normalize();
        side *= radius;
        up *= radius;
        Point3D Shift(Point3D p, Vector3D s, Vector3D u) => p + s + u;
        var a0 = Shift(a, side, up);
        var a1 = Shift(a, -side, up);
        var a2 = Shift(a, -side, -up);
        var a3 = Shift(a, side, -up);
        var b0 = Shift(b, side, up);
        var b1 = Shift(b, -side, up);
        var b2 = Shift(b, -side, -up);
        var b3 = Shift(b, side, -up);
        AddFace(mesh, a0, b0, b1, a1);
        AddFace(mesh, a1, b1, b2, a2);
        AddFace(mesh, a2, b2, b3, a3);
        AddFace(mesh, a3, b3, b0, a0);
        AddFace(mesh, a3, a2, a1, a0);
        AddFace(mesh, b0, b1, b2, b3);
    }

    private static void AddFace(MeshGeometry3D mesh, Point3D a, Point3D b, Point3D c, Point3D d)
    {
        var start = mesh.Positions.Count;
        mesh.Positions.Add(a);
        mesh.Positions.Add(b);
        mesh.Positions.Add(c);
        mesh.Positions.Add(d);
        mesh.TriangleIndices.Add(start);
        mesh.TriangleIndices.Add(start + 1);
        mesh.TriangleIndices.Add(start + 2);
        mesh.TriangleIndices.Add(start);
        mesh.TriangleIndices.Add(start + 2);
        mesh.TriangleIndices.Add(start + 3);
    }

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(
            name,
            typeof(T),
            typeof(FoldDemoControl),
            new PropertyMetadata(defaultValue, (d, _) => ((FoldDemoControl)d).RebuildTexture()));

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
