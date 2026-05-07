using System;
using System.Collections.Generic;
using System.Drawing;
using Vellum;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;
using Color = Vellum.Rendering.Color;

namespace JitterDemo;

public class RigidBodyTag(bool doNotDraw = true)
{
    public bool DoNotDraw { get; set; } = doNotDraw;
}

// Built-in primitive drawables. Subclassing is the simplest way to key them in
// the RenderWindow's Type-based registry and to configure per-shape material bits.
public sealed class CubeDrawable() : InstancedDrawable(Mesh.Cube());
public sealed class SphereDrawable() : InstancedDrawable(Mesh.Sphere());
public sealed class ConeDrawable() : InstancedDrawable(Mesh.Cone());
public sealed class CylinderDrawable() : InstancedDrawable(Mesh.Cylinder());
public sealed class HalfSphereDrawable() : InstancedDrawable(Mesh.HalfSphere());

public sealed class FloorDrawable : InstancedDrawable
{
    public FloorDrawable() : base(Mesh.Quad(halfSize: 100, uvScale: 100))
    {
        var texture = new Texture2D();
        Image.LoadImage(System.IO.Path.Combine("assets", "unit.tga"))
             .FixedData((img, ptr) => texture.LoadImage(ptr, img.Width, img.Height));
        texture.SetWrap(TextureWrap.Repeat);
        texture.SetAnisotropy(Anisotropy.X8);

        Material = new Material
        {
            Tint = Vector3.Zero,
            Specular = new Vector3(0.1f, 0.1f, 0.1f),
            Shininess = 10f,
            Alpha = 1f,
            VertexColorWeight = 0f,
            TextureWeight = 1f,
            Texture = texture
        };
    }
}

public partial class Playground : RenderWindow
{
    private readonly World world;

    private const float PhysicsTimestep = 1f / 100f;
    private bool multiThread = true;
    private RigidBodyShape? floorShape;

    private CubeDrawable cubes = null!;
    private SphereDrawable spheres = null!;
    private ConeDrawable cones = null!;
    private CylinderDrawable cylinders = null!;
    private HalfSphereDrawable halfSpheres = null!;
    private FloorDrawable floor = null!;

    public CubeDrawable Cubes => cubes;
    public SphereDrawable Spheres => spheres;
    public ConeDrawable Cones => cones;
    public CylinderDrawable Cylinders => cylinders;
    public HalfSphereDrawable HalfSpheres => halfSpheres;
    public FloorDrawable Floor => floor;

    private readonly List<IDemo> demos = new()
    {
        new Demo00(),
        new Demo01(),
        new Demo02(),
        new Demo03(),
        new Demo04(),
        new Demo05(),
        new Demo06(),
        new Demo07(),
        //new Demo08(),   // contact manifold test
        new Demo09(),
        new Demo10(),
        // new Demo11(),  // double pendulum
        new Demo12(),
        new Demo13(),
        new Demo14(),
        new Demo15(),
        new Demo16(),
        new Demo17(),
        // new Demo18(),  // point test
        // new Demo19(),  // ray cast test
        new Demo20(),
        new Demo21(),
        new Demo22(),
        new Demo23(),
        new Demo24(),
        new Demo25(),
        new Demo26(), // angular sweep
        new Demo27(),
        new Demo28(),
        new Demo29(),
        new Demo30(),
        new Demo31(),
    };

    private IDemo? currentDemo;

    private void SwitchDemo(int index)
    {
        (currentDemo as ICleanDemo)?.CleanUp();
        ResetScene();
        selectedDemoIndex = index;
        currentDemo = demos[index];
        currentDemo.Build(this, world);
    }

    public Playground()
    {
        world = new World();
        world.NullBody.Tag = new RigidBodyTag();
        drawBox = DrawBox;
    }

    private void ResetScene()
    {
        floorShape = null;
        world.Clear();
        world.DynamicTree.Filter = World.DefaultDynamicTreeFilter;
        world.BroadPhaseFilter = null;
        world.NarrowPhaseFilter = new TriangleEdgeCollisionFilter();
        world.Gravity = new JVector(0, -9.81f, 0);
        world.SubstepCount = 1;
        world.SolverIterations = (8, 4);
    }

    public void AddFloor()
    {
        RigidBody body = World.CreateRigidBody();
        floorShape = new BoxShape(200, 200, 200);
        body.Position = new JVector(0, -100, 0f);
        body.MotionType = MotionType.Static;
        body.AddShape(floorShape);
    }

    public override void Load()
    {
        base.Load();
        ResetScene();
        AddFloor();

        cubes = GetDrawable<CubeDrawable>();
        spheres = GetDrawable<SphereDrawable>();
        cones = GetDrawable<ConeDrawable>();
        cylinders = GetDrawable<CylinderDrawable>();
        halfSpheres = GetDrawable<HalfSphereDrawable>();
        floor = GetDrawable<FloorDrawable>();

        VerticalSync = false;

        Gui.Theme = new Theme();
        Gui.Theme.PanelBg = new Color(20, 20, 20, 80);
        Gui.Theme.PanelBorder = new Color(156, 156, 156, 72);
        Gui.Theme.ButtonBg = new Color(68, 68, 68, 120);
        Gui.Theme.ButtonBgHover = new Color(92, 92, 92, 152);
        Gui.Theme.ButtonBgPressed = new Color(60, 60, 60, 136);
        Gui.Theme.ButtonBorder = new Color(148, 148, 148, 72);
        Gui.Theme.ButtonBorderHover = new Color(188, 188, 188, 96);
        Gui.Theme.ButtonBorderPressed = new Color(140, 140, 140, 84);
        Gui.Theme.ToggleBg = new Color(72, 72, 72, 132);
        Gui.Theme.ToggleBgHover = new Color(72, 104, 156, 176);
        Gui.Theme.ToggleBgPressed = new Color(60, 90, 140, 188);
        Gui.Theme.ToggleBgActive = new Color(88, 78, 58, 176);
        Gui.Theme.ToggleBorder = new Color(148, 148, 148, 72);
        Gui.Theme.ToggleBorderHover = new Color(154, 184, 224, 124);
        Gui.Theme.ToggleBorderPressed = new Color(132, 166, 210, 132);
        Gui.Theme.ToggleBorderActive = new Color(255, 200, 90, 216);
        Gui.Theme.PlotBg = new Color(54, 84, 132, 108);
        Gui.Theme.PlotBorder = new Color(136, 170, 222, 132);
        Gui.Theme.PlotFill = new Color(255, 186, 52, 240);
        Gui.Theme.SliderBg = new Color(40, 48, 60, 188);
        Gui.Theme.SliderBgHover = new Color(48, 58, 72, 204);
        Gui.Theme.SliderBgActive = new Color(34, 42, 54, 216);
        Gui.Theme.SliderFill = new Color(77, 121, 186, 222);
        Gui.Theme.SliderFillActive = new Color(96, 145, 216, 234);
        Gui.Theme.SliderBorder = new Color(142, 152, 168, 108);
        Gui.Theme.Separator = new Color(196, 196, 196, 112);
        Gui.Theme.SelectableBg = Color.Transparent;
        Gui.Theme.SelectableBgHover = new Color(72, 118, 188, 76);
        Gui.Theme.SelectableBgPressed = new Color(72, 118, 188, 112);
        Gui.Theme.SelectableBgSelected = new Color(72, 118, 188, 188);
        Gui.Theme.SelectableBorder = Color.Transparent;
        Gui.Theme.SelectableBorderHover = Color.Transparent;
        Gui.Theme.SelectableBorderPressed = Color.Transparent;
        Gui.Theme.SelectableBorderSelected = new Color(128, 170, 226, 136);
        Gui.Theme.CollapsingHeaderBg = Color.Transparent;
        Gui.Theme.CollapsingHeaderBgHover = new Color(72, 118, 188, 64);
        Gui.Theme.CollapsingHeaderBgPressed = new Color(72, 118, 188, 96);
        Gui.Theme.CollapsingHeaderBgOpen = Color.Transparent;
        Gui.Theme.CollapsingHeaderPadding = new EdgeInsets(2f, 2f);
        Gui.Theme.ScrollbarTrack = new Color(80, 80, 80, 150);
        Gui.Theme.ScrollbarThumb = new Color(40, 40, 40, 150);
        Gui.Theme.ScrollbarThumbHover = new Color(120, 166, 232, 150);
        Gui.Theme.ScrollbarThumbActive = new Color(120, 166, 232, 208);
        Gui.Theme.WindowTitleText = new Color(255, 200, 90);
        Gui.Theme.TextPrimary = Color.White;
        Gui.Theme.TextSecondary = new Color(228, 228, 228);
        Gui.Theme.TreeIndent = 0f;
        Gui.Theme.UseLcdText = true;
        Gui.Theme.SliderBlockWidthFactor = 0.2f;
    }

    public RigidBodyShape? FloorShape => floorShape;

    public World World => world;

    public void ShootPrimitive()
    {
        const float primitiveVelocity = 20f;

        var pos = Camera.Position;
        var dir = Camera.Direction;

        var sb = World.CreateRigidBody();
        sb.Position = Conversion.ToJitterVector(pos);
        sb.Velocity = Conversion.ToJitterVector(dir * primitiveVelocity);

        var ss = new BoxShape(1);
        sb.AddShape(ss);
    }

    private void DrawShape(Shape shape, in Matrix4 mat, in Vector3 color)
    {
        Matrix4 ms;

        switch (shape)
        {
            case BoxShape s:
                ms = MatrixHelper.CreateScale(s.Size.X, s.Size.Y, s.Size.Z);
                cubes.Push(mat * ms, color);
                break;
            case SphereShape s:
                ms = MatrixHelper.CreateScale(s.Radius * 2);
                spheres.Push(mat * ms, color);
                break;
            case CylinderShape s:
                ms = MatrixHelper.CreateScale(s.Radius, s.Height, s.Radius);
                cylinders.Push(mat * ms, color);
                break;
            case CapsuleShape s:
                ms = MatrixHelper.CreateScale(s.Radius, s.Length, s.Radius);
                cylinders.Push(mat * ms, color);
                ms = MatrixHelper.CreateTranslation(0, 0.5f * s.Length, 0) * MatrixHelper.CreateScale(s.Radius * 2);
                halfSpheres.Push(mat * ms, color);
                halfSpheres.Push(mat * MatrixHelper.CreateRotationX(MathF.PI) * ms, color);
                break;
            case ConeShape s:
                ms = MatrixHelper.CreateScale(s.Radius * 2, s.Height, s.Radius * 2);
                cones.Push(mat * ms, color);
                break;
        }
    }

    protected override void DrawCustomOverlay(int logicalWidth, int logicalHeight,
        int framebufferWidth, int framebufferHeight)
    {
        if (!overlayFramePrepared)
            PrepareCustomOverlayFrame(logicalWidth, logicalHeight, framebufferWidth, framebufferHeight);

        Gui.EndFrame();
        overlayFramePrepared = false;
        WantsCaptureKeyboard = Gui.WantsCaptureKeyboard;
        WantsCaptureMouse = Gui.WantsCaptureMouse;
    }

    public override void Draw()
    {
        world.Step(PhysicsTimestep, multiThread);

        UpdateDisplayText();
        (int framebufferWidth, int framebufferHeight) = FramebufferSize;
        PrepareCustomOverlayFrame(Width, Math.Max(1, Height), framebufferWidth, framebufferHeight);

        foreach (RigidBody body in world.RigidBodies)
        {
            if (body.Tag is RigidBodyTag { DoNotDraw: true }) continue;

            Matrix4 mat = Conversion.FromJitter(body);

            foreach (var shape in body.Shapes)
            {
                if (shape == floorShape)
                {
                    floor.Push(Matrix4.Identity);
                    continue;
                }

                var color = ColorGenerator.GetColor(shape.GetHashCode());
                if (!shape.RigidBody.Data.IsActive) color += new Vector3(0.2f, 0.2f, 0.2f);

                if (shape is TransformedShape ts)
                {
                    Matrix4 tmat = mat * MatrixHelper.CreateTranslation(Conversion.FromJitter(ts.Translation)) *
                                   Conversion.FromJitter(ts.Transformation);
                    DrawShape(ts.OriginalShape, tmat, color);
                }
                else
                {
                    DrawShape(shape, mat, color);
                }
            }
        }

        (currentDemo as IDrawUpdate)?.DrawUpdate();

        DebugDraw();

        if (!WantsCaptureMouse && (Mouse.ButtonPressBegin(Mouse.Button.Left) || grabbing))
        {
            Pick();
        }

        if (!Mouse.IsButtonDown(Mouse.Button.Left))
        {
            if (grabConstraint != null) world.Remove(grabConstraint);
            grabBody = null;
            grabConstraint = null;
            grabbing = false;
        }

        if (Keyboard.KeyPressBegin(Keyboard.Key.M))
        {
            multiThread = !multiThread;
        }

        if (Keyboard.KeyPressBegin(Keyboard.Key.Space))
        {
            ShootPrimitive();
        }

        base.Draw();
    }
}
