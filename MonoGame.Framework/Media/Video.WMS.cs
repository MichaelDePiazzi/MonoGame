// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using SharpDX;
using SharpDX.MediaFoundation;
using System;

namespace Microsoft.Xna.Framework.Media
{
    public sealed partial class Video : IDisposable
    {
        private Topology _topology;
        internal Topology Topology { get { return _topology; } }

        internal VideoSampleGrabber SampleGrabber { get; private set; }

        private SharpDX.MediaFoundation.MediaSource _mediaSource;

        private void PlatformInitialize()
        {
            if (Topology != null)
                return;

            MediaManagerState.CheckStartup();

            MediaFactory.CreateTopology(out _topology);

            {
                SourceResolver resolver = new SourceResolver();
                ComObject source = resolver.CreateObjectFromURL(FileName, SourceResolverFlags.MediaSource);
                _mediaSource = source.QueryInterface<SharpDX.MediaFoundation.MediaSource>();
                resolver.Dispose();
                source.Dispose();
            }

            PresentationDescriptor presDesc;
            _mediaSource.CreatePresentationDescriptor(out presDesc);

            var descriptorCount = presDesc.StreamDescriptorCount;
            for (var i = 0; i < descriptorCount; i++)
            {
                SharpDX.Mathematics.Interop.RawBool selected;
                StreamDescriptor desc;
                presDesc.GetStreamDescriptorByIndex(i, out selected, out desc);

                if (selected)
                {
                    TopologyNode sourceNode;
                    MediaFactory.CreateTopologyNode(TopologyType.SourceStreamNode, out sourceNode);

                    sourceNode.Set(TopologyNodeAttributeKeys.Source, _mediaSource);
                    sourceNode.Set(TopologyNodeAttributeKeys.PresentationDescriptor, presDesc);
                    sourceNode.Set(TopologyNodeAttributeKeys.StreamDescriptor, desc);

                    TopologyNode outputNode;
                    MediaFactory.CreateTopologyNode(TopologyType.OutputNode, out outputNode);
                    outputNode.Set(TopologyNodeAttributeKeys.NoshutdownOnRemove, false);

                    var typeHandler = desc.MediaTypeHandler;
                    var majorType = typeHandler.MajorType;
                    if (majorType == MediaTypeGuids.Video)
                    {
                        Activate activate;

                        SampleGrabber = new VideoSampleGrabber();

                        var mediaType = new MediaType();
                        mediaType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
                        mediaType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Rgb32);

                        MediaFactory.CreateSampleGrabberSinkActivate(mediaType, SampleGrabber, out activate);
                        outputNode.Object = activate;

                        activate.Dispose();
                        mediaType.Dispose();
                    }

                    if (majorType == MediaTypeGuids.Audio)
                    {
                        Activate activate;
                        MediaFactory.CreateAudioRendererActivate(out activate);

                        outputNode.Object = activate;
                        activate.Dispose();
                    }

                    _topology.AddNode(sourceNode);
                    _topology.AddNode(outputNode);
                    sourceNode.ConnectOutput(0, outputNode, 0);

                    sourceNode.Dispose();
                    outputNode.Dispose();
                    typeHandler.Dispose();
                }

                desc.Dispose();
            }

            presDesc.Dispose();
        }

        private void PlatformDispose(bool disposing)
        {
            if (_topology != null)
            {
                _topology.Dispose();
                _topology = null;
            }

            if (SampleGrabber != null)
            {
                SampleGrabber.Dispose();
                SampleGrabber = null;
            }

            if ((_mediaSource != null) && !_mediaSource.IsDisposed)
            {
                _mediaSource.Shutdown();
                _mediaSource.Dispose();
                _mediaSource = null;
            }
        }
    }
}
