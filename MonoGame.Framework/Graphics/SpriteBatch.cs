// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Helper class for drawing text strings and sprites in one or more optimized batches.
    /// </summary>
	public class SpriteBatch : GraphicsResource
	{
        #region Private Fields
        readonly SpriteBatcher _batcher;

		SpriteSortMode _sortMode;
		BlendState _blendState;
		SamplerState _samplerState;
		DepthStencilState _depthStencilState; 
		RasterizerState _rasterizerState;		
		Effect _effect;
        bool _beginCalled;

		Effect _spriteEffect;
	    readonly EffectParameter _matrixTransform;
        readonly EffectPass _spritePass;

		Matrix _matrix;
		Rectangle _tempRect = new Rectangle (0,0,0,0);
		Vector2 _texCoordTL = new Vector2 (0,0);
		Vector2 _texCoordBR = new Vector2 (0,0);
        #endregion

        internal static bool NeedsHalfPixelOffset;

        /// <summary>
        /// Constructs a <see cref="SpriteBatch"/>.
        /// </summary>
        /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/>, which will be used for sprite rendering.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="graphicsDevice"/> is null.</exception>
        public SpriteBatch (GraphicsDevice graphicsDevice)
		{
			if (graphicsDevice == null)
            {
				throw new ArgumentNullException ("graphicsDevice", FrameworkResources.ResourceCreationWhenDeviceIsNull);
			}	

			this.GraphicsDevice = graphicsDevice;

            // Use a custom SpriteEffect so we can control the transformation matrix
            _spriteEffect = new Effect(graphicsDevice, EffectResource.SpriteEffect.Bytecode);
            _matrixTransform = _spriteEffect.Parameters["MatrixTransform"];
            _spritePass = _spriteEffect.CurrentTechnique.Passes[0];

            _batcher = new SpriteBatcher(graphicsDevice);

            _beginCalled = false;
		}

        /// <summary>
        /// Begins a new sprite and text batch with the specified render state.
        /// </summary>
        /// <param name="sortMode">The drawing order for sprite and text drawing. <see cref="SpriteSortMode.Deferred"/> by default.</param>
        /// <param name="blendState">State of the blending. Uses <see cref="BlendState.AlphaBlend"/> if null.</param>
        /// <param name="samplerState">State of the sampler. Uses <see cref="SamplerState.LinearClamp"/> if null.</param>
        /// <param name="depthStencilState">State of the depth-stencil buffer. Uses <see cref="DepthStencilState.None"/> if null.</param>
        /// <param name="rasterizerState">State of the rasterization. Uses <see cref="RasterizerState.CullCounterClockwise"/> if null.</param>
        /// <param name="effect">A custom <see cref="Effect"/> to override the default sprite effect. Uses default sprite effect if null.</param>
        /// <param name="transformMatrix">An optional matrix used to transform the sprite geometry. Uses <see cref="Matrix.Identity"/> if null.</param>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Begin"/> is called next time without previous <see cref="End"/>.</exception>
        /// <remarks>This method uses optional parameters.</remarks>
        /// <remarks>The <see cref="Begin"/> Begin should be called before drawing commands, and you cannot call it again before subsequent <see cref="End"/>.</remarks>
        public void Begin
        (
             SpriteSortMode sortMode = SpriteSortMode.Deferred,
             BlendState blendState = null,
             SamplerState samplerState = null,
             DepthStencilState depthStencilState = null,
             RasterizerState rasterizerState = null,
             Effect effect = null,
             Matrix? transformMatrix = null
        )
        {
            if (_beginCalled)
                throw new InvalidOperationException("Begin cannot be called again until End has been successfully called.");

            // defaults
            _sortMode = sortMode;
            _blendState = blendState ?? BlendState.AlphaBlend;
            _samplerState = samplerState ?? SamplerState.LinearClamp;
            _depthStencilState = depthStencilState ?? DepthStencilState.None;
            _rasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;
            _effect = effect;
            _matrix = transformMatrix ?? Matrix.Identity;

            // Setup things now so a user can change them.
            if (sortMode == SpriteSortMode.Immediate)
            {
                Setup();
            }

            _beginCalled = true;
        }

        /// <summary>
        /// Flushes all batched text and sprites to the screen.
        /// </summary>
        /// <remarks>This command should be called after <see cref="Begin"/> and drawing commands.</remarks>
		public void End ()
		{	
			_beginCalled = false;

			if (_sortMode != SpriteSortMode.Immediate)
				Setup();
            
            _batcher.DrawBatch(_sortMode, _effect);
        }
		
		void Setup() 
        {
            var gd = GraphicsDevice;
			gd.BlendState = _blendState;
			gd.DepthStencilState = _depthStencilState;
			gd.RasterizerState = _rasterizerState;
			gd.SamplerStates[0] = _samplerState;
			
            // Setup the default sprite effect.
			var vp = gd.Viewport;

		    Matrix projection;

            // Normal 3D cameras look into the -z direction (z = 1 is in font of z = 0). The
            // sprite batch layer depth is the opposite (z = 0 is in front of z = 1).
            // --> We get the correct matrix with near plane 0 and far plane -1.
            Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, -1, out projection);

            // Some platforms require a half pixel offset to match DX.
            if (NeedsHalfPixelOffset)
            {
                projection.M41 += -0.5f * projection.M11;
                projection.M42 += -0.5f * projection.M22;
            }

            Matrix.Multiply(ref _matrix, ref projection, out projection);

            _matrixTransform.SetValue(projection);
            _spritePass.Apply();
		}
		
        void CheckValid(Texture2D texture)
        {
            if (texture == null)
                throw new ArgumentNullException("texture");
            if (!_beginCalled)
                throw new InvalidOperationException("Draw was called, but Begin has not yet been called. Begin must be called successfully before you can call Draw.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckValidOpt(Texture2D texture)
        {
            if (texture == null)
                throw new ArgumentNullException("texture");
            if (!_beginCalled)
                throw new InvalidOperationException("Draw was called, but Begin has not yet been called. Begin must be called successfully before you can call Draw.");
        }

        void CheckValid(SpriteFont spriteFont, string text)
        {
            if (spriteFont == null)
                throw new ArgumentNullException("spriteFont");
            if (text == null)
                throw new ArgumentNullException("text");
            if (!_beginCalled)
                throw new InvalidOperationException("DrawString was called, but Begin has not yet been called. Begin must be called successfully before you can call DrawString.");
        }

        void CheckValid(SpriteFont spriteFont, StringBuilder text)
        {
            if (spriteFont == null)
                throw new ArgumentNullException("spriteFont");
            if (text == null)
                throw new ArgumentNullException("text");
            if (!_beginCalled)
                throw new InvalidOperationException("DrawString was called, but Begin has not yet been called. Begin must be called successfully before you can call DrawString.");
        }

        /// <summary>
        /// Submit a sprite for drawing in the current batch.
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The drawing location on screen or null if <paramref name="destinationRectangle"> is used.</paramref></param>
        /// <param name="destinationRectangle">The drawing bounds on screen or null if <paramref name="position"> is used.</paramref></param>
        /// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
        /// <param name="origin">An optional center of rotation. Uses <see cref="Vector2.Zero"/> if null.</param>
        /// <param name="rotation">An optional rotation of this sprite. 0 by default.</param>
        /// <param name="scale">An optional scale vector. Uses <see cref="Vector2.One"/> if null.</param>
        /// <param name="color">An optional color mask. Uses <see cref="Color.White"/> if null.</param>
        /// <param name="effects">The optional drawing modificators. <see cref="SpriteEffects.None"/> by default.</param>
        /// <param name="layerDepth">An optional depth of the layer of this sprite. 0 by default.</param>
        /// <exception cref="InvalidOperationException">Throwns if both <paramref name="position"/> and <paramref name="destinationRectangle"/> been used.</exception>
        /// <remarks>This overload uses optional parameters. This overload requires only one of <paramref name="position"/> and <paramref name="destinationRectangle"/> been used.</remarks>
        public void Draw (Texture2D texture,
                Vector2? position = null,
                Rectangle? destinationRectangle = null,
                Rectangle? sourceRectangle = null,
                Vector2? origin = null,
                float rotation = 0f,
                Vector2? scale = null,
                Color? color = null,
                SpriteEffects effects = SpriteEffects.None,
                float layerDepth = 0f)
        {

            // Assign default values to null parameters here, as they are not compile-time constants
            if(!color.HasValue)
                color = Color.White;
            if(!origin.HasValue)
                origin = Vector2.Zero;
            if(!scale.HasValue)
                scale = Vector2.One;

            // If both drawRectangle and position are null, or if both have been assigned a value, raise an error
            if((destinationRectangle.HasValue) == (position.HasValue))
            {
                throw new InvalidOperationException("Expected drawRectangle or position, but received neither or both.");
            }
            else if(position != null)
            {
                // Call Draw() using position
                Draw(texture, (Vector2)position, sourceRectangle, (Color)color, rotation, (Vector2)origin, (Vector2)scale, effects, layerDepth);
            }
            else
            {
                // Call Draw() using drawRectangle
                Draw(texture, (Rectangle)destinationRectangle, sourceRectangle, (Color)color, rotation, (Vector2)origin, effects, layerDepth);
            }
        }

        public void DrawOpt(Texture2D texture,
                Vector2? position = null,
                Rectangle? destinationRectangle = null,
                Rectangle? sourceRectangle = null,
                Vector2? origin = null,
                float rotation = 0f,
                Vector2? scale = null,
                Color? color = null,
                SpriteEffects effects = SpriteEffects.None,
                float layerDepth = 0f)
        {
            CheckValidOpt(texture);

            // If both drawRectangle and position are null, or if both have been assigned a value, raise an error
            if (destinationRectangle.HasValue == position.HasValue)
                throw new InvalidOperationException("Expected drawRectangle or position, but received neither or both.");

            var item = _batcher.CreateBatchItem();

            if (position.HasValue)
            {
                if (sourceRectangle.HasValue)
                    SetPartialTexture(texture, sourceRectangle.GetValueOrDefault());
                else
                    SetFullTexture(texture);
                ApplyFlip(effects);

                if (origin.HasValue)
                {
                    _origin = origin.GetValueOrDefault();
                    if (scale.HasValue)
                        ApplyScale(scale.GetValueOrDefault());
                    if (rotation == 0f)
                    {
                        SetPosition(position.GetValueOrDefault());
                        item.SetPosition(_position, _size);
                    }
                    else
                    {
                        var sin = (float)Math.Sin(rotation);
                        var cos = (float)Math.Cos(rotation);
                        SetRotatedPosition(position.GetValueOrDefault(), sin, cos);
                        item.SetRotatedPosition(_position, _size, sin, cos);
                    }
                }
                else
                {
                    _origin = Vector2.Zero;
                    if (scale.HasValue)
                        ApplyScaleToSizeOnly(scale.GetValueOrDefault());
                    if (rotation == 0f)
                    {
                        item.SetPosition(position.GetValueOrDefault(), _size);
                    }
                    else
                    {
                        var sin = (float)Math.Sin(rotation);
                        var cos = (float)Math.Cos(rotation);
                        item.SetRotatedPosition(position.GetValueOrDefault(), _size, sin, cos);
                    }
                }
            }
            else
            {
                if (sourceRectangle.HasValue)
                    SetPartialTextureCoordsOnly(texture, sourceRectangle.GetValueOrDefault());
                else
                    SetFullTextureCoordsOnly();
                ApplyFlip(effects);

                var destRectValue = destinationRectangle.GetValueOrDefault();
                _size.X = destRectValue.Width;
                _size.Y = destRectValue.Height;

                if (origin.HasValue)
                {
                    _origin = origin.GetValueOrDefault();
                    _origin.X *= (_size.X / ((sourceRectangle.HasValue && sourceRectangle.Value.Width != 0) ? sourceRectangle.Value.Width : texture.Width));
                    _origin.Y *= (_size.Y / ((sourceRectangle.HasValue && sourceRectangle.Value.Height != 0) ? sourceRectangle.Value.Height : texture.Height));

                    if (rotation == 0f)
                    {
                        SetPosition(new Vector2(destRectValue.X, destRectValue.Y));
                        item.SetPosition(_position, _size);
                    }
                    else
                    {
                        var sin = (float)Math.Sin(rotation);
                        var cos = (float)Math.Cos(rotation);
                        SetRotatedPosition(new Vector2(destRectValue.X, destRectValue.Y), sin, cos);
                        item.SetRotatedPosition(_position, _size, sin, cos);
                    }
                }
                else
                {
                    if (rotation == 0f)
                    {
                        item.SetPosition(new Vector2(destRectValue.X, destRectValue.Y), _size);
                    }
                    else
                    {
                        var sin = (float)Math.Sin(rotation);
                        var cos = (float)Math.Cos(rotation);
                        item.SetRotatedPosition(new Vector2(destRectValue.X, destRectValue.Y), _size, sin, cos);
                    }
                }
            }
            item.SetDepth(layerDepth);
            if (color.HasValue)
                item.SetColor(color.GetValueOrDefault());
            else
                item.SetDefaultColor();
            item.SetTextureCoords(_texCoordTL, _texCoordBR);

            FlushIfNeeded();
        }

        /// <summary>
        /// Submit a sprite for drawing in the current batch.
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
        /// <param name="color">A color mask.</param>
        /// <param name="rotation">A rotation of this sprite.</param>
        /// <param name="origin">Center of the rotation. 0,0 by default.</param>
        /// <param name="scale">A scaling of this sprite.</param>
        /// <param name="effects">Modificators for drawing. Can be combined.</param>
        /// <param name="layerDepth">A depth of the layer of this sprite.</param>
		public void Draw (Texture2D texture,
				Vector2 position,
				Rectangle? sourceRectangle,
				Color color,
				float rotation,
				Vector2 origin,
				Vector2 scale,
				SpriteEffects effects,
                float layerDepth)
		{
            CheckValid(texture);

            var w = texture.Width * scale.X;
            var h = texture.Height * scale.Y;
			if (sourceRectangle.HasValue)
            {
				w = sourceRectangle.Value.Width*scale.X;
				h = sourceRectangle.Value.Height*scale.Y;
			}

            DrawInternal(texture,
				new Vector4(position.X, position.Y, w, h),
				sourceRectangle,
				color,
				rotation,
				origin * scale,
				effects,
                layerDepth,
				true);
		}

        public void DrawOpt(Texture2D texture,
                Vector2 position,
                Rectangle? sourceRectangle,
                Color color,
                float rotation,
                Vector2 origin,
                Vector2 scale,
                SpriteEffects effects,
                float layerDepth)
        {
            CheckValid(texture);

            if (sourceRectangle.HasValue)
                SetPartialTexture(texture, sourceRectangle.GetValueOrDefault());
            else
                SetFullTexture(texture);
            _origin = origin;
            ApplyScale(scale);
            ApplyFlip(effects);

            var item = _batcher.CreateBatchItem();
            if (rotation == 0f)
            {
                SetPosition(position);
                item.SetPosition(_position, _size);
            }
            else
            {
                var sin = (float)Math.Sin(rotation);
                var cos = (float)Math.Cos(rotation);
                SetRotatedPosition(position, sin, cos);
                item.SetRotatedPosition(_position, _size, sin, cos);
            }
            item.SetDepth(layerDepth);
            item.SetColor(color);
            item.SetTextureCoords(_texCoordTL, _texCoordBR);

            FlushIfNeeded();
        }

        /// <summary>
        /// Submit a sprite for drawing in the current batch.
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
        /// <param name="color">A color mask.</param>
        /// <param name="rotation">A rotation of this sprite.</param>
        /// <param name="origin">Center of the rotation. 0,0 by default.</param>
        /// <param name="scale">A scaling of this sprite.</param>
        /// <param name="effects">Modificators for drawing. Can be combined.</param>
        /// <param name="layerDepth">A depth of the layer of this sprite.</param>
		public void Draw (Texture2D texture,
				Vector2 position,
				Rectangle? sourceRectangle,
				Color color,
				float rotation,
				Vector2 origin,
				float scale,
				SpriteEffects effects,
                float layerDepth)
		{
            CheckValid(texture);

            var w = texture.Width * scale;
            var h = texture.Height * scale;
            if (sourceRectangle.HasValue)
            {
                w = sourceRectangle.Value.Width * scale;
                h = sourceRectangle.Value.Height * scale;
            }

            DrawInternal(texture,
                new Vector4(position.X, position.Y, w, h),
				sourceRectangle,
				color,
				rotation,
				origin * scale,
				effects,
                layerDepth,
				true);
		}

        /// <summary>
        /// Submit a sprite for drawing in the current batch.
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="destinationRectangle">The drawing bounds on screen.</param>
        /// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
        /// <param name="color">A color mask.</param>
        /// <param name="rotation">A rotation of this sprite.</param>
        /// <param name="origin">Center of the rotation. 0,0 by default.</param>
        /// <param name="effects">Modificators for drawing. Can be combined.</param>
        /// <param name="layerDepth">A depth of the layer of this sprite.</param>
		public void Draw (Texture2D texture,
			Rectangle destinationRectangle,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			SpriteEffects effects,
            float layerDepth)
		{
            CheckValid(texture);

            DrawInternal(texture,
			      new Vector4(destinationRectangle.X,
			                  destinationRectangle.Y,
			                  destinationRectangle.Width,
			                  destinationRectangle.Height),
			      sourceRectangle,
			      color,
			      rotation,
			      new Vector2(origin.X * ((float)destinationRectangle.Width / (float)( (sourceRectangle.HasValue && sourceRectangle.Value.Width != 0) ? sourceRectangle.Value.Width : texture.Width)),
                        			origin.Y * ((float)destinationRectangle.Height) / (float)( (sourceRectangle.HasValue && sourceRectangle.Value.Height != 0) ? sourceRectangle.Value.Height : texture.Height)),
			      effects,
                  layerDepth,
			      true);
		}

	    public void Draw(Texture2D texture, VertexPositionColorTexture[] vertices, float layerDepth = 0f)
	    {
            var item = _batcher.CreateBatchItem();

            item.Texture = texture;

            switch (_sortMode)
            {
                case SpriteSortMode.Texture:
                    item.SortKey = texture.SortingKey;
                    break;
                case SpriteSortMode.FrontToBack:
                    item.SortKey = layerDepth;
                    break;
                case SpriteSortMode.BackToFront:
                    item.SortKey = -layerDepth;
                    break;
            }

            item.vertexTL = vertices[0];
            item.vertexTR = vertices[1];
            item.vertexBL = vertices[2];
            item.vertexBR = vertices[3];

            FlushIfNeeded();
        }

		internal void DrawInternal (Texture2D texture,
			Vector4 destinationRectangle,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			SpriteEffects effect,
			float depth,
			bool autoFlush)
		{
			var item = _batcher.CreateBatchItem();

			item.Texture = texture;

            // set SortKey based on SpriteSortMode.
            switch ( _sortMode )
            {
                // Comparison of Texture objects.
                case SpriteSortMode.Texture:
                    item.SortKey = texture.SortingKey;
                    break;
                // Comparison of Depth
                case SpriteSortMode.FrontToBack:
                    item.SortKey = depth;
                    break;
                // Comparison of Depth in reverse
                case SpriteSortMode.BackToFront:
                    item.SortKey = -depth;
                    break;
            }

			if (sourceRectangle.HasValue)
            {
				_tempRect = sourceRectangle.Value;
                _texCoordTL.X = _tempRect.X / (float)texture.Width;
                _texCoordTL.Y = _tempRect.Y / (float)texture.Height;
                _texCoordBR.X = (_tempRect.X + _tempRect.Width) / (float)texture.Width;
                _texCoordBR.Y = (_tempRect.Y + _tempRect.Height) / (float)texture.Height;
            }
            else
            {
                _texCoordTL.X = 0f;
                _texCoordTL.Y = 0f;
                _texCoordBR.X = 1f;
                _texCoordBR.Y = 1f;
            }
            
			if ((effect & SpriteEffects.FlipVertically) != 0) {
                var temp = _texCoordBR.Y;
				_texCoordBR.Y = _texCoordTL.Y;
				_texCoordTL.Y = temp;
			}
			if ((effect & SpriteEffects.FlipHorizontally) != 0) {
                var temp = _texCoordBR.X;
				_texCoordBR.X = _texCoordTL.X;
				_texCoordTL.X = temp;
			}

		    if (rotation == 0f)
		    {
                item.Set(destinationRectangle.X - origin.X,
                        destinationRectangle.Y - origin.Y,
                        destinationRectangle.Z,
                        destinationRectangle.W,
                        color,
                        _texCoordTL,
                        _texCoordBR,
                        depth);
            }
            else
		    {
                item.Set(destinationRectangle.X,
                        destinationRectangle.Y,
                        -origin.X,
                        -origin.Y,
                        destinationRectangle.Z,
                        destinationRectangle.W,
                        (float)Math.Sin(rotation),
                        (float)Math.Cos(rotation),
                        color,
                        _texCoordTL,
                        _texCoordBR,
                        depth);
            }

			if (autoFlush)
			{
				FlushIfNeeded();
			}
		}

		// Mark the end of a draw operation for Immediate SpriteSortMode.
		internal void FlushIfNeeded()
		{
			if (_sortMode == SpriteSortMode.Immediate)
			{
				//_batcher.DrawBatch(_sortMode, _effect);
                _batcher.Reset();
			}
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FlushIfNeededOpt()
        {
            if (_sortMode == SpriteSortMode.Immediate)
                //_batcher.DrawBatch(_sortMode, _effect);
                _batcher.Reset();
        }

        /// <summary>
        /// Submit a sprite for drawing in the current batch.
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
        /// <param name="color">A color mask.</param>
		public void Draw (Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color)
		{
			Draw (texture, position, sourceRectangle, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
		}

        /// <summary>
        /// Submit a sprite for drawing in the current batch.
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="destinationRectangle">The drawing bounds on screen.</param>
        /// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
        /// <param name="color">A color mask.</param>
		public void Draw (Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
		{
			Draw (texture, destinationRectangle, sourceRectangle, color, 0, Vector2.Zero, SpriteEffects.None, 0f);
		}

        /// <summary>
        /// Submit a sprite for drawing in the current batch.
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="color">A color mask.</param>
        public void Draw (Texture2D texture, Vector2 position, Color color)
		{
			Draw (texture, position, null, color);
		}

        public void DrawOpt(Texture2D texture, Vector2 position, Color color)
        {
            CheckValidOpt(texture);

            SetFullTexture(texture);

            var item = _batcher.CreateBatchItem();
            item.Texture = texture;
            item.SortKey = (_sortMode == SpriteSortMode.Texture) ? texture.SortingKey : 0f;
            item.SetPosition(position, _size);
            item.SetMisc(color, _texCoordTL, _texCoordBR, 0f);

            FlushIfNeededOpt();
        }

        public void DrawOpt2(Texture2D texture, Vector2 position, Color color)
        {
            CheckValidOpt(texture);

            var item = _batcher.CreateBatchItem();
            item.Texture = texture;
            item.SortKey = (_sortMode == SpriteSortMode.Texture) ? texture.SortingKey : 0f;
            item.SetPosition(position, texture.width, texture.Height);
            item.SetDefaultDepth();
            item.SetColor(color);
            item.SetFullTextureCoords();

            FlushIfNeededOpt();
        }

        public void DrawOpt3(Texture2D texture, Vector2 position, Color color)
        {
            if (texture == null)
                throw new ArgumentNullException("texture");
            if (!_beginCalled)
                throw new InvalidOperationException("Draw was called, but Begin has not yet been called. Begin must be called successfully before you can call Draw.");

            var item = _batcher.CreateBatchItem();
            item.Texture = texture;
            item.SortKey = (_sortMode == SpriteSortMode.Texture) ? texture.SortingKey : 0f;

            item.vertexTL.Position.X = position.X;
            item.vertexTL.Position.Y = position.Y;

            float w = texture.width;
            item.vertexTR.Position.X = position.X + w;
            item.vertexTR.Position.Y = position.Y;

            item.vertexBL.Position.X = position.X;
            float h = texture.Height;
            item.vertexBL.Position.Y = position.Y + h;

            item.vertexBR.Position.X = position.X + w;
            item.vertexBR.Position.Y = position.Y + h;

            item.vertexTL.Position.Z = 0f;
            item.vertexTR.Position.Z = 0f;
            item.vertexBL.Position.Z = 0f;
            item.vertexBR.Position.Z = 0f;

            item.vertexTL.Color = color;
            item.vertexTR.Color = color;
            item.vertexBL.Color = color;
            item.vertexBR.Color = color;

            item.vertexTL.TextureCoordinate.X = 0f;
            item.vertexTL.TextureCoordinate.Y = 0f;

            item.vertexTR.TextureCoordinate.X = 1f;
            item.vertexTR.TextureCoordinate.Y = 0f;

            item.vertexBL.TextureCoordinate.X = 0f;
            item.vertexBL.TextureCoordinate.Y = 1f;

            item.vertexBR.TextureCoordinate.X = 1f;
            item.vertexBR.TextureCoordinate.Y = 1f;

            if (_sortMode == SpriteSortMode.Immediate)
                //_batcher.DrawBatch(_sortMode, _effect);
                _batcher.Reset();
        }

        /// <summary>
        /// Submit a sprite for drawing in the current batch.
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="destinationRectangle">The drawing bounds on screen.</param>
        /// <param name="color">A color mask.</param>
        public void Draw(Texture2D texture, Rectangle destinationRectangle, Color color)
		{
            Draw(texture, destinationRectangle, null, color);
		}

        /// <summary>
        /// Submit a text string of sprites for drawing in the current batch.
        /// </summary>
        /// <param name="spriteFont">A font.</param>
        /// <param name="text">The text which will be drawn.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="color">A color mask.</param>
		public void DrawString (SpriteFont spriteFont, string text, Vector2 position, Color color)
		{
            CheckValid(spriteFont, text);

            var source = new SpriteFont.CharacterSource(text);
			spriteFont.DrawInto (
                this, ref source, position, color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
		}

        /// <summary>
        /// Submit a text string of sprites for drawing in the current batch.
        /// </summary>
        /// <param name="spriteFont">A font.</param>
        /// <param name="text">The text which will be drawn.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="color">A color mask.</param>
        /// <param name="rotation">A rotation of this string.</param>
        /// <param name="origin">Center of the rotation. 0,0 by default.</param>
        /// <param name="scale">A scaling of this string.</param>
        /// <param name="effects">Modificators for drawing. Can be combined.</param>
        /// <param name="layerDepth">A depth of the layer of this string.</param>
		public void DrawString (
			SpriteFont spriteFont, string text, Vector2 position, Color color,
            float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
		{
            CheckValid(spriteFont, text);

			var scaleVec = new Vector2(scale, scale);
            var source = new SpriteFont.CharacterSource(text);
            spriteFont.DrawInto(this, ref source, position, color, rotation, origin, scaleVec, effects, layerDepth);
		}

        /// <summary>
        /// Submit a text string of sprites for drawing in the current batch.
        /// </summary>
        /// <param name="spriteFont">A font.</param>
        /// <param name="text">The text which will be drawn.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="color">A color mask.</param>
        /// <param name="rotation">A rotation of this string.</param>
        /// <param name="origin">Center of the rotation. 0,0 by default.</param>
        /// <param name="scale">A scaling of this string.</param>
        /// <param name="effects">Modificators for drawing. Can be combined.</param>
        /// <param name="layerDepth">A depth of the layer of this string.</param>
		public void DrawString (
			SpriteFont spriteFont, string text, Vector2 position, Color color,
            float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
		{
            CheckValid(spriteFont, text);

            var source = new SpriteFont.CharacterSource(text);
            spriteFont.DrawInto(this, ref source, position, color, rotation, origin, scale, effects, layerDepth);
		}

        /// <summary>
        /// Submit a text string of sprites for drawing in the current batch.
        /// </summary>
        /// <param name="spriteFont">A font.</param>
        /// <param name="text">The text which will be drawn.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="color">A color mask.</param>
		public void DrawString (SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color)
		{
            CheckValid(spriteFont, text);

            var source = new SpriteFont.CharacterSource(text);
			spriteFont.DrawInto(this, ref source, position, color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
		}

        /// <summary>
        /// Submit a text string of sprites for drawing in the current batch.
        /// </summary>
        /// <param name="spriteFont">A font.</param>
        /// <param name="text">The text which will be drawn.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="color">A color mask.</param>
        /// <param name="rotation">A rotation of this string.</param>
        /// <param name="origin">Center of the rotation. 0,0 by default.</param>
        /// <param name="scale">A scaling of this string.</param>
        /// <param name="effects">Modificators for drawing. Can be combined.</param>
        /// <param name="layerDepth">A depth of the layer of this string.</param>
		public void DrawString (
			SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color,
            float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
		{
            CheckValid(spriteFont, text);

			var scaleVec = new Vector2 (scale, scale);
            var source = new SpriteFont.CharacterSource(text);
            spriteFont.DrawInto(this, ref source, position, color, rotation, origin, scaleVec, effects, layerDepth);
		}

        /// <summary>
        /// Submit a text string of sprites for drawing in the current batch.
        /// </summary>
        /// <param name="spriteFont">A font.</param>
        /// <param name="text">The text which will be drawn.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="color">A color mask.</param>
        /// <param name="rotation">A rotation of this string.</param>
        /// <param name="origin">Center of the rotation. 0,0 by default.</param>
        /// <param name="scale">A scaling of this string.</param>
        /// <param name="effects">Modificators for drawing. Can be combined.</param>
        /// <param name="layerDepth">A depth of the layer of this string.</param>
		public void DrawString (
			SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color,
            float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
		{
            CheckValid(spriteFont, text);

            var source = new SpriteFont.CharacterSource(text);
            spriteFont.DrawInto(this, ref source, position, color, rotation, origin, scale, effects, layerDepth);
		}

        private Vector2 _size, _origin, _position;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFullTextureCoordsOnly()
        {
            _texCoordTL.X = 0f;
            _texCoordTL.Y = 0f;
            _texCoordBR.X = 1f;
            _texCoordBR.Y = 1f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFullTexture(Texture2D texture)
        {
            _texCoordTL.X = 0f;
            _texCoordTL.Y = 0f;
            _texCoordBR.X = 1f;
            _texCoordBR.Y = 1f;

            _size.X = texture.Width;
            _size.Y = texture.Height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public void SetPartialTexture(Texture2D texture, Rectangle sourceRectangle)
	    {
            _texCoordTL.X = sourceRectangle.X * texture._texelSize.X;
            _texCoordTL.Y = sourceRectangle.Y * texture._texelSize.Y;
            _texCoordBR.X = (sourceRectangle.X + sourceRectangle.Width) * texture._texelSize.X;
            _texCoordBR.Y = (sourceRectangle.Y + sourceRectangle.Height) * texture._texelSize.Y;

            _size.X = sourceRectangle.Width;
            _size.Y = sourceRectangle.Height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPartialTextureCoordsOnly(Texture2D texture, Rectangle sourceRectangle)
        {
            _texCoordTL.X = sourceRectangle.X * texture._texelSize.X;
            _texCoordTL.Y = sourceRectangle.Y * texture._texelSize.Y;
            _texCoordBR.X = (sourceRectangle.X + sourceRectangle.Width) * texture._texelSize.X;
            _texCoordBR.Y = (sourceRectangle.Y + sourceRectangle.Height) * texture._texelSize.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public void ApplyScale(float scale)
	    {
	        _origin.X *= scale;
	        _origin.Y *= scale;
	        _size.X *= scale;
	        _size.Y *= scale;
	    }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyScale(Vector2 scale)
        {
            _origin.X *= scale.X;
            _origin.Y *= scale.Y;
            _size.X *= scale.X;
            _size.Y *= scale.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyScaleToSizeOnly(Vector2 scale)
        {
            _size.X *= scale.X;
            _size.Y *= scale.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyFlip(SpriteEffects effects)
	    {
            if ((effects & SpriteEffects.FlipVertically) != 0)
            {
                var temp = _texCoordBR.Y;
                _texCoordBR.Y = _texCoordTL.Y;
                _texCoordTL.Y = temp;
            }
            if ((effects & SpriteEffects.FlipHorizontally) != 0)
            {
                var temp = _texCoordBR.X;
                _texCoordBR.X = _texCoordTL.X;
                _texCoordTL.X = temp;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPosition(Vector2 position)
	    {
	        _position.X = position.X - _origin.X;
	        _position.Y = position.Y - _origin.Y;
	    }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRotatedPosition(Vector2 position, float sin, float cos)
        {
            _position.X = position.X - (_origin.X * cos - _origin.Y * sin);
            _position.Y = position.Y - (_origin.X * sin + _origin.Y * cos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetSortKey(Texture2D texture, float depth)
	    {
            switch (_sortMode)
            {
                case SpriteSortMode.Texture:
                    return texture.SortingKey;
                case SpriteSortMode.FrontToBack:
                    return depth;
                case SpriteSortMode.BackToFront:
                    return -depth;
            }
	        return 0f;
	    }

        /// <summary>
        /// Immediately releases the unmanaged resources used by this object.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (_spriteEffect != null)
                    {
                        _spriteEffect.Dispose();
                        _spriteEffect = null;
                    }
                }
            }
            base.Dispose(disposing);
        }
	}
}

