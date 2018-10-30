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
        private MediaSession _session;
        private AudioStreamVolume _volumeController;
        private readonly object _volumeLock = new object();
        private PresentationClock _clock;
        private byte[] _black = null;
#if WINRT
        readonly object _locker = new object();
#endif

        private Guid AudioStreamVolumeGuid;
        private Texture2D _texture;
        private Callback _callback;
        private Topology _currentTopology;

        private static Video _nextVideo;
        private static TimeSpan? _nextVideoStartPosition;
        private static Variant? _desiredPosition;

        private enum SessionState { Stopped, Stopping, Started, Paused, Ended, Closed }
        private SessionState _sessionState = SessionState.Stopped;

        private static readonly Variant PositionCurrent = new Variant();
        private static readonly Variant PositionBeginning = new Variant { ElementType = VariantElementType.Long, Value = 0L };

        private static readonly double MillisecondsPerStopwatchTick = 1000.0 / System.Diagnostics.Stopwatch.Frequency;

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
                if ((_player == null) || (_player._session == null))
                    return;

                var ev = _player._session.EndGetEvent(asyncResultRef);

                switch (ev.TypeInfo)
                {
                    case MediaEventTypes.SessionEnded:
                        _player.OnSessionEnded();
                        break;
                    case MediaEventTypes.SessionTopologyStatus:
                        if (ev.Get(EventAttributeKeys.TopologyStatus) == TopologyStatus.Ready)
                            _player.OnTopologyReady();
                        break;
                    case MediaEventTypes.SessionStopped:
                        _player.OnSessionStopped();
                        break;
                    case MediaEventTypes.SessionClosed:
                        // The session has been closed, no further events should be generated
                        _player.OnSessionClosed();
                        return;
                }

                _player._session.BeginGetEvent(this, null);
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
                if ((_texture.Width != _currentVideo.Width) || (_texture.Height != _currentVideo.Height))
                {
                    _texture.Dispose();
                    _texture = null;
                }
            }

            if (_texture == null)
            {
                _texture = new Texture2D(_graphicsDevice, _currentVideo.Width, _currentVideo.Height, false, SurfaceFormat.Bgr32);
                _black = null;
            }
        }

        private Texture2D PlatformGetTexture()
        {
            CreateTexture();

            if ((_currentVideo != null) && (_currentVideo.Topology == _currentTopology) && ((_sessionState == SessionState.Started) || (_sessionState == SessionState.Paused)))
            {
                var texData = _currentVideo.SampleGrabber.TextureData;
                if (texData != null)
                {
                    _texture.SetData(texData);
                    return _texture;
                }
            }

            // Texture data is not currently available, so return a black texture
            if (_black == null)
                _black = new byte[_texture.Width * _texture.Height * 4];
            _texture.SetData(_black);
            return _texture;
        }

        private void PlatformGetState(ref MediaState result)
        {
        }

        private void PlatformPause()
        {
            if (_sessionState != SessionState.Started)
                return;
            _sessionState = SessionState.Paused;
            _session.Pause();
        }

        private void PlatformPlay()
        {
            if (_currentTopology == _currentVideo.Topology)
                ReplayCurrentVideo(_currentVideo, null);
            else
                PlayNewVideo(_currentVideo, null);
        }

        private void ReplayCurrentVideo(Video video, TimeSpan? startPosition)
        {
            if (_sessionState == SessionState.Stopping)
            {
                // The video will be started after the SessionStopped event is received
                _nextVideo = video;
                _nextVideoStartPosition = startPosition;
                return;
            }

            StartSession(PositionVariantFor(startPosition));
        }

        private void PlayNewVideo(Video video, TimeSpan? startPosition)
        {
            if (_sessionState != SessionState.Stopped)
            {
                // The session needs to be stopped to reset the play position
                // The new video will be started after the SessionStopped event is received
                _nextVideo = video;
                _nextVideoStartPosition = startPosition;
                PlatformStop();
                return;
            }

            StartNewVideo(video, startPosition);
        }

        private void StartNewVideo(Video video, TimeSpan? startPosition)
        {
            lock (_volumeLock)
            {
                if (_volumeController != null)
                {
                    _volumeController.Dispose();
                    _volumeController = null;
                }
            }

            _currentTopology = video.Topology;

            //We need to start playing from 0, then seek the stream when the topology is ready, otherwise the song doesn't play.
            if (startPosition.HasValue)
                _desiredPosition = PositionVariantFor(startPosition.Value);
            _session.SetTopology(SessionSetTopologyFlags.Immediate, _currentTopology);

            StartSession(PositionBeginning);

            // The volume service won't be available until the session topology
            // is ready, so we now need to wait for the event indicating this
        }

        private void StartSession(Variant startPosition)
        {
            _sessionState = SessionState.Started;
            _session.Start(null, startPosition);
        }

        private void PlatformResume()
        {
            if (_sessionState != SessionState.Paused)
                return;
            StartSession(PositionCurrent);
        }

        private void PlatformStop()
        {
            if ((_sessionState == SessionState.Stopped) || (_sessionState == SessionState.Stopping))
                return;
            bool hasFinishedPlaying = (_sessionState == SessionState.Ended);
            _sessionState = SessionState.Stopping;
            if (hasFinishedPlaying)
            {
                // The play position needs to be reset before stopping otherwise the next video may not start playing
                _session.Start(null, PositionBeginning);
            }
            _session.Stop();
        }

        private bool WaitForInternalStateChange(SessionState expectedState, int milliseconds)
        {
            var startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            while (_sessionState != expectedState)
            {
                var elapsedMilliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - startTimestamp) * MillisecondsPerStopwatchTick;
                if (elapsedMilliseconds >= milliseconds)
                    return false;
#if WINRT
                lock (_locker)
                    System.Threading.Monitor.Wait(_locker, 1);
#else
                Thread.Sleep(1);
#endif
            }
            return true;
        }

        private void SetChannelVolumes()
        {
            lock (_volumeLock)
            {
                if (_volumeController == null)
                    return;

                float volume = _isMuted ? 0f : _volume;
                for (int i = 0; i < _volumeController.ChannelCount; i++)
                    _volumeController.SetChannelVolume(i, volume);
            }
        }

        private void PlatformSetVolume()
        {
            SetChannelVolumes();
        }

        private void PlatformSetIsLooped()
        {
        }

        private void PlatformSetIsMuted()
        {
            SetChannelVolumes();
        }

        private TimeSpan PlatformGetPlayPosition()
        {
            if ((_sessionState == SessionState.Stopped) || (_sessionState == SessionState.Stopping))
                return TimeSpan.Zero;
            try
            {
                return TimeSpan.FromTicks(_clock.Time);
            }
            catch (SharpDXException)
            {
                // The presentation clock is most likely not quite ready yet
                return TimeSpan.Zero;
            }
        }

        private void PlatformDispose(bool disposing)
        {
            if (!disposing)
                return;

            if ((_session != null) && !_session.IsDisposed)
            {
                _session.Close();
                WaitForInternalStateChange(SessionState.Closed, 5000);

                _session.Shutdown();

                SharpDX.Utilities.Dispose(ref _session);
            }

            lock (_volumeLock)
                SharpDX.Utilities.Dispose(ref _volumeController);
            SharpDX.Utilities.Dispose(ref _clock);
            SharpDX.Utilities.Dispose(ref _texture);
            SharpDX.Utilities.Dispose(ref _callback);
        }

        private void OnTopologyReady()
        {
            // Get the volume interface. Throws a "NoInterface" exception if the video has no audio tracks.
            try
            {
                IntPtr volumeObjectPtr;
                MediaFactory.GetService(_session, MediaServiceKeys.StreamVolume, AudioStreamVolumeGuid, out volumeObjectPtr);
                lock (_volumeLock)
                    _volumeController = CppObject.FromPointer<AudioStreamVolume>(volumeObjectPtr);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode != Result.NoInterface)
                    throw;
            }

            SetChannelVolumes();

            if (_desiredPosition.HasValue)
            {
                StartSession(_desiredPosition.Value);
                _desiredPosition = null;
            }
        }

        private void OnSessionStopped()
        {
            _sessionState = SessionState.Stopped;
            if (_nextVideo != null)
            {
                if (_nextVideo.Topology != _currentTopology)
                    StartNewVideo(_nextVideo, _nextVideoStartPosition);
                else
                    StartSession(PositionVariantFor(_nextVideoStartPosition));
                _nextVideo = null;
            }
        }

        private void OnSessionClosed()
        {
            _sessionState = SessionState.Closed;
        }

        private void OnSessionEnded()
        {
            if (_isLooped)
                StartSession(PositionBeginning);
            else
                _sessionState = SessionState.Ended;
        }

        private static Variant PositionVariantFor(TimeSpan? position)
        {
            if (position.HasValue)
                return new Variant { Value = position.Value.Ticks };
            return PositionBeginning;
        }
    }
}
