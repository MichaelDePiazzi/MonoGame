// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Xna.Framework.Graphics
{
    public class SpriteBatchItem : IComparable<SpriteBatchItem>
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
		
		public void Set ( float x, float y, float dx, float dy, float w, float h, float sin, float cos, Color color, Vector2 texCoordTL, Vector2 texCoordBR, float depth )
		{
            // TODO, Should we be just assigning the Depth Value to Z?
            // According to http://blogs.msdn.com/b/shawnhar/archive/2011/01/12/spritebatch-billboards-in-a-3d-world.aspx
            // We do.
			vertexTL.Position.X = x+dx*cos-dy*sin;
            vertexTL.Position.Y = y+dx*sin+dy*cos;
            vertexTL.Position.Z = depth;
            vertexTL.Color = color;
            vertexTL.TextureCoordinate.X = texCoordTL.X;
            vertexTL.TextureCoordinate.Y = texCoordTL.Y;

			vertexTR.Position.X = x+(dx+w)*cos-dy*sin;
            vertexTR.Position.Y = y+(dx+w)*sin+dy*cos;
            vertexTR.Position.Z = depth;
            vertexTR.Color = color;
            vertexTR.TextureCoordinate.X = texCoordBR.X;
            vertexTR.TextureCoordinate.Y = texCoordTL.Y;

			vertexBL.Position.X = x+dx*cos-(dy+h)*sin;
            vertexBL.Position.Y = y+dx*sin+(dy+h)*cos;
            vertexBL.Position.Z = depth;
            vertexBL.Color = color;
            vertexBL.TextureCoordinate.X = texCoordTL.X;
            vertexBL.TextureCoordinate.Y = texCoordBR.Y;

			vertexBR.Position.X = x+(dx+w)*cos-(dy+h)*sin;
            vertexBR.Position.Y = y+(dx+w)*sin+(dy+h)*cos;
            vertexBR.Position.Z = depth;
            vertexBR.Color = color;
            vertexBR.TextureCoordinate.X = texCoordBR.X;
            vertexBR.TextureCoordinate.Y = texCoordBR.Y;
		}

        public void Set(float x, float y, float w, float h, Color color, Vector2 texCoordTL, Vector2 texCoordBR, float depth)
        {
            vertexTL.Position.X = x;
            vertexTL.Position.Y = y;
            vertexTL.Position.Z = depth;
            vertexTL.Color = color;
            vertexTL.TextureCoordinate.X = texCoordTL.X;
            vertexTL.TextureCoordinate.Y = texCoordTL.Y;

            vertexTR.Position.X = x + w;
            vertexTR.Position.Y = y;
            vertexTR.Position.Z = depth;
            vertexTR.Color = color;
            vertexTR.TextureCoordinate.X = texCoordBR.X;
            vertexTR.TextureCoordinate.Y = texCoordTL.Y;

            vertexBL.Position.X = x;
            vertexBL.Position.Y = y + h;
            vertexBL.Position.Z = depth;
            vertexBL.Color = color;
            vertexBL.TextureCoordinate.X = texCoordTL.X;
            vertexBL.TextureCoordinate.Y = texCoordBR.Y;

            vertexBR.Position.X = x + w;
            vertexBR.Position.Y = y + h;
            vertexBR.Position.Z = depth;
            vertexBR.Color = color;
            vertexBR.TextureCoordinate.X = texCoordBR.X;
            vertexBR.TextureCoordinate.Y = texCoordBR.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRotatedPosition(Vector2 position, Vector2 size, float sin, float cos)
        {
            vertexTL.Position.X = position.X;
            vertexTL.Position.Y = position.Y;

            vertexTR.Position.X = position.X + size.X * cos;
            vertexTR.Position.Y = position.Y + size.X * sin;

            vertexBL.Position.X = position.X - size.Y * sin;
            vertexBL.Position.Y = position.Y + size.Y * cos;

            vertexBR.Position.X = position.X + size.X * cos - size.Y * sin;
            vertexBR.Position.Y = position.Y + size.X * sin + size.Y * cos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPosition(Vector2 position, Vector2 size)
	    {
            vertexTL.Position.X = position.X;
            vertexTL.Position.Y = position.Y;

            vertexTR.Position.X = position.X + size.X;
            vertexTR.Position.Y = position.Y;

            vertexBL.Position.X = position.X;
            vertexBL.Position.Y = position.Y + size.Y;

            vertexBR.Position.X = position.X + size.X;
            vertexBR.Position.Y = position.Y + size.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPosition(Vector2 position, float w, float h)
        {
            vertexTL.Position.X = position.X;
            vertexTL.Position.Y = position.Y;

            vertexTR.Position.X = position.X + w;
            vertexTR.Position.Y = position.Y;

            vertexBL.Position.X = position.X;
            vertexBL.Position.Y = position.Y + h;

            vertexBR.Position.X = position.X + w;
            vertexBR.Position.Y = position.Y + h;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public void SetMisc(Color color, Vector2 texCoordTL, Vector2 texCoordBR, float depth)
	    {
            vertexTL.Position.Z = depth;
            vertexTR.Position.Z = depth;
            vertexBL.Position.Z = depth;
            vertexBR.Position.Z = depth;

            vertexTL.Color = color;
            vertexTR.Color = color;
            vertexBL.Color = color;
            vertexBR.Color = color;

            vertexTL.TextureCoordinate.X = texCoordTL.X;
            vertexTL.TextureCoordinate.Y = texCoordTL.Y;

            vertexTR.TextureCoordinate.X = texCoordBR.X;
            vertexTR.TextureCoordinate.Y = texCoordTL.Y;

            vertexBL.TextureCoordinate.X = texCoordTL.X;
            vertexBL.TextureCoordinate.Y = texCoordBR.Y;

            vertexBR.TextureCoordinate.X = texCoordBR.X;
            vertexBR.TextureCoordinate.Y = texCoordBR.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDefaultDepth()
        {
            vertexTL.Position.Z = 0f;
            vertexTR.Position.Z = 0f;
            vertexBL.Position.Z = 0f;
            vertexBR.Position.Z = 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public void SetDepth(float depth)
	    {
            vertexTL.Position.Z = depth;
            vertexTR.Position.Z = depth;
            vertexBL.Position.Z = depth;
            vertexBR.Position.Z = depth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDefaultColor()
        {
            vertexTL.Color = Color.White;
            vertexTR.Color = Color.White;
            vertexBL.Color = Color.White;
            vertexBR.Color = Color.White;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetColor(Color color)
        {
            vertexTL.Color = color;
            vertexTR.Color = color;
            vertexBL.Color = color;
            vertexBR.Color = color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFullTextureCoords()
        {
            vertexTL.TextureCoordinate.X = 0f;
            vertexTL.TextureCoordinate.Y = 0f;

            vertexTR.TextureCoordinate.X = 1f;
            vertexTR.TextureCoordinate.Y = 0f;

            vertexBL.TextureCoordinate.X = 0f;
            vertexBL.TextureCoordinate.Y = 1f;

            vertexBR.TextureCoordinate.X = 1f;
            vertexBR.TextureCoordinate.Y = 1f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public void SetTextureCoords(Vector2 texCoordTL, Vector2 texCoordBR)
	    {
            vertexTL.TextureCoordinate.X = texCoordTL.X;
            vertexTL.TextureCoordinate.Y = texCoordTL.Y;

            vertexTR.TextureCoordinate.X = texCoordBR.X;
            vertexTR.TextureCoordinate.Y = texCoordTL.Y;

            vertexBL.TextureCoordinate.X = texCoordTL.X;
            vertexBL.TextureCoordinate.Y = texCoordBR.Y;

            vertexBR.TextureCoordinate.X = texCoordBR.X;
            vertexBR.TextureCoordinate.Y = texCoordBR.Y;
        }

        #region Implement IComparable
        public int CompareTo(SpriteBatchItem other)
        {
            return SortKey.CompareTo(other.SortKey);
        }
        #endregion
    }
}

