﻿using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace SeeShark.Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dotnet run <camera-device>");
                return;
            }

            var cameraDevice = args[0];

            Console.WriteLine("Current directory: " + Environment.CurrentDirectory);
            Console.WriteLine("Running in {0}-bit mode.", Environment.Is64BitProcess ? "64" : "32");

            // FFmpegBinariesHelper.RegisterFFmpegBinaries();
            ffmpeg.RootPath = "/usr/lib";

            Console.WriteLine($"FFmpeg version info: {ffmpeg.av_version_info()}");

            SetupLogging();
            ConfigureHWDecoder(out var deviceType);

            Console.WriteLine("Decoding...");
            DecodeAllFramesToImages(deviceType, cameraDevice);
        }

        private static void ConfigureHWDecoder(out AVHWDeviceType HWtype)
        {
            HWtype = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
            Console.Write("Use hardware acceleration for decoding? [y/N] ");
            var key = Console.ReadLine();
            var availableHWDecoders = new Dictionary<int, AVHWDeviceType>();

            if (key == "y")
            {
                Console.WriteLine("Select hardware decoder:");
                var type = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
                var number = 0;

                while ((type = ffmpeg.av_hwdevice_iterate_types(type)) != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
                {
                    Console.WriteLine($"{++number}. {type}");
                    availableHWDecoders.Add(number, type);
                }

                if (availableHWDecoders.Count == 0)
                {
                    Console.WriteLine("Your system have no hardware decoders.");
                    HWtype = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
                    return;
                }

                var decoderNumber = availableHWDecoders
                    .SingleOrDefault(t => t.Value == AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2).Key;
                if (decoderNumber == 0)
                    decoderNumber = availableHWDecoders.First().Key;
                Console.Write($"Selected [{decoderNumber}] ");
                int.TryParse(Console.ReadLine(), out var inputDecoderNumber);
                availableHWDecoders.TryGetValue(inputDecoderNumber == 0 ? decoderNumber : inputDecoderNumber,
                    out HWtype);
            }
        }

        private static unsafe void SetupLogging()
        {
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);

            // do not convert to local function
            av_log_set_callback_callback logCallback = (p0, level, format, vl) =>
            {
                if (level > ffmpeg.av_log_get_level()) return;

                var lineSize = 1024;
                var lineBuffer = stackalloc byte[lineSize];
                var printPrefix = 1;
                ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);
                var line = Marshal.PtrToStringAnsi((IntPtr)lineBuffer);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(line);
                Console.ResetColor();
            };

            ffmpeg.av_log_set_callback(logCallback);
        }

        private static unsafe void DecodeAllFramesToImages(AVHWDeviceType HWDevice, string url)
        {
            using var decoder = new CameraStreamDecoder("v4l2", url, HWDevice);

            Console.WriteLine($"codec name: {decoder.CodecName}");

            var info = decoder.GetContextInfo();
            info.ToList().ForEach(x => Console.WriteLine($"{x.Key} = {x.Value}"));

            var srcPixelFormat = HWDevice == AVHWDeviceType.AV_HWDEVICE_TYPE_NONE
                ? decoder.PixelFormat
                : GetHWPixelFormat(HWDevice);
            var dstPixelFormat = AVPixelFormat.AV_PIX_FMT_RGB24;
            var width = decoder.FrameWidth;
            var height = decoder.FrameHeight;
            using var vfc = new VideoFrameConverter(
                width, height, srcPixelFormat,
                width, height, dstPixelFormat
            );


            byte_ptrArray4 dstData;
            int_array4 dstLineSizes;
            int bufferSize = ffmpeg.av_image_alloc(
                ref dstData, ref dstLineSizes,
                width, height, dstPixelFormat, 1).ThrowExceptionIfError();

            var frameNumber = 0;
            while (decoder.TryDecodeNextFrame(out var frame))
            {
                var cFrame = vfc.Convert(frame);


                var srcData = new byte_ptrArray4();
                var srcLineSizes = new int_array4();
                srcData.UpdateFrom(cFrame.data);
                srcLineSizes.UpdateFrom(cFrame.linesize);

                ffmpeg.av_image_copy(
                    ref dstData, ref dstLineSizes,
                    ref srcData, srcLineSizes,
                    srcPixelFormat, decoder.FrameWidth, decoder.FrameHeight);

                var span0 = new ReadOnlySpan<byte>(dstData[0], bufferSize);

                Console.WriteLine($"frame: {frameNumber}");
                frameNumber++;
            }
        }

        private unsafe static void Write_BytePtrArray8_ToFile(byte_ptrArray8 data, string filename)
        {
            var stream = new BufferedStream(File.Create(filename));

            var array = data.ToArray();
            foreach (var el in array)
            {

            }
        }

        private static AVPixelFormat GetHWPixelFormat(AVHWDeviceType hWDevice)
        {
            return hWDevice switch
            {
                AVHWDeviceType.AV_HWDEVICE_TYPE_NONE => AVPixelFormat.AV_PIX_FMT_NONE,
                AVHWDeviceType.AV_HWDEVICE_TYPE_VDPAU => AVPixelFormat.AV_PIX_FMT_VDPAU,
                AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA => AVPixelFormat.AV_PIX_FMT_CUDA,
                AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI => AVPixelFormat.AV_PIX_FMT_VAAPI,
                AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2 => AVPixelFormat.AV_PIX_FMT_NV12,
                AVHWDeviceType.AV_HWDEVICE_TYPE_QSV => AVPixelFormat.AV_PIX_FMT_QSV,
                AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX => AVPixelFormat.AV_PIX_FMT_VIDEOTOOLBOX,
                AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA => AVPixelFormat.AV_PIX_FMT_NV12,
                AVHWDeviceType.AV_HWDEVICE_TYPE_DRM => AVPixelFormat.AV_PIX_FMT_DRM_PRIME,
                AVHWDeviceType.AV_HWDEVICE_TYPE_OPENCL => AVPixelFormat.AV_PIX_FMT_OPENCL,
                AVHWDeviceType.AV_HWDEVICE_TYPE_MEDIACODEC => AVPixelFormat.AV_PIX_FMT_MEDIACODEC,
                _ => AVPixelFormat.AV_PIX_FMT_NONE
            };
        }
    }
}