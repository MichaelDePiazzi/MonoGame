// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    internal class SpriteBatchItem : IComparable<SpriteBatchItem>
	{
		public Texture2D Texture;
        public float SortKey;

        public VertexPositionColorTexture vertexTL;
		public VertexPositionColorTexture vertexTR;
		public VertexPositionColorTexture vertexBL;
		public VertexPositionColorTexture vertexBR;
		public SpriteBatchItem ()
		{
			vertexTL = new VertexPositionColorTexture();
            vertexTR = new VertexPositionColorTexture();
            vertexBL = new VertexPositionColorTexture();
            vertexBR = new VertexPositionColorTexture();            
		}

        public void Set(Texture2D texture,
            Vector4 destinationRectangle,
            Vector4 textureCoords,
            Color color,
            float rotation,
            Vector2 origin,
            SpriteEffects effect,
            float depth,
            SpriteSortMode sortMode)
        {
            Texture = texture;

            // set SortKey based on SpriteSortMode.
            switch (sortMode)
            {
                case SpriteSortMode.Deferred:
                case SpriteSortMode.Immediate:
                    break;
                // Comparison of Texture objects.
                case SpriteSortMode.Texture:
                    SortKey = texture.SortingKey;
                    break;
                // Comparison of Depth
                case SpriteSortMode.FrontToBack:
                    SortKey = depth;
                    break;
                // Comparison of Depth in reverse
                case SpriteSortMode.BackToFront:
                    SortKey = -depth;
                    break;
            }

            if ((effect & (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically)) != 0)
            {
                if ((effect & SpriteEffects.FlipHorizontally) != 0)
                {
                    var temp = textureCoords.Z;
                    textureCoords.Z = textureCoords.X;
                    textureCoords.X = temp;
                }
                if ((effect & SpriteEffects.FlipVertically) != 0)
                {
                    var temp = textureCoords.W;
                    textureCoords.W = textureCoords.Y;
                    textureCoords.Y = temp;
                }
            }

            if (rotation == 0f)
            {
                destinationRectangle.X -= origin.X;
                destinationRectangle.Y -= origin.Y;

                vertexTL.Position.X = destinationRectangle.X;
                vertexTL.Position.Y = destinationRectangle.Y;

                vertexTR.Position.X = destinationRectangle.X + destinationRectangle.Z;
                vertexTR.Position.Y = destinationRectangle.Y;

                vertexBL.Position.X = destinationRectangle.X;
                vertexBL.Position.Y = destinationRectangle.Y + destinationRectangle.W;

                vertexBR.Position.X = destinationRectangle.X + destinationRectangle.Z;
                vertexBR.Position.Y = destinationRectangle.Y + destinationRectangle.W;
            }
            else
            {
                var sin = (float)Math.Sin(rotation);
                var cos = (float)Math.Cos(rotation);

                if (origin != Vector2.Zero)
                {
                    destinationRectangle.X -= origin.X * cos - origin.Y * sin;
                    destinationRectangle.Y -= origin.X * sin + origin.Y * cos;
                }

                vertexTL.Position.X = destinationRectangle.X;
                vertexTL.Position.Y = destinationRectangle.Y;

                vertexTR.Position.X = destinationRectangle.X + destinationRectangle.Z * cos;
                vertexTR.Position.Y = destinationRectangle.Y + destinationRectangle.Z * sin;

                vertexBL.Position.X = destinationRectangle.X - destinationRectangle.W * sin;
                vertexBL.Position.Y = destinationRectangle.Y + destinationRectangle.W * cos;

                vertexBR.Position.X = destinationRectangle.X + destinationRectangle.Z * cos - destinationRectangle.W * sin;
                vertexBR.Position.Y = destinationRectangle.Y + destinationRectangle.Z * sin + destinationRectangle.W * cos;
            }

            // According to http://blogs.msdn.com/b/shawnhar/archive/2011/01/12/spritebatch-billboards-in-a-3d-world.aspx
            // the "depth" value is assigned to Z

            vertexTL.Position.Z = depth;
            vertexTL.Color = color;
            vertexTL.TextureCoordinate.X = textureCoords.X;
            vertexTL.TextureCoordinate.Y = textureCoords.Y;

            vertexTR.Position.Z = depth;
            vertexTR.Color = color;
            vertexTR.TextureCoordinate.X = textureCoords.Z;
            vertexTR.TextureCoordinate.Y = textureCoords.Y;

            vertexBL.Position.Z = depth;
            vertexBL.Color = color;
            vertexBL.TextureCoordinate.X = textureCoords.X;
            vertexBL.TextureCoordinate.Y = textureCoords.W;

            vertexBR.Position.Z = depth;
            vertexBR.Color = color;
            vertexBR.TextureCoordinate.X = textureCoords.Z;
            vertexBR.TextureCoordinate.Y = textureCoords.W;
        }

        #region Implement IComparable
        public int CompareTo(SpriteBatchItem other)
        {
            return SortKey.CompareTo(other.SortKey);
        }
        #endregion
    }
}

