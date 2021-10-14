﻿using CarinaStudio;
using CarinaStudio.IO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Carina.PixelViewer.Media.ImageRenderers
{
	/// <summary>
	/// Base implementation of <see cref="IImageRenderer"/>.
	/// </summary>
	abstract class BaseImageRenderer : IImageRenderer
	{
		/// <summary>
		/// Initialize new <see cref="BaseImageRenderer"/> instance.
		/// </summary>
		/// <param name="format">Format supported by this instance.</param>
		protected BaseImageRenderer(ImageFormat format)
		{
			this.Format = format;
			this.Logger = App.Current.LoggerFactory.CreateLogger(this.GetType().Name);
		}


		/// <summary>
		/// Logger.
		/// </summary>
		protected ILogger Logger { get; }


		/// <summary>
		/// Called to perform image rendering. The method will be called in background thread.
		/// </summary>
		/// <param name="source">Source of raw image data.</param>
		/// <param name="imageStream"><see cref="Stream"/> to read raw image data.</param>
		/// <param name="bitmapBuffer"><see cref="IBitmapBuffer"/> to receive rendered image.</param>
		/// <param name="renderingOptions">Rendering options.</param>
		/// <param name="planeOptions">List of <see cref="ImagePlaneOptions"/> for rendering.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		protected abstract void OnRender(IImageDataSource source, Stream imageStream, IBitmapBuffer bitmapBuffer, ImageRenderingOptions renderingOptions, IList<ImagePlaneOptions> planeOptions, CancellationToken cancellationToken);


		// Render.
		public async Task Render(IImageDataSource source, IBitmapBuffer bitmapBuffer, ImageRenderingOptions renderingOptions, IList<ImagePlaneOptions> planeOptions, CancellationToken cancellationToken)
		{
			// check parameter
			if (bitmapBuffer.Format != this.RenderedFormat)
				throw new ArgumentException($"Invalid format of bitmap buffer: {bitmapBuffer.Format}.");

			// share resources
			using var sharedSource = source.Share();
			using var sharedBitmapBuffer = bitmapBuffer.Share();

			// open stream
			var stream = await sharedSource.OpenStreamAsync(StreamAccess.Read);
			if (cancellationToken.IsCancellationRequested)
			{
				Global.RunWithoutErrorAsync(stream.Dispose);
				throw new TaskCanceledException();
			}

			// render
			try
            {
				await Task.Run(() =>
				{
					this.OnRender(sharedSource, stream, sharedBitmapBuffer, renderingOptions, planeOptions, cancellationToken);
				});
			}
			finally
            {
				Global.RunWithoutErrorAsync(stream.Dispose);
			}
		}


		// Implementations.
		public abstract IList<ImagePlaneOptions> CreateDefaultPlaneOptions(int width, int height);
		public abstract int EvaluatePixelCount(IImageDataSource source);
		public abstract long EvaluateSourceDataSize(int width, int height, ImageRenderingOptions renderingOptions, IList<ImagePlaneOptions> planeOptions);
		public ImageFormat Format { get; }
		public virtual BitmapFormat RenderedFormat => BitmapFormat.Bgra32;
	}
}
