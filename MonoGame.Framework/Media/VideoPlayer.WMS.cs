// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Microsoft.Xna.Framework.Graphics;
using SharpDX;
using SharpDX.MediaFoundation;
using SharpDX.Win32;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Xna.Framework.Media
{
    public sealed partial class VideoPlayer : IDisposable
    {
        enum InternalState
        {
            Stopped,
            WaitingForSessionStart,
            Playing,
            WaitingForSessionPaused,
            Paused,
            WaitingForSessionStop,
            PresentationEnded,
            Closed,
        }

        private InternalState _internalState;
        private MediaSession _session;
        private AudioStreamVolume _volumeController;
        private PresentationClock _clock;
        private const int defaultTimeoutMs = 1000;
        private byte[] _black = null;
#if WINRT
        readonly object _locker = new object();
#endif

        private Guid AudioStreamVolumeGuid;
        private Texture2D _texture;
        private Callback _callback;
        internal MediaSession Session { get { return _session; } }
        private Topology _currentTopology;

        private static readonly Variant PositionCurrent = new Variant();
        private static readonly Variant PositionBeginning = new Variant { ElementType = VariantElementType.Long, Value = 0L };

        private class Callback : AsyncCallbackBase
        {
            private VideoPlayer _player;

            public Callback(VideoPlayer player)
            {
                _player = player;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                _player = null;
            }

            public override void Invoke(AsyncResult asyncResultRef)
            {
                if ((_player == null) || (_player.Session == null))
                    return;

                var ev = _player.Session.EndGetEvent(asyncResultRef);

                // Trigger an "on Video Ended" event here if needed
                if (ev.TypeInfo == MediaEventTypes.SessionTopologyStatus && ev.Get(EventAttributeKeys.TopologyStatus) == TopologyStatus.Ready)
                    _player.OnTopologyReady();
                else if (ev.TypeInfo == MediaEventTypes.SessionStarted)
                    _player.OnSessionStarted();
                else if (ev.TypeInfo == MediaEventTypes.SessionStopped)
                    _player.OnSessionStopped();
                else if (ev.TypeInfo == MediaEventTypes.SessionClosed)
                {
                    // The session has been closed, no further events should be generated
                    _player.OnSessionClosed();
                    return;
                }
                else if (ev.TypeInfo == MediaEventTypes.SessionPaused)
                    _player.OnSessionPaused();
                else if (ev.TypeInfo == MediaEventTypes.EndOfPresentation)
                    _player.OnPresentationEnded();

                _player.Session.BeginGetEvent(this, null);
            }
        }

        private void PlatformInitialize()
        {
            // The GUID is specified in a GuidAttribute attached to the class
            AudioStreamVolumeGuid = Guid.Parse(((GuidAttribute)typeof(AudioStreamVolume).GetCustomAttributes(typeof(GuidAttribute), false)[0]).Value);

            MediaManagerState.CheckStartup();
            MediaFactory.CreateMediaSession(null, out _session);

            _callback = new Callback(this);
            _session.BeginGetEvent(_callback, null);

            _clock = _session.Clock.QueryInterface<PresentationClock>();
        }

        private void CreateTexture()
        {
            if (_currentVideo == null)
                return;

            if (_texture != null)
            {
                // If the new video is different in size to the previous video, dispose of the current texture
                if (_texture.Width != _currentVideo.Width || _texture.Height != _currentVideo.Height)
                {
                    _texture.Dispose();
                    _texture = null;
                }
            }

            if (_texture == null)
                _texture = new Texture2D(_graphicsDevice, _currentVideo.Width, _currentVideo.Height, false, SurfaceFormat.Bgr32);
        }

        private Texture2D PlatformGetTexture()
        {
            CreateTexture();

            if (_currentVideo != null && State != MediaState.Stopped && State != MediaState.Paused)
            {
                var sampleGrabber = _currentVideo.SampleGrabber;
                var texData = sampleGrabber.TextureData;
                if (texData != null)
                    _texture.SetData(texData);
                else
                {
                    // No texture data was returned, so make sure the texture is set to something.
                    if (_black == null)
                    {
                        _black = new byte[_texture.Width * _texture.Height * SurfaceFormat.Bgr32.GetSize()];
                        Array.Clear(_black, 0, _black.Length);
                    }
                    _texture.SetData(_black);
                }
            }

            return _texture;
        }

        private void PlatformGetState(ref MediaState result)
        {
            switch (_internalState)
            {
                case InternalState.Stopped:
                    result = MediaState.Stopped;
                    return;

                case InternalState.Paused:
                    result = MediaState.Paused;
                    return;
            }

            result = MediaState.Playing;
        }

        private void PlatformPause()
        {
            _internalState = InternalState.WaitingForSessionPaused;
            _session.Pause();
            WaitForInternalStateChange(InternalState.Paused);
        }

        private void PlatformPlay()
        {
            // Cleanup the last video first.
            if (State != MediaState.Stopped)
            {
                PlatformStop();
            }

            if (_currentTopology != _currentVideo.Topology)
            {
                if (_volumeController != null)
                {
                    _volumeController.Dispose();
                    _volumeController = null;
                }

                CreateTexture();

                // Set the new video.
                _currentTopology = _currentVideo.Topology;
                _internalState = InternalState.WaitingForSessionStart;
                _session.SetTopology(SessionSetTopologyFlags.Immediate, _currentVideo.Topology);
            }
            else
            {
                _session.Start(null, PositionBeginning);
            }

            WaitForInternalStateChange(InternalState.Playing);
        }

        private void PlatformResume()
        {
            _internalState = InternalState.WaitingForSessionStart;
            _session.Start(null, PositionCurrent);
            WaitForInternalStateChange(InternalState.Playing);
        }

        private void PlatformStop()
        {
            if (State != MediaState.Stopped)
            {
                _internalState = InternalState.WaitingForSessionStop;
                _session.Stop();
                WaitForInternalStateChange(InternalState.Stopped);
            }
            else
            {
                _internalState = InternalState.Stopped;
            }
        }

        bool WaitForInternalStateChange(InternalState expectedState, int milliseconds = defaultTimeoutMs)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (_internalState != expectedState)
            {
#if WINRT
                lock (_locker)
                    System.Threading.Monitor.Wait(_locker, 1);
#else
                Thread.Sleep(1);
#endif
                if (timer.ElapsedMilliseconds > milliseconds)
                    return false;
            }
            return true;
        }

        private void SetChannelVolumes()
        {
            if (_volumeController != null && !_volumeController.IsDisposed)
            {
                float volume = _volume;
                if (IsMuted)
                    volume = 0.0f;

                for (int i = 0; i < _volumeController.ChannelCount; i++)
                {
                    _volumeController.SetChannelVolume(i, volume);
                }
            }
        }

        private void PlatformSetVolume()
        {
            if (_volumeController == null)
                return;

            SetChannelVolumes();
        }

        private void PlatformSetIsLooped()
        {
        }

        private void PlatformSetIsMuted()
        {
            if (_volumeController == null)
                return;

            SetChannelVolumes();
        }

        private TimeSpan PlatformGetPlayPosition()
        {
            return TimeSpan.FromTicks(_clock.Time);
        }

        private void PlatformDispose(bool disposing)
        {
            if (!disposing)
                return;

            if ((_session != null) && !_session.IsDisposed)
            {
                _session.Close();
                WaitForInternalStateChange(InternalState.Closed, 5000);

                _session.Shutdown();

                SharpDX.Utilities.Dispose(ref _session);
            }

            SharpDX.Utilities.Dispose(ref _volumeController);
            SharpDX.Utilities.Dispose(ref _clock);
            SharpDX.Utilities.Dispose(ref _texture);
            SharpDX.Utilities.Dispose(ref _callback);
        }

        private void OnTopologyReady()
        {
            if (_session.IsDisposed)
                return;

            // Get the volume interface. Returns null if the video has no audio tracks.
            IntPtr volumeObj;
            try
            {
                MediaFactory.GetService(_session, MediaServiceKeys.StreamVolume, AudioStreamVolumeGuid, out volumeObj);
                _volumeController = CppObject.FromPointer<AudioStreamVolume>(volumeObj);
            }
            catch (SharpDXException ex)
            {
                unchecked
                {
                    if (ex.HResult != (int)0x80004002) // E_NOINTERFACE
                        throw;
                }
            }

            SetChannelVolumes();

            // Start playing.
            _session.Start(null, PositionBeginning);
        }

        private void OnSessionStarted()
        {
            _internalState = InternalState.Playing;
        }

        private void OnSessionStopped()
        {
            _internalState = InternalState.Stopped;
        }
 
        private void OnSessionClosed()
        {
            _internalState = InternalState.Closed;
        }

        private void OnSessionPaused()
        {
            _internalState = InternalState.Paused;
        }

        private void OnPresentationEnded()
        {
            if (_isLooped)
            {
                _session.Start(null, PositionBeginning);
                WaitForInternalStateChange(InternalState.Playing);
            }
            else
            {
                _internalState = InternalState.PresentationEnded;
                Stop();
            }
        }
    }
}
