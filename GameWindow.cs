﻿using Mycraft.WorldUI;
using Mycraft.Utils;
using Mycraft.World;
using OpenGL;
using System;
using System.Drawing;
using System.Windows.Forms;
using Mycraft.GUI;
using Mycraft.Physics;
using System.Diagnostics;
using Mycraft.Blocks;

// TODO make good file not found handling

namespace Mycraft
{
    public class GameWindow : Form
    {
        private readonly GlControl glControl;
        private readonly Stopwatch stopwatch;

        private Origin origin;
        private GameWorld world;
        private Selection selection;
        private GUIRectangle cross;

        private Hotbar hotbar;

        private FallingBox playerBox;

        private const float MOVEMENT_SPEED = 3.7f, MOUSE_SENSIVITY = .003f;
        private Camera camera;
        private Matrix4x4f projection;

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
                DepthBits = 8u,
                MultisampleBits = 4u,
                StencilBits = 0u
            };

            Resize += OnResized;
            glControl.MouseEnter += (o, e) => Cursor.Hide();
            glControl.MouseMove += OnMouseMove;
            glControl.MouseDown += OnMouseDown;
            glControl.MouseWheel += OnMouseWheel;
            glControl.ContextCreated += OnContextCreated;
            glControl.ContextDestroying += OnContextDestroyed;
            glControl.ContextUpdate += OnContextUpdate;
            glControl.Render += Render;

            Controls.Add(glControl);
            ResumeLayout(false);

            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            int select = hotbar.Selected - e.Delta / 120;
            if (select < 0)
                hotbar.Selected = Hotbar.CAPACITY - 1;
            else
                hotbar.Selected = select % Hotbar.CAPACITY;
        }

        private (float dx, float dy) GrabCursor()
        {
            Point screenLocation = Location;
            Point screenCenter = new Point(ClientSize.Width / 2, ClientSize.Height / 2);
            Point cursorPos = new Point(
                screenLocation.X + screenCenter.X,
                screenLocation.Y + screenCenter.Y
            );

            float dx = Cursor.Position.X - cursorPos.X;
            float dy = Cursor.Position.Y - cursorPos.Y;

            Cursor.Position = cursorPos;
            return (dx, dy);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            const float HALF_PI = .5f * (float)Math.PI;

            var (dx, dy) = GrabCursor();

            Vertex2f rotation = camera.Rotation;
            rotation.x = FuncUtils.FixRotation(rotation.x + MOUSE_SENSIVITY * dx);
            rotation.y = FuncUtils.Clamp(-HALF_PI, rotation.y - MOUSE_SENSIVITY * dy, HALF_PI);

            camera.Rotation = rotation;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (selection.IsSelected)
            {
                if (e.Button == MouseButtons.Left)
                {
                    world.SetBlock(
                        selection.Position.x,
                        selection.Position.y,
                        selection.Position.z,
                        BlockRegistry.Air
                    );
                }
                else if (e.Button == MouseButtons.Right)
                {
                    Vertex3i placeBlockCoords = Block.GetNeighbour(selection.Position, selection.Side);
                    world.SetBlock(
                        placeBlockCoords.x,
                        placeBlockCoords.y,
                        placeBlockCoords.z,
                        hotbar.SelectedBlock
                    );
                }
            }
        }
           
        private void OnResized(object sender, EventArgs e)
        {
            Gl.Viewport(0, 0, ClientSize.Width, ClientSize.Height);
            projection = Matrix4x4f.Perspective(70, (float)ClientSize.Width / ClientSize.Height, .01f, 100f);

            Gl.UseProgram(Resources.GUIShader.glId);
            Resources.GUIShader.Projection = Matrix4x4f.Ortho2D(0f, ClientSize.Width - 1, ClientSize.Height - 1, 0f);

            int pixelSize = ClientSize.Height / 200;

            cross = new GUIRectangle(
                new Vertex2i(
                    ClientSize.Width / 2 - 6 * pixelSize,
                    ClientSize.Height / 2 - 6 * pixelSize
                ),
                new Vertex2i(12 * pixelSize, 12 * pixelSize)
            );

            hotbar = new Hotbar(
                new Vertex2i(
                    ClientSize.Width / 2 - 91 * pixelSize,
                    ClientSize.Height - 21 * pixelSize
                ),
                pixelSize,
                1, new Block[] { BlockRegistry.Stone, BlockRegistry.Grass, BlockRegistry.Dirt, null, null, null, null, null, null, null }
            );
        }

        private void OnContextCreated(object sender, GlControlEventArgs e)
        {
            Resources.LoadAll();

            OnResized(null, null);
            Cursor.Hide();
            GrabCursor();

            origin = new Origin();
            selection = new Selection();

            Gl.LineWidth(2f);
            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.Multisample);

            world = new GameWorld();
            world.GenerateSpawnArea();
            world.Update();

            playerBox = new FallingBox(world, new Vertex3f(.25f, 20f, .25f), new Vertex3f(.75f, 1.7f, .75f));
            camera = new Camera(new Vertex3f(.5f, 3.5f, .5f), new Vertex2f(0f, 0f));
        }

        private void OnContextUpdate(object sender, GlControlEventArgs e)
        {
            double deltaTime = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
            world.Update();

            int forwardInput    = FuncUtils.GetInput1d(Keys.W, Keys.S);
            int horizontalInput = FuncUtils.GetInput1d(Keys.D, Keys.A);

            playerBox.Move(camera.RelativeToYaw(
                forwardInput,
                horizontalInput
            ) * (deltaTime * MOVEMENT_SPEED));
            playerBox.Update(deltaTime);
            if (playerBox.IsGrounded && FuncUtils.IsKeyPressed(Keys.Space))
            {
                Vertex3f velocity = playerBox.Velocity;
                velocity.y = 8f;
                playerBox.Velocity = velocity;
            }

            if (playerBox.Position.y < -64f)
            {
                Vertex3f velocity = playerBox.Velocity;
                velocity.y *= -1f;
                playerBox.Velocity = velocity;
            }

            /*
            float speed = deltaTime * MOVEMENT_SPEED;
            camera.MoveRelativeToYaw(
                speed * forwardInput,
                speed * horizontalInput
            );
            camera.Translate(0f, FuncUtils.GetInput1d(Keys.Space, Keys.LShiftKey) * speed, 0f);
            */
            camera.Position = playerBox.Position + new Vertex3f(.375f, 1.5f, .375f);
            camera.UpdateTransformMatrix();

            if (RayCasting.Raycast(world, camera.Position, camera.Forward, out Hit hit))
                selection.Select(hit.blockCoords, hit.side);
            else
                selection.Deselect();

            if (camera.Position.y < 0)
                Gl.ClearColor(.05f, .05f, .05f, 1f);
            else
                Gl.ClearColor(0.53f, 0.81f, 0.98f, 1f);
        }

        private void Render(object sender, GlControlEventArgs e)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Matrix4x4f vp = projection * camera.TransformMatrix;

            // Draw the world
            Gl.UseProgram(Resources.GameWorldShader.glId);
            Gl.Enable(EnableCap.DepthTest);
            Resources.GameWorldShader.MVP = vp;
            world.Draw();

            // Draw UI stuff
            Gl.UseProgram(Resources.WorldUIShader.glId);
            Resources.WorldUIShader.VP = vp;
            selection.Draw();

            Resources.WorldUIShader.Model = Matrix4x4f.Identity;
            origin.Draw();

            // Draw GUI
            Gl.UseProgram(Resources.GUIShader.glId);
            Gl.Disable(EnableCap.DepthTest);
            Gl.Disable(EnableCap.CullFace);

            Resources.CrossTexture.Bind();
            cross.Draw();
            
            hotbar.Draw();
        }

        private void OnContextDestroyed(object sender, GlControlEventArgs e)
        {
            Resources.DisposeAll();
            world.Dispose();
            origin.Dispose();
            selection.Dispose();
            hotbar.Dispose();
        }
    }
}
