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
            ref Vector2 position,
            ref Vector2 size,
            ref Vector4 textureCoords,
            Color color,
            float rotation,
            ref Vector2 origin,
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
                position.X -= origin.X;
                position.Y -= origin.Y;

                vertexTL.Position.X = position.X;
                vertexTL.Position.Y = position.Y;

                vertexTR.Position.X = position.X + size.X;
                vertexTR.Position.Y = position.Y;

                vertexBL.Position.X = position.X;
                vertexBL.Position.Y = position.Y + size.Y;

                vertexBR.Position.X = position.X + size.X;
                vertexBR.Position.Y = position.Y + size.Y;
            }
            else
            {
                var sin = (float)Math.Sin(rotation);
                var cos = (float)Math.Cos(rotation);

                if (origin != Vector2.Zero)
                {
                    position.X -= origin.X * cos - origin.Y * sin;
                    position.Y -= origin.X * sin + origin.Y * cos;
                }

                vertexTL.Position.X = position.X;
                vertexTL.Position.Y = position.Y;

                vertexTR.Position.X = position.X + size.X * cos;
                vertexTR.Position.Y = position.Y + size.X * sin;

                vertexBL.Position.X = position.X - size.Y * sin;
                vertexBL.Position.Y = position.Y + size.Y * cos;

                vertexBR.Position.X = position.X + size.X * cos - size.Y * sin;
                vertexBR.Position.Y = position.Y + size.X * sin + size.Y * cos;
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

