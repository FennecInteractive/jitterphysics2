using System;
using System.Collections.Generic;
using System.Linq;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Demo31 : IDemo, IDrawUpdate
{
    public string Name => "Spaceship Move Towards";
    public string Description => "Spaceship testing move towards functionality.";

    private const float MaxSpeed = 20f;
    private const float MaxTurnSpeed = 4f;

    // Force/torque tuning: higher values = snappier acceleration
    private const float LinearAcceleration = 80f;    // N (force per unit error, scaled by mass)
    private const float LinearDrag = 20f;            // damping force opposing current velocity
    private const float AngularAcceleration = 60f;   // N·m (torque per unit error, scaled by inertia)
    private const float AngularDrag = 15f;           // damping torque opposing current angular velocity

    private Teapot teapot = null!;
    private RigidBody teapotBody = null!;
    private RigidBody square = null!;
    private Matrix4 shift;

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();


        // Add Square
        square = world.CreateRigidBody();
        square.AffectedByGravity = false;
        square.Position = new JVector(10, 10, 10);
        square.AddShape(new BoxShape(3f, 3f, 3f));
        square.SetMassInertia(10);


        // Add teapot
        teapot = RenderWindow.Instance.GetDrawable<Teapot>();

        var vertices = teapot.Mesh.Vertices.Select(v
            => new JVector(v.Position.X, v.Position.Y, v.Position.Z)).Distinct().ToList();

        // Find a few points on the convex hull of the teapot.
        var reducedVertices = ShapeHelper.SampleHull(vertices, subdivisions: 3);

        // Use these points to create a PointCloudShape. One could also use all vertices
        // of the teapot, but this would be slower since it also includes vertices that are
        // inside the convex hull of the teapot.
        PointCloudShape pcs = new PointCloudShape(reducedVertices);

        // Jitter requires the center of mass to be at the origin!

        // get the center of mass and shift the convex hull, such that the new
        // center of mass is at the origin.
        pcs.GetCenter(out JVector ctr);
        pcs.Shift = -ctr;
        // pcs.GetCenter(out JVector ctr); <- this would now return (0,0,0)

        // also shift the visual representation of the teapot
        shift = MatrixHelper.CreateTranslation(-ctr.X, -ctr.Y, -ctr.Z);

        teapotBody = world.CreateRigidBody();
        teapotBody.AffectedByGravity = false;

        teapotBody.Damping = (0.005f, 0.005f);
        teapotBody.Position = new JVector(0, 10, 0);
        teapotBody.AddShape(pcs.Clone());
        teapotBody.SetMassInertia(10);
    }

    public void DrawUpdate()
    {
        JVector toTarget = square.Position - teapotBody.Position;
        float distance = toTarget.Length();

        // Local forward is -Z axis of the body orientation (unit quaternion basis vectors are already normalized)
        JVector forward = -teapotBody.Orientation.GetBasisZ();

        // Mass is needed to convert desired acceleration into force (F = m·a)
        float mass = teapotBody.Mass;

        // --- Steering: proportional controller rotating forward toward the target ---
        if (distance > 0.01f)
        {
            JVector desiredDir = toTarget * (1f / distance);

            JVector turnAxis = forward % desiredDir;
            float sinAngle = turnAxis.Length();
            float dot = Math.Clamp(JVector.Dot(forward, desiredDir), -1f, 1f);

            JVector desiredAngularVelocity = JVector.Zero;

            if (sinAngle > 0.0001f)
            {
                float angle = MathF.Acos(dot);
                JVector turnAxisNorm = turnAxis * (1f / sinAngle);
                float turnSpeed = Math.Min(angle / MathF.PI * MaxTurnSpeed * 2f, MaxTurnSpeed);
                desiredAngularVelocity += turnAxisNorm * turnSpeed;
            }
            else if (dot < 0f)
            {
                JVector perp = MathF.Abs(forward.X) < 0.9f ? JVector.UnitX : JVector.UnitY;
                JVector axis = JVector.NormalizeSafe(forward % perp);
                desiredAngularVelocity += axis * MaxTurnSpeed;
            }

            // Forward thrust: proportional force toward desired speed (F = m·a), plus opposing drag.
            // Remap dot [-1,1] → speed fraction [0,1], clamped to a minimum of 0.4 so the teapot
            // slows slightly on sharp turns but never drops to a near-stop.
            float speedFraction = Math.Max(0.4f, (dot + 1f) * 0.5f);
            float desiredSpeed = speedFraction * MaxSpeed;
            float currentSpeed = JVector.Dot(teapotBody.Velocity, forward);
            float speedError = desiredSpeed - currentSpeed;
            teapotBody.AddForce(forward * (speedError * LinearAcceleration * mass));
            teapotBody.AddForce(-teapotBody.Velocity * (LinearDrag * mass));

            // Rotational drive: torque proportional to angular velocity error (P controller), plus angular drag.
            // The inertia tensor in the solver scales the resulting angular acceleration automatically.
            JVector angularError = desiredAngularVelocity - teapotBody.AngularVelocity;
            teapotBody.Torque += angularError * AngularAcceleration;
            teapotBody.Torque += -teapotBody.AngularVelocity * AngularDrag;
        }
        else
        {
            // Brake to a stop
            teapotBody.AddForce(-teapotBody.Velocity * (LinearDrag * mass * 3f));
            teapotBody.Torque += -teapotBody.AngularVelocity * (AngularDrag * 3f);
        }

        // --- Upright correction: trend local up back toward world up ---
        // The correction axis is the cross product of local up and world up, which is always
        // perpendicular to the yaw/pitch steering axis, so it never interferes with steering.
        // Only the angular velocity component along the tilt axis is damped here.
        JVector localUp = teapotBody.Orientation.GetBasisY();
        JVector worldUp = JVector.UnitY;
        JVector uprightAxis = localUp % worldUp;
        float sinTilt = uprightAxis.Length();

        if (sinTilt > 0.0001f)
        {
            float tiltDot = Math.Clamp(JVector.Dot(localUp, worldUp), -1f, 1f);
            float tiltAngle = MathF.Acos(tiltDot);
            JVector uprightAxisNorm = uprightAxis * (1f / sinTilt);
            float desiredUprightSpeed = Math.Min(tiltAngle / MathF.PI * MaxTurnSpeed * 2f, MaxTurnSpeed);
            float currentUprightOmega = JVector.Dot(teapotBody.AngularVelocity, uprightAxisNorm);
            JVector uprightAngularError = uprightAxisNorm * (desiredUprightSpeed - currentUprightOmega);
            teapotBody.Torque += uprightAngularError * AngularAcceleration;
        }

        var color = ColorGenerator.GetColor(teapotBody.GetHashCode());
        if (!teapotBody.IsActive) color += new Vector3(0.2f, 0.2f, 0.2f);
        teapot.Push(Conversion.FromJitter(teapotBody) * shift, color);
    }
}