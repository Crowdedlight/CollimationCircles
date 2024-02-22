﻿using System;
using CollimationCircles.Messages;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using System.Collections.Generic;
using System.Threading;

namespace CollimationCircles.Services
{
    public enum StreamSource
    {
        UVC,
        RaspberryPi,
        Remote,
        Undefined
    }

    internal class LibVLCService : ILibVLCService
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly LibVLC libVLC;

        private string protocol = string.Empty;
        private string address = string.Empty;
        private string port = string.Empty;
        private string pathAndQuery = string.Empty;
        private const string rpiPort = "49555";

        public string FullAddress { get; set; } = string.Empty;
        public MediaPlayer MediaPlayer { get; }

        private StreamSource streamSource;

        public LibVLCService()
        {
            FullAddress = GetFullUrlFromParts();

            // https://wiki.videolan.org/VLC_command-line_help/

            string[] libVLCOptions = [
                //$"--width=320",
                //$"--height=240",
                //$"--zoom=1.5",
                //$"--log-verbose=0"
                //"--video-filter=adjust{contrast=1.0,brightness=1.0,hue=0,saturation=1.0,gamma=1.0}"
            ];

            libVLC = new(libVLCOptions);

            MediaPlayer = new(libVLC)
            {
                FileCaching = 0,
                NetworkCaching = 0,
                EnableHardwareDecoding = true
            };

            MediaPlayer.Opening += (sender, e) => WeakReferenceMessenger.Default.Send(new CameraStateMessage(CameraState.Opening));
            MediaPlayer.Playing += (sender, e) => WeakReferenceMessenger.Default.Send(new CameraStateMessage(CameraState.Playing));
            MediaPlayer.Paused += (sender, e) => WeakReferenceMessenger.Default.Send(new CameraStateMessage(CameraState.Paused));
            MediaPlayer.Stopped += (sender, e) => WeakReferenceMessenger.Default.Send(new CameraStateMessage(CameraState.Stopped));
        }

        public void Play()
        {
            if (streamSource == StreamSource.RaspberryPi)
            {
                //rpicam-vid -t 0 --inline --listen -n -o tcp://0.0.0.0:5000

                List<string> parameters = [
                    "-t",
                    "0",
                    "--inline",
                    "--listen",
                    "-n",
                    "-o",
                    $"tcp://0.0.0.0:{rpiPort}"
                ];

                AppService.ExecuteCommand(
                    "rpicam-vid",
                    parameters, timeout: 0);

                Thread.Sleep(1000);
            }

            if (!string.IsNullOrWhiteSpace(FullAddress))
            {
                string[] mediaAdditionalOptions = [
                    //$"--osd",
                    //$"--video-title=my title",
                    //$"--avcodec-hw=any",
                    //$"--zoom=0.25"
                ];

                using var media = new Media(
                        libVLC,
                        FullAddress,
                        FromType.FromLocation,
                        mediaAdditionalOptions
                        );

                MediaPlayer.Play(media);
                logger.Info($"Playing web camera stream: '{media.Mrl}'");
            }
        }

        private string GetFullUrlFromParts()
        {
            pathAndQuery = string.Empty;
            port = string.Empty;
            address = string.Empty;

            if (streamSource == StreamSource.UVC)
            {
                if (OperatingSystem.IsWindows())
                {
                    protocol = "dshow";
                }
                else if (OperatingSystem.IsMacOS())
                {
                    protocol = "qtcapture";
                }
                else
                {
                    protocol = "v4l2";
                    address = "/dev/video0";
                }
            }
            else if (streamSource == StreamSource.RaspberryPi)
            {
                protocol = "tcp/h264";
                address = "localhost";
                port = rpiPort;
            }
            else if (streamSource == StreamSource.Remote)
            {
                protocol = "http";
            }

            string newRemoteAddress = address;
            string addr = newRemoteAddress;
            string pth = string.IsNullOrWhiteSpace(pathAndQuery) ? "" : pathAndQuery;
            string prt = string.IsNullOrWhiteSpace(port) ? "" : $":{port}";

            return $"{protocol}://{addr}{prt}{pth}";
        }

        public string DefaultAddress(StreamSource streamSource)
        {
            this.streamSource = streamSource;
            FullAddress = GetFullUrlFromParts();
            return FullAddress;
        }
    }
}
