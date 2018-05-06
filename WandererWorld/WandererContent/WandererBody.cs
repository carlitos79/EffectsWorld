using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;

namespace WandererWorld.WandererContent
{
    public class WandererBody : WandererLimbs
    {
        private List<IWanderer> children = new List<IWanderer>();

        private Game game;
        private Matrix limbWorld;

        private Vector3 parentPosition;
        private Vector3 parentRotation = Vector3.Zero;
        private Vector3 jointPosition = new Vector3(0, 0, 0);

        public WandererBody(Game game) : base(game, "torso")
        {
            LoadContent();
            this.game = game;

            scale = new Vector3(3, 5, 3);
            parentPosition = new Vector3(0, 1, 0);

            children.Add(new Head(game, new Vector3(0, 4.8f, 0)));
            children.Add(new RightArm(game, new Vector3(2.2f, 1.5f, 0)));
            children.Add(new LeftArm(game, new Vector3(-2.2f, 1.5f, 0)));
            children.Add(new RightLeg(game, new Vector3(0.8f, -3.8f, 0)));
            children.Add(new LeftLeg(game, new Vector3(-0.8f, -3.8f, 0)));
        }

        public override void DrawLimb(GameTime gameTime, Matrix world)
        {
            limbWorld = Matrix.CreateScale(scale) * World * Matrix.CreateTranslation(parentPosition);
            Effect.World = limbWorld * world;
            Effect.Texture = texture;

            game.GraphicsDevice.SetVertexBuffer(vertexBuffer);
            game.GraphicsDevice.Indices = indexBuffer;

            foreach (EffectPass effect in Effect.CurrentTechnique.Passes)
            {
                effect.Apply();
                game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
            }

            // Here is where all the children are drawn
            foreach (IWanderer child in children)
            {
                child.DrawLimb(gameTime, World * world);
            }
        }

        public override void UpdateLimbMovement(GameTime gameTime)
        {
            // Here is where the parent and the children sets in motion.
            if (Keyboard.GetState().IsKeyDown(Keys.Right))
            {
                parentRotation = new Vector3(parentRotation.X - 0.05f, parentRotation.Y, parentRotation.Z);

                if (parentRotation.X <= -1.55f)
                {
                    parentRotation.X += 0.05f;
                }
            }      

            if (Keyboard.GetState().IsKeyDown(Keys.Left))
            {
                parentRotation = new Vector3(parentRotation.X + 0.05f, parentRotation.Y, parentRotation.Z);

                if (parentRotation.X >= 1.55f)
                {
                    parentRotation.X -= 0.05f;
                }
            }

            World = Matrix.Identity *
                Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(parentRotation.X, parentRotation.Y, parentRotation.Z)) *
                Matrix.CreateTranslation(parentPosition);

            // Here is where all the children are updated
            foreach (IWanderer child in children)
            {
                child.UpdateLimbMovement(gameTime);
            }
        }
    }
}
