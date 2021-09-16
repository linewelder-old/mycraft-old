﻿using OpenGL;
using System.Collections.Generic;

namespace Mycraft.Physics
{
    public class AABB
    {
        public Vertex3f Size { get; set; }
        public Vertex3f Position => position;

        private Vertex3f position, lastPosition;
        private Vertex3f delta;

        public AABB(Vertex3f position, Vertex3f size)
        {
            this.position = position;
            Size = size;

            lastPosition = position;
            delta = new Vertex3f();
        }

        /// <summary>
        /// Teleport the box to the coords.
        /// </summary>
        /// <param name="pos">The coords</param>
        public void ForceMoveTo(Vertex3f pos)
        {
            lastPosition = pos;
            position = pos;
            delta = new Vertex3f();
        }

        /// <summary>
        /// Move the box ignoring collisions.
        /// </summary>
        /// <param name="d">The movement amount</param>
        public void ForceMove(Vertex3f d)
            => ForceMoveTo(position + d);

        /// <summary>
        /// Move the box.
        /// </summary>
        /// <param name="d">The movement amount</param>
        public void Move(Vertex3f d)
        {
            position += d;
            delta += d;
        }

        protected bool CollideX(IEnumerable<AABB> others)
        {
            bool hasCollided = false;

            position.x += delta.x;
            foreach (AABB other in others)
                if (delta.x != 0
                    && other.position.y - Size.y < position.y && position.y < other.position.y + other.Size.y
                    && other.position.z - Size.z < position.z && position.z < other.position.z + other.Size.z
                   )
                    if (delta.x > 0
                        && lastPosition.x + Size.x <= other.position.x
                        && other.position.x < position.x + Size.x
                       )
                    {
                        position.x = other.position.x - Size.x;
                        hasCollided = true;
                    }
                    else if (
                        position.x < other.position.x + other.Size.x
                        && other.position.x + other.Size.x <= lastPosition.x
                       )
                    {
                        position.x = other.position.x + other.Size.x;
                        hasCollided = true;
                    }

            return hasCollided;
        }

        protected bool CollideY(IEnumerable<AABB> others)
        {
            bool hasCollided = false;

            position.y += delta.y;
            foreach (AABB other in others)
                if (delta.y != 0
                    && other.position.x - Size.x < position.x && position.x < other.position.x + other.Size.x
                    && other.position.z - Size.z < position.z && position.z < other.position.z + other.Size.z
                   )
                    if (delta.y > 0
                        && lastPosition.y + Size.y <= other.position.y
                        && other.position.y < position.y + Size.y
                       )
                    {
                        position.y = other.position.y - Size.y;
                        hasCollided = true;
                    }
                    else if (
                        position.y < other.position.y + other.Size.y
                        && other.position.y + other.Size.y <= lastPosition.y
                       )
                    {
                        position.y = other.position.y + other.Size.y;
                        hasCollided = true;
                    }

            return hasCollided;
        }

        protected bool CollideZ(IEnumerable<AABB> others)
        {
            bool hasCollided = false;

            position.z += delta.z;
            foreach (AABB other in others)
                if (delta.z != 0
                    && other.position.x - Size.x < position.x && position.x < other.position.x + other.Size.x
                    && other.position.y - Size.y < position.y && position.y < other.position.y + other.Size.y
                   )
                    if (delta.z > 0
                        && lastPosition.z + Size.z <= other.position.z
                        && other.position.z < position.z + Size.z
                       )
                    {
                        position.z = other.position.z - Size.z;
                        hasCollided = true;
                    }
                    else if (
                        position.z < other.position.z + other.Size.z
                        && other.position.z + other.Size.z <= lastPosition.z
                       )
                    {
                        position.z = other.position.z + other.Size.z;
                        hasCollided = true;
                    }

            return hasCollided;
        }

        protected void Start()
        {
            position = lastPosition;
        }

        protected void End()
        {
            lastPosition = position;
            delta = new Vertex3f();
        }

        public void Collide(IEnumerable<AABB> others)
        {
            Start();

            CollideX(others);
            CollideY(others);
            CollideZ(others);

            End();
        }
    }
}