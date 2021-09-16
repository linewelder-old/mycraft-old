﻿using Mycraft.World;
using OpenGL;
using System;
using System.Collections.Generic;

namespace Mycraft.Physics
{
    public class FallingBox : AABB
    {
        private const float GRAVITY = 0.4f;

        public Vertex3f Velocity { get => velocity; set => velocity = value; }

        public bool IsGrounded { get; private set; }

        private GameWorld world;
        private Vertex3f velocity;

        public FallingBox(GameWorld world, Vertex3f position, Vertex3f size)
            : base(position, size)
        {
            this.world = world;
        }

        private Vertex3i ToBlockCoords(Vertex3f coords)
            => new Vertex3i(
                (int)Math.Floor(coords.x),
                (int)Math.Floor(coords.y),
                (int)Math.Floor(coords.z)
            );

        public void Update(double deltaTime)
        {
            Move(velocity * deltaTime);
            velocity.y -= GRAVITY;
            IsGrounded = false;

            Vertex3f boxEnd_ = Position + Size;
            Vertex3i boxStart = ToBlockCoords(Position);
            Vertex3i boxEnd = ToBlockCoords(boxEnd_);

            List<AABB> aabbs = new List<AABB>();
            for (int x = boxStart.x; x <= boxEnd.x; x++)
                for (int y = boxStart.y; y <= boxEnd.y; y++)
                    for (int z = boxStart.z; z <= boxEnd.z; z++)
                        if (world.GetBlock(x, y, z).HasCollider)
                            aabbs.Add(new AABB(new Vertex3f(x, y, z), new Vertex3f(1f, 1f, 1f)));

            Start();
            if (CollideX(aabbs)) velocity.x = 0f;
            if (CollideY(aabbs))
            {
                IsGrounded = velocity.y < 0;
                velocity.y = 0f;
            }
            if (CollideZ(aabbs)) velocity.z = 0f;
            End();
        }
    }
}