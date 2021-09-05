﻿using Mycraft.Graphics;
using Mycraft.Utils;
using OpenGL;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Mycraft
{
    public class GameWindow : Form
    {
        private const float RADIANS_TO_DEGREES = (float)(180d / Math.PI);
        private readonly GlControl glControl;

        private ShaderProgram program;
        private VertexArray trangle;

        private readonly float[] vertices =
        {
             1f, -1f, 0f,
            -1f, -1f, 0f,
             0f,  1f, 0f
        };

        private readonly string vertexSource =
@"#version 330 core

layout(location = 0) in vec3 position;
uniform mat4 mvp;

void main()
{
    gl_Position = mvp * vec4(position, 1.0);
}";

        private readonly string fragmentSource =
@"#version 330 core

void main()
{
    gl_FragColor = vec4(1.0);
}";

        private Matrix4x4f view, projection;
        private Vertex3f cameraPosition = new Vertex3f(0f, 0f, -5f);
        private Vertex2f cameraRotation;
        private readonly Input2d movementInput, rotationInput;

        public GameWindow()
        {
            SuspendLayout();

            Name = "GameWindow";
            Text = "Mycraft";
            KeyPreview = true;
            ClientSize = new Size(1920, 1080);

            glControl = new GlControl
            {
                Name = "GLControl",
                Dock = DockStyle.Fill,

                Animation = true,
                AnimationTimer = false,

                ColorBits = 24u,
                DepthBits = 0u,
                MultisampleBits = 0u,
                StencilBits = 0u
            };

            Resize += OnResized;
            glControl.ContextCreated += OnContextCreated;
            glControl.ContextDestroying += OnContextDestroyed;
            glControl.ContextUpdate += OnContextUpdate;
            glControl.Render += Render;

            Controls.Add(glControl);
            ResumeLayout(false);

            movementInput = new Input2d(this, Keys.W, Keys.A, Keys.S, Keys.D);
            rotationInput = new Input2d(this, Keys.U, Keys.H, Keys.J, Keys.K);
        }

        private void OnResized(object sender, EventArgs e)
        {
            Gl.Viewport(0, 0, ClientSize.Width, ClientSize.Height);

            projection = Matrix4x4f.Perspective(70, (float)ClientSize.Width / ClientSize.Height, .01f, 100f);
        }

        private void OnContextCreated(object sender, GlControlEventArgs e)
        {
            OnResized(null, null);

            program = new ShaderProgram(vertexSource, fragmentSource);
            trangle = new VertexArray(vertices);
        }

        private void OnContextUpdate(object sender, GlControlEventArgs e)
        {
            cameraRotation.x += .05f * rotationInput.X;
            cameraRotation.y -= .05f * rotationInput.Y;

            cameraPosition.z += .2f * (float)Math.Cos(cameraRotation.x) * movementInput.Y
                              - .2f * (float)Math.Sin(cameraRotation.x) * movementInput.X;
            cameraPosition.x -= .2f * (float)Math.Sin(cameraRotation.x) * movementInput.Y
                              + .2f * (float)Math.Cos(cameraRotation.x) * movementInput.X;

            view = Matrix4x4f.RotatedX(cameraRotation.y * RADIANS_TO_DEGREES)
                 * Matrix4x4f.RotatedY(cameraRotation.x * RADIANS_TO_DEGREES)
                 * Matrix4x4f.Translated(cameraPosition.x, cameraPosition.y, cameraPosition.z);

            Text = $"Mycraft | yaw = {cameraRotation.x}";
        }

        private void Render(object sender, GlControlEventArgs e)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            Gl.UseProgram(program.glId);
            program.MVP = projection * view;
            trangle.Draw(PrimitiveType.Triangles);
        }

        private void OnContextDestroyed(object sender, GlControlEventArgs e)
        {
            program.Dispose();
            trangle.Dispose();
        }
    }
}
